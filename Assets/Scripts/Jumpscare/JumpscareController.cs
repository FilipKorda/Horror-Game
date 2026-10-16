using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class JumpscareController : MonoBehaviour
{
    public static JumpscareController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip jumpscareSfx;
    [SerializeField] private GameObject[] toggleObjects;
    [SerializeField] private PlayerFearMeter playerFearMeter;

    [Header("Gracz")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerHider playerHider;
    [SerializeField] private PauseController pauseController;

    [Header("Canvas - efekty uderzenia")]
    [SerializeField] private Image impactFlash;
    [SerializeField] private Image bloodVignette;
    [SerializeField] private Color flashColor = new Color(1f, 0.95f, 0.9f, 1f);
    [SerializeField] private Color flashBloodColor = new Color(0.55f, 0f, 0f, 1f);
    [SerializeField] private Color vignetteColor = new Color(0.25f, 0f, 0f, 1f);
    [SerializeField] private FearEffectPreset impactFearEffect;
    [SerializeField] private float impactFearIntensity = 1.25f;
    [SerializeField] private float lungeFearIntensity = 0.6f;

    [Header("Atak 3D")]
    [SerializeField] private float faceDistance = 0.5f;
    [SerializeField] private float faceEndDistance = 0.3f;
    [SerializeField] private float zoomFovAmount = 18f;
    [SerializeField, Range(0f, 1f)] private float lungeZoomPortion = 0.4f;
    [SerializeField] private float faceHeightOffset = -0.04f;
    [SerializeField] private float faceAimOffset = 0.1f;
    [SerializeField, Range(0f, 1f)] private float neckLookShare = 0.3f;
    [SerializeField] private float turnDuration = 0.12f;
    [SerializeField] private float lungeDuration = 0.26f;
    [SerializeField]
    private AnimationCurve lungeCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f), new Keyframe(0.35f, 0.08f), new Keyframe(1f, 1f, 2.6f, 0f));
    [SerializeField] private float cameraTrackSharpness = 28f;
    [SerializeField] private float impactHoldDuration = 0.9f;
    [SerializeField] private float thrashPosition = 0.035f;
    [SerializeField] private float thrashRotation = 4f;
    [SerializeField] private float thrashFrequency = 18f;
    [SerializeField] private float jumpscareNearClip = 0.03f;

    [Header("Światło na twarzy wroga")]
    [SerializeField] private bool useFaceLight = true;
    [SerializeField] private Color faceLightColor = new Color(0.75f, 0.82f, 0.95f, 1f);
    [SerializeField] private float faceLightIntensity = 0.15f;
    [SerializeField] private float faceLightRange = 1.2f;

    [Header("Zakończenie")]
    [SerializeField] private float endFlashFadeDuration = 0.5f;

    private struct AttackPose
    {
        public bool active;
        public Transform attacker;
        public Transform head;
        public Transform camera;
        public Vector3 faceAxis;
        public Vector3 upAxis;
        public Vector3 rootStart;
        public Vector3 facePoint;
        public Vector3 jitter;
        public Quaternion baseRotation;
        public Quaternion twist;
        public float aimWeight;
        public float moveWeight;
    }

    private bool isPlaying;
    private AttackPose pose;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        Instance = this;

        HideImage(impactFlash);
        HideImage(bloodVignette);
    }

    private void LateUpdate()
    {
        if (!pose.active || pose.attacker == null || pose.camera == null)
            return;

        Transform attacker = pose.attacker;

        attacker.rotation = pose.baseRotation * pose.twist;
        LookHeadAtCamera(pose.head, pose.faceAxis, pose.upAxis, pose.camera.position, pose.aimWeight);

        Vector3 target = GetRootForHeadAt(attacker, pose.head, pose.facePoint) + pose.jitter;
        attacker.position = Vector3.LerpUnclamped(pose.rootStart, target, pose.moveWeight);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayAttack(Transform attackerRoot, Animator attackerAnimator, Transform attackerHead,
        Action onAttackerReleased, bool playerIsHidden, AudioClip lungeClip = null,
        bool skipTurn = false, float lungeDurationOverride = 0f, AnimationCurve lungeCurveOverride = null)
    {
        if (isPlaying || attackerRoot == null)
        {
            onAttackerReleased?.Invoke();
            return;
        }

        ResolvePlayerReferences();

        if (playerCamera == null)
        {
            Debug.LogError("[JumpscareController] Brak kamery gracza - pomijam jumpscare.", this);
            onAttackerReleased?.Invoke();
            playerFearMeter?.AddFear();
            return;
        }

        float duration = lungeDurationOverride > 0f ? lungeDurationOverride : lungeDuration;
        AnimationCurve curve = lungeCurveOverride ?? lungeCurve;

        StartCoroutine(AttackRoutine(attackerRoot, attackerAnimator, attackerHead, onAttackerReleased, playerIsHidden, lungeClip,
            skipTurn ? 0f : turnDuration, duration, curve));
    }

    private void ResolvePlayerReferences()
    {
        if (playerHider == null)
            playerHider = FindFirstObjectByType<PlayerHider>();

        if (playerCamera == null && playerHider != null)
            playerCamera = playerHider.GetComponentInChildren<Camera>();

        if (pauseController == null)
            pauseController = FindFirstObjectByType<PauseController>();
    }

    private IEnumerator AttackRoutine(Transform attacker, Animator animator, Transform head,
        Action onAttackerReleased, bool playerIsHidden, AudioClip lungeClip,
        float turnTime, float lungeTime, AnimationCurve curve)
    {
        isPlaying = true;
        bool couldPause = BlockPause();

        Transform cameraTransform = playerCamera.transform;
        Transform body = cameraTransform.parent;

        Quaternion savedBodyRotation = body != null ? body.rotation : Quaternion.identity;
        Quaternion savedCameraLocalRotation = cameraTransform.localRotation;
        float savedFov = playerCamera.fieldOfView;
        float savedNearClip = playerCamera.nearClipPlane;

        if (!playerIsHidden && playerHider != null)
        {
            playerHider.SetMovementLocked(true);
            playerHider.SetLookLocked(true);
        }

        GetLocalFaceAxes(attacker, head, out Vector3 localFaceAxis, out Vector3 localUpAxis);

        EnableObjects(false);

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.SetTrigger("Attack");
        }

        if (lungeClip != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(lungeClip);

        if (sfxSource != null && jumpscareSfx != null)
            AudioManager.Instance.PlayJumpscareSFX(jumpscareSfx);

        playerCamera.nearClipPlane = Mathf.Min(savedNearClip, jumpscareNearClip);
        Light faceLight = useFaceLight ? CreateFaceLight(cameraTransform) : null;

        float elapsed = 0f;
        while (elapsed < turnTime)
        {
            TrackHead(cameraTransform, GetFacePoint(attacker, head, localFaceAxis), 1f - Mathf.Exp(-cameraTrackSharpness * 1.5f * Time.deltaTime));
            elapsed += Time.deltaTime;
            yield return null;
        }

        Vector3 rootStart = attacker.position;
        Quaternion rotationStart = attacker.rotation;
        Vector3 toAttacker = Vector3.ProjectOnPlane(GetHeadPosition(attacker, head) - cameraTransform.position, Vector3.up);
        if (toAttacker.sqrMagnitude < 0.0001f)
            toAttacker = cameraTransform.forward;

        Vector3 lungeDirection = toAttacker.normalized;
        Vector3 facePoint = cameraTransform.position + lungeDirection * faceDistance + Vector3.up * faceHeightOffset;
        Quaternion rotationEnd = Quaternion.LookRotation(-lungeDirection, Vector3.up);

        PlayFearEffect(lungeFearIntensity);
        float lungeFov = savedFov - zoomFovAmount * lungeZoomPortion;

        pose = new AttackPose
        {
            attacker = attacker,
            head = head,
            faceAxis = localFaceAxis,
            upAxis = localUpAxis,
            camera = cameraTransform,
            rootStart = rootStart,
            facePoint = facePoint,
            twist = Quaternion.identity
        };

        elapsed = 0f;
        while (elapsed < lungeTime)
        {
            elapsed += Time.deltaTime;
            float t = curve.Evaluate(Mathf.Clamp01(elapsed / lungeTime));

            pose.baseRotation = Quaternion.Slerp(rotationStart, rotationEnd, Mathf.Clamp01(t * 1.5f));
            pose.aimWeight = Mathf.Clamp01(t * 2f);
            pose.moveWeight = t;
            pose.active = true;

            playerCamera.fieldOfView = Mathf.Lerp(savedFov, lungeFov, Mathf.Clamp01(t));

            TrackHead(cameraTransform, GetFacePoint(attacker, head, localFaceAxis), 1f - Mathf.Exp(-cameraTrackSharpness * Time.deltaTime));
            yield return null;
        }

        pose.baseRotation = rotationEnd;
        pose.aimWeight = 1f;
        pose.moveWeight = 1f;

        PlayFearEffect(impactFearIntensity);

        float seed = UnityEngine.Random.value * 100f;
        elapsed = 0f;
        while (elapsed < impactHoldDuration)
        {
            elapsed += Time.deltaTime;

            float k = Mathf.Clamp01(elapsed / impactHoldDuration);
            float push = k * k;
            float shake = Mathf.Lerp(0.6f, 1.6f, k);
            float closeness = Mathf.Lerp(1f, 0.35f, push);
            Vector3 pushedFacePoint = cameraTransform.position + lungeDirection * Mathf.Lerp(faceDistance, faceEndDistance, push) + Vector3.up * faceHeightOffset;

            float n = elapsed * thrashFrequency;
            pose.jitter = new Vector3(Noise(n, seed), Noise(n, seed + 1f), Noise(n, seed + 2f)) * (thrashPosition * closeness);
            pose.twist = Quaternion.Euler(new Vector3(Noise(n, seed + 3f), Noise(n, seed + 4f), Noise(n, seed + 5f)) * (thrashRotation * shake));
            pose.facePoint = pushedFacePoint;

            playerCamera.fieldOfView = Mathf.Lerp(lungeFov, savedFov - zoomFovAmount, Mathf.SmoothStep(0f, 1f, k));

            TrackHead(cameraTransform, GetFacePoint(attacker, head, localFaceAxis), 1f - Mathf.Exp(-cameraTrackSharpness * Time.deltaTime));
            UpdateImpactOverlay(elapsed);
            yield return null;
        }

        SetImage(impactFlash, flashBloodColor, 1f);
        HideImage(bloodVignette);
        pose.active = false;

        if (faceLight != null)
            Destroy(faceLight.gameObject);

        try
        {
            onAttackerReleased?.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }

        if (body != null)
            body.rotation = savedBodyRotation;

        cameraTransform.localRotation = savedCameraLocalRotation;
        playerCamera.fieldOfView = savedFov;
        playerCamera.nearClipPlane = savedNearClip;

        playerFearMeter?.AddFear();

        elapsed = 0f;
        while (elapsed < endFlashFadeDuration)
        {
            elapsed += Time.deltaTime;
            float fade = 1f - Mathf.Clamp01(elapsed / endFlashFadeDuration);
            SetImage(impactFlash, flashBloodColor, fade * fade);
            yield return null;
        }

        HideImage(impactFlash);
        EnableObjects(true);

        if (!playerIsHidden && playerHider != null)
        {
            playerHider.SetMovementLocked(false);
            playerHider.SetLookLocked(false);
        }

        RestorePause(couldPause);
        isPlaying = false;
    }

    private void PlayFearEffect(float intensity)
    {
        if (impactFearEffect != null && FearCameraEffect.Instance != null && intensity > 0f)
            FearCameraEffect.Instance.Play(impactFearEffect, intensity);
    }

    private void UpdateImpactOverlay(float elapsed)
    {
        float flash = Mathf.Clamp01(1f - elapsed / 0.18f);
        Color flashTint = Color.Lerp(flashBloodColor, flashColor, Mathf.Clamp01(1f - elapsed / 0.04f));
        SetImage(impactFlash, flashTint, flash * flash * 0.6f);

        if (bloodVignette != null)
        {
            float rise = Mathf.Clamp01(elapsed / 0.12f);
            float pulse = 0.85f + 0.15f * Mathf.Sin(elapsed * 40f);
            SetImage(bloodVignette, vignetteColor, rise * pulse);
            bloodVignette.rectTransform.localScale = Vector3.one * (1.08f - 0.08f * rise);
        }
    }

    private void TrackHead(Transform cameraTransform, Vector3 headPosition, float blend)
    {
        Vector3 toHead = headPosition - cameraTransform.position;
        if (toHead.sqrMagnitude < 0.0001f)
            return;

        Quaternion target = Quaternion.LookRotation(toHead, Vector3.up);
        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, target, Mathf.Clamp01(blend));
    }

    private void LookHeadAtCamera(Transform head, Vector3 localFaceAxis, Vector3 localUpAxis, Vector3 cameraPosition, float weight)
    {
        if (head == null || weight <= 0f)
            return;

        Transform neckUpper = head.parent;
        Transform neckLower = neckUpper != null ? neckUpper.parent : null;

        RotateBoneTowardCamera(neckLower, head, localFaceAxis, cameraPosition, neckLookShare * weight);
        RotateBoneTowardCamera(neckUpper, head, localFaceAxis, cameraPosition, neckLookShare * weight);
        RotateBoneTowardCamera(head, head, localFaceAxis, cameraPosition, weight);

        Vector3 toCamera = cameraPosition - head.position;
        Vector3 headUp = Vector3.ProjectOnPlane(head.TransformDirection(localUpAxis), toCamera);
        Vector3 worldUp = Vector3.ProjectOnPlane(Vector3.up, toCamera);

        if (headUp.sqrMagnitude < 0.0001f || worldUp.sqrMagnitude < 0.0001f)
            return;

        float roll = Vector3.SignedAngle(headUp, worldUp, toCamera) * weight;
        head.rotation = Quaternion.AngleAxis(roll, toCamera.normalized) * head.rotation;
    }

    private static void RotateBoneTowardCamera(Transform bone, Transform head, Vector3 localFaceAxis, Vector3 cameraPosition, float weight)
    {
        if (bone == null || weight <= 0f)
            return;

        Vector3 face = head.TransformDirection(localFaceAxis);
        Vector3 toCamera = cameraPosition - head.position;

        if (face.sqrMagnitude < 0.0001f || toCamera.sqrMagnitude < 0.0001f)
            return;

        Quaternion delta = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(face, toCamera), weight);
        bone.rotation = delta * bone.rotation;
    }

    private static void GetLocalFaceAxes(Transform attacker, Transform head, out Vector3 faceAxis, out Vector3 upAxis)
    {
        faceAxis = Vector3.forward;
        upAxis = Vector3.up;

        if (head == null)
            return;

        foreach (SkinnedMeshRenderer skin in attacker.GetComponentsInChildren<SkinnedMeshRenderer>(false))
        {
            if (skin.sharedMesh == null || skin.name.IndexOf("eye", StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            if (TryGetEyeAxes(attacker, head, skin, out faceAxis, out upAxis))
                return;
        }

        faceAxis = head.InverseTransformDirection(attacker.forward);
        upAxis = head.InverseTransformDirection(attacker.up);
    }

    private static bool TryGetEyeAxes(Transform attacker, Transform head, SkinnedMeshRenderer eyes, out Vector3 faceAxis, out Vector3 upAxis)
    {
        faceAxis = Vector3.forward;
        upAxis = Vector3.up;

        Transform[] bones = eyes.bones;
        Matrix4x4[] bindposes = eyes.sharedMesh.bindposes;

        int headIndex = Array.IndexOf(bones, head);
        int baseIndex = Array.IndexOf(bones, eyes.rootBone != null ? eyes.rootBone : attacker);
        int leftIndex = FindBoneIndex(bones, "upperarm_l", "clavicle_l");
        int rightIndex = FindBoneIndex(bones, "upperarm_r", "clavicle_r");

        if (!IsValidBone(headIndex, bindposes) || !IsValidBone(baseIndex, bindposes)
            || !IsValidBone(leftIndex, bindposes) || !IsValidBone(rightIndex, bindposes))
            return false;

        Vector3 up = BindPosition(bindposes, headIndex) - BindPosition(bindposes, baseIndex);
        Vector3 right = Vector3.ProjectOnPlane(BindPosition(bindposes, rightIndex) - BindPosition(bindposes, leftIndex), up);

        if (up.sqrMagnitude < 0.000001f || right.sqrMagnitude < 0.000001f)
            return false;

        Matrix4x4 meshToHead = bindposes[headIndex];
        faceAxis = meshToHead.MultiplyVector(Vector3.Cross(up.normalized, right.normalized)).normalized;
        upAxis = meshToHead.MultiplyVector(up).normalized;

        Mesh baked = new Mesh();
        eyes.BakeMesh(baked, true);
        Vector3 eyeCenter = baked.bounds.center;
        Destroy(baked);

        Vector3 toEyes = eyes.transform.TransformPoint(eyeCenter) - head.position;

        if (Vector3.Dot(head.TransformDirection(faceAxis), toEyes) < 0f)
            faceAxis = -faceAxis;

        return true;
    }

    private static int FindBoneIndex(Transform[] bones, params string[] names)
    {
        foreach (string boneName in names)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] != null && bones[i].name == boneName)
                    return i;
            }
        }

        return -1;
    }

    private static bool IsValidBone(int index, Matrix4x4[] bindposes)
    {
        return index >= 0 && index < bindposes.Length;
    }

    private static Vector3 BindPosition(Matrix4x4[] bindposes, int index)
    {
        return bindposes[index].inverse.GetColumn(3);
    }

    private Vector3 GetFacePoint(Transform attacker, Transform head, Vector3 localFaceAxis)
    {
        if (head == null)
            return GetHeadPosition(attacker, head);

        return head.position + head.TransformDirection(localFaceAxis).normalized * faceAimOffset;
    }

    private static Vector3 GetHeadPosition(Transform attacker, Transform head)
    {
        return head != null ? head.position : attacker.position + Vector3.up * 1.5f;
    }

    private static Vector3 GetRootForHeadAt(Transform attacker, Transform head, Vector3 headTarget)
    {
        return headTarget - (GetHeadPosition(attacker, head) - attacker.position);
    }

    private Light CreateFaceLight(Transform cameraTransform)
    {
        GameObject lightObject = new GameObject("Jumpscare Face Light");
        lightObject.transform.SetParent(cameraTransform, false);
        lightObject.transform.localPosition = new Vector3(0.22f, 0.3f, 0f);

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = faceLightColor;
        light.intensity = faceLightIntensity;
        light.range = faceLightRange;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel;

        return light;
    }

    private bool BlockPause()
    {
        if (pauseController == null)
            return false;

        bool couldPause = pauseController.CanPause;
        pauseController.CanPause = false;
        return couldPause;
    }

    private void RestorePause(bool couldPause)
    {
        if (pauseController != null && couldPause)
            pauseController.CanPause = true;
    }

    private void EnableObjects(bool enable)
    {
        if (toggleObjects == null)
            return;

        foreach (GameObject obj in toggleObjects)
        {
            if (obj != null)
                obj.SetActive(enable);
        }
    }

    private static void SetImage(Image image, Color color, float alpha)
    {
        if (image == null)
            return;

        color.a = alpha;
        image.gameObject.SetActive(true);
        image.color = color;
    }

    private static void HideImage(Image image)
    {
        if (image == null)
            return;

        Color color = image.color;
        color.a = 0f;
        image.color = color;
        image.gameObject.SetActive(false);
    }

    private static float Noise(float time, float seed)
    {
        return Mathf.PerlinNoise(time, seed) * 2f - 1f;
    }
}
