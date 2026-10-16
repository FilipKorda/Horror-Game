using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraSequence : MonoBehaviour
{
    [SerializeField] private GameObject[] cameras;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject uIcanvas;
    [SerializeField] private Volume volume;
    [SerializeField] private TutorialCanvas tutorialCanvas;
    [SerializeField] private float delay = 5f;
    [SerializeField] private float transitionDuration = 1f;
    [SerializeField] private float fovStart = 60f;
    [SerializeField] private float fovEnd = 50f;
    [SerializeField] private float fovDuration = 5f;
    [SerializeField] private DialogueSequence dialogueSequence;
    [SerializeField] private QuestManager questManager;
    [SerializeField] private PauseController pauseController;
    private ColorAdjustments colorAdjustments;
    private Coroutine cameraCoroutine;
    private bool isSkipped;

    private void Start()
    {
        pauseController.CanPause = false;

        if (volume != null)
            volume.profile.TryGet(out colorAdjustments);

        uIcanvas.SetActive(false);
        player.SetActive(false);

        dialogueSequence.Hide();

        cameraCoroutine = StartCoroutine(CameraRoutine());
    }

    public void Skip()
    {
        if (isSkipped)
            return;

        isSkipped = true;

        if (cameraCoroutine != null)
            StopCoroutine(cameraCoroutine);

        StopAllCoroutines();

        DisableAllCameras();

        dialogueSequence.Hide();

        if (colorAdjustments != null)
            colorAdjustments.postExposure.value = 0f;

        player.SetActive(true);
        uIcanvas.SetActive(true);
        pauseController.CanPause = true;

        tutorialCanvas.Initialize();

        gameObject.SetActive(false);

        questManager.OnCutsceneFinished();
    }

    private IEnumerator CameraRoutine()
    {
        DisableAllCameras();

        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].SetActive(true);

            Camera currentCamera = cameras[i].GetComponentInChildren<Camera>();

            if (currentCamera != null)
            {
                bool zoomIn = i % 2 == 0;

                float startFov = zoomIn ? fovStart : fovEnd;
                float targetFov = zoomIn ? fovEnd : fovStart;

                currentCamera.fieldOfView = startFov;

                StartCoroutine(ChangeFOV(currentCamera, startFov, targetFov));
            }

            if (i == 0)
                dialogueSequence.Initialize();

            yield return new WaitForSeconds(delay - transitionDuration);

            dialogueSequence.Hide();

            yield return StartCoroutine(ChangeExposure(0f, -10f));

            if (i == cameras.Length - 1)
            {
                player.SetActive(true);
                uIcanvas.SetActive(true);
                pauseController.CanPause = true;

                yield return null;

                cameras[i].SetActive(false);

                colorAdjustments.postExposure.value = 0f;

                tutorialCanvas.Initialize();

                gameObject.SetActive(false);

                yield break;
            }

            cameras[i].SetActive(false);
            cameras[i + 1].SetActive(true);

            yield return StartCoroutine(ChangeExposure(-10f, 0f));

            dialogueSequence.Show();
        }
    }

    private IEnumerator ChangeFOV(Camera currentCamera, float startFov, float targetFov)
    {
        float elapsed = 0f;

        while (elapsed < fovDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / fovDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            currentCamera.fieldOfView = Mathf.Lerp(startFov, targetFov, t);

            yield return null;
        }

        currentCamera.fieldOfView = targetFov;
    }

    private IEnumerator ChangeExposure(float startValue, float targetValue)
    {
        if (colorAdjustments == null)
            yield break;

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / transitionDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            colorAdjustments.postExposure.value =
                Mathf.Lerp(startValue, targetValue, t);

            yield return null;
        }

        colorAdjustments.postExposure.value = targetValue;
    }

    private void DisableAllCameras()
    {
        foreach (GameObject cameraObject in cameras)
        {
            if (cameraObject != null)
                cameraObject.SetActive(false);
        }
    }
}