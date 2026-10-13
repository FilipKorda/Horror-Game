using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;


[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    private enum State { Patrolling, Chasing, Redirecting, Approaching, Minigame, Caught }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform eyes;
    [SerializeField] private AudioClip heartbeatClip;
    [SerializeField] private AudioClip[] roarClips;
    [SerializeField] private AudioClip boomClip;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float targetHeightOffsetAfterJump = -0.5f;
    private Transform approachingPoint;
    private Transform player;

    [Header("Audio 3D (roar, boom)")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private float voiceMinDistance = 3f;
    [SerializeField] private float voiceMaxDistance = 22f;
    [SerializeField] private AudioRolloffMode voiceRolloffMode = AudioRolloffMode.Linear;
    [SerializeField, Range(0f, 1f)] private float boomVolume = 1f;

    [Header("Jumpscare Camera (złapanie bez ukrycia)")]
    [SerializeField] private float cameraTurnDuration = 0.2f;
    [SerializeField] private float cameraZoomDuration = 0.2f;
    [SerializeField] private float jumpscareZoomFOV = 35f;
    [SerializeField] private Transform targetAfterCatch;
    private Camera playerCamera;
    private PlayerHider playerHider;
    private InteractionSystem interactionSystem;

    [Header("Patrol (losowe punkty w promieniu)")]
    [SerializeField] private Transform patrolAreaCenter;
    [SerializeField] private float patrolRadius = 16f;
    [SerializeField] private float minPatrolPointDistance = 11f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitTimeAtPoint = 2f;
    [SerializeField] private float minWallClearance = 1f;
    [SerializeField] private int maxPatrolPointAttempts = 10;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float repathThreshold = 0.5f;
    [SerializeField] private float stuckCheckInterval = 1f;
    [SerializeField] private float stuckDistanceThreshold = 0.2f;
    [SerializeField] private float catchDistance = 1f;

    [Header("Despawn")]
    [SerializeField] private float minNoInteractionDespawnTime = 20f;
    [SerializeField] private float maxNoInteractionDespawnTime = 40f;

    [Header("Detection")]
    [SerializeField] private float viewRadius = 20f;
    [SerializeField, Range(0f, 360f)] private float viewAngle = 150f;
    [SerializeField] private LayerMask obstacleLayerMask;

    [Header("Animation")]
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private float idleAnimValue = 0f;
    [SerializeField] private float walkAnimValue = 1f;
    [SerializeField] private float runAnimValue = 2f;
    [SerializeField] private float lookBehindAnimValue = -1f;
    [SerializeField] private float animationDamping = 0.15f;

    [Header("TEST START MINI GAME")]
    public InputActionReference testStartMiniGame;

    private NavMeshAgent agent;
    private NavMeshPath patrolPath;
    private State state;
    private Vector3 patrolCenter;

    private float waitTimer;
    private bool isWaitingAtPoint;
    private Vector3 lastChaseDestination;

    private float stuckTimer;
    private Vector3 lastCheckedPosition;

    private float lastPlayerInteractionTime;
    private float noInteractionDespawnTime;
    private bool isPlayerHidden;

    public event System.Action<EnemyAI> OnDespawnRequested;
    public static event System.Action OnChaseStarted;
    public static event System.Action OnChaseEnded;
    private bool isChaseActive;
    private int navMeshAreaMask;

    private const int PlayerSamplePointCount = 3;
    private readonly RaycastHit[] sightHits = new RaycastHit[16];

    public void SetNavMeshAreaMask(int mask)
    {
        navMeshAreaMask = mask;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        patrolPath = new NavMeshPath();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (playerHider == null && player != null)
            playerHider = player.GetComponent<PlayerHider>();

        if (interactionSystem == null && player != null)
            interactionSystem = player.GetComponentInChildren<InteractionSystem>();

        if (playerCamera == null)
            playerCamera = player.GetComponentInChildren<Camera>();

        patrolCenter = patrolAreaCenter != null ? patrolAreaCenter.position : transform.position;


        if (voiceSource == null)
            voiceSource = gameObject.AddComponent<AudioSource>();

        voiceSource.playOnAwake = false;
        voiceSource.rolloffMode = voiceRolloffMode;
        voiceSource.minDistance = voiceMinDistance;
        voiceSource.maxDistance = voiceMaxDistance;
    }

    private void OnEnable()
    {
        PlayerHider.OnPlayerHidden += HandlePlayerHidden;
        PlayerHider.OnPlayerRevealed += HandlePlayerRevealed;

        testStartMiniGame.action.performed += TestStartMiniGame;
        testStartMiniGame.action.Enable();
    }

    private void OnDisable()
    {
        PlayerHider.OnPlayerHidden -= HandlePlayerHidden;
        PlayerHider.OnPlayerRevealed -= HandlePlayerRevealed;

        testStartMiniGame.action.performed -= TestStartMiniGame;
    }

    private void OnDestroy()
    {
        SetChaseActive(false);
    }

    private void SetChaseActive(bool active)
    {
        if (isChaseActive == active)
            return;

        isChaseActive = active;

        if (active)
            OnChaseStarted?.Invoke();
        else
            OnChaseEnded?.Invoke();
    }

    private void TestStartMiniGame(InputAction.CallbackContext ctx)
    {
        TESTEnterMiniGame();
    }

    private void HandlePlayerHidden(HidingSpot hidingSpot)
    {
        isPlayerHidden = true;

        if (state != State.Chasing)
            return;

        if (hidingSpot == null || hidingSpot.EnemyInteractionPoint == null)
            return;

        approachingPoint = hidingSpot.EnemyInteractionPoint;

        EnterApproachingState();
    }

    private void Start()
    {
        state = State.Patrolling;
        agent.speed = patrolSpeed;

        lastPlayerInteractionTime = Time.time;
        noInteractionDespawnTime = Random.Range(minNoInteractionDespawnTime, maxNoInteractionDespawnTime);

        isPlayerHidden = false;

        GoToRandomPatrolPoint();
    }

    private void Update()
    {
        bool canSeePlayer = CanSeePlayer();

        if (canSeePlayer)
            lastPlayerInteractionTime = Time.time;

        if ((state == State.Chasing || state == State.Approaching) && player != null
            && Vector3.Distance(transform.position, player.position) <= catchDistance)
        {
            EnterCaughtState();
            return;
        }

        switch (state)
        {
            case State.Patrolling:
                UpdatePatrol(canSeePlayer);
                break;

            case State.Chasing:
                UpdateChase();
                break;

            case State.Redirecting:
                UpdateRedirect();
                break;

            case State.Approaching:
                UpdateApproaching();
                break;

            case State.Minigame:
                // enemy stoi w miejscu, czeka na wynik minigry - patrz HandleMinigameWon/HandleMinigameLost
                break;
        }

        UpdateStuckDetection();

        UpdateAnimation();
    }

    private void UpdatePatrol(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            EnterChaseState();
            return;
        }

        if (Time.time - lastPlayerInteractionTime > noInteractionDespawnTime)
        {
            RequestDespawn();
            return;
        }

        if (isWaitingAtPoint)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaitingAtPoint = false;
                GoToRandomPatrolPoint();
            }
            return;
        }

        if (!agent.pathPending && agent.hasPath && agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            GoToRandomPatrolPoint();
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            isWaitingAtPoint = true;
            waitTimer = waitTimeAtPoint;
        }
    }

    private void UpdateChase()
    {
        if (player == null)
            return;

        if (agent.hasPath && !agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            EnterRedirectState();
            return;
        }

        if (Vector3.Distance(player.position, lastChaseDestination) > repathThreshold)
        {
            lastChaseDestination = player.position;
            agent.SetDestination(player.position);
        }
    }

    private void UpdateRedirect()
    {
        if (agent.hasPath && !agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            EnterRedirectState();
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            RequestDespawn();
    }

    private void UpdateApproaching()
    {
        if (agent.hasPath && !agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            EnterRedirectState();
            return;
        }

        if (agent.pathPending)
            return;

        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            agent.SetDestination(approachingPoint.position);
            return;
        }

        if (agent.remainingDistance <= 0.05f && agent.velocity.sqrMagnitude < 0.01f)
        {
            agent.isStopped = true;
            agent.ResetPath();
            EnterMinigameState();
        }
    }

    private void RequestDespawn()
    {
        AudioManager.Instance.StopMonsterRoar(voiceSource);
        OnDespawnRequested?.Invoke(this);
    }

    private void UpdateStuckDetection()
    {
        bool isMoving = state == State.Chasing || state == State.Redirecting || state == State.Approaching
            || (state == State.Patrolling && !isWaitingAtPoint);

        if (!isMoving)
        {
            stuckTimer = 0f;
            lastCheckedPosition = transform.position;
            return;
        }

        stuckTimer += Time.deltaTime;
        if (stuckTimer < stuckCheckInterval)
            return;

        float movedDistance = Vector3.Distance(transform.position, lastCheckedPosition);
        stuckTimer = 0f;
        lastCheckedPosition = transform.position;

        bool stillFarFromGoal = agent.hasPath
            && Vector3.Distance(transform.position, agent.destination) > agent.stoppingDistance + 0.5f;

        if (movedDistance < stuckDistanceThreshold && stillFarFromGoal)
        {
            if (state == State.Patrolling)
                GoToRandomPatrolPoint();
            else
                EnterRedirectState();
        }
    }

    private void EnterChaseState()
    {
        state = State.Chasing;
        agent.isStopped = false;
        agent.speed = chaseSpeed;

        agent.stoppingDistance = 0f;
        isWaitingAtPoint = false;
        lastChaseDestination = Vector3.positiveInfinity;
        AudioManager.Instance.StartMonsterRoar(voiceSource, roarClips);

        stuckTimer = 0f;
        lastCheckedPosition = transform.position;

        SetChaseActive(true);
    }

    private void EnterPatrolState()
    {
        state = State.Patrolling;
        agent.isStopped = false;
        agent.stoppingDistance = 1f;
        agent.speed = patrolSpeed;

        lastPlayerInteractionTime = Time.time;
        GoToRandomPatrolPoint();
        AudioManager.Instance.StopMonsterRoar(voiceSource);
        SetChaseActive(false);
    }

    private void EnterRedirectState()
    {
        state = State.Redirecting;
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.stoppingDistance = 1f;
        isWaitingAtPoint = false;
        SetChaseActive(false);

        if (TryGetRandomPatrolPoint(out Vector3 point))
            agent.SetDestination(point);
        else
            EnterPatrolState();
    }

    private void EnterApproachingState()
    {
        state = State.Approaching;
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.stoppingDistance = 0f;
        isWaitingAtPoint = false;

        agent.SetDestination(approachingPoint.position);
    }

    public void TESTEnterMiniGame()
    {
        EnterMinigameState();
    }
    private void EnterMinigameState()
    {
        AudioManager.Instance.StartHeartbeat(heartbeatClip);
        AudioManager.Instance.StartMonsterRoar(voiceSource, roarClips);

        state = State.Minigame;
        agent.isStopped = true;
        agent.ResetPath();

        if (ChaseMinigameController.Instance != null)
            ChaseMinigameController.Instance.StartMinigame(HandleMinigameWon, HandleMinigameLost);
        else
            Debug.LogError("[EnemyAI] Brak ChaseMinigameController w scenie - nie mogę uruchomić minigry.", this);
    }

    private void HandleMinigameWon()
    {
        EnterPatrolState();
    }

    private void HandleMinigameLost()
    {
        EnterCaughtState();
    }

    private void HandlePlayerRevealed()
    {
        isPlayerHidden = false;
    }

    private void EnterCaughtState()
    {
        state = State.Caught;
        agent.isStopped = true;
        agent.ResetPath();
        SetChaseActive(false);

        if (isPlayerHidden)
        {
            StartCoroutine(MoveEnemyToPlayer());
        }
        else
        {
            StartCoroutine(JumpscareCaughtSequence());
        }
    }


    private IEnumerator JumpscareCaughtSequence()
    {
        if (interactionSystem != null)
            interactionSystem.CancelHiding();

        if (playerHider != null)
        {
            playerHider.SetMovementLocked(true);
            playerHider.SetLookLocked(true);
        }

        float originalFOV = playerCamera != null ? playerCamera.fieldOfView : 0f;

        if (playerCamera != null)
        {
            yield return RotateCameraTowardsEnemy();
            yield return ZoomCamera();
        }

        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(0.1f);

        if (JumpscareController.Instance != null)
            JumpscareController.Instance.Play();
        else
            Debug.LogError("[EnemyAI] Brak JumpscareController w scenie - nie mogę odtworzyć jumpscare'a.", this);

        RequestDespawn();

        if (playerCamera != null)
            playerCamera.fieldOfView = originalFOV;

        if (playerHider != null)
        {
            playerHider.SetMovementLocked(false);
            playerHider.SetLookLocked(false);
        }
    }

    private IEnumerator RotateCameraTowardsEnemy()
    {
        Vector3 lookTarget = targetAfterCatch != null ? targetAfterCatch.position : transform.position;
        Vector3 direction = lookTarget - playerCamera.transform.position;

        if (direction.sqrMagnitude < 0.0001f)
            yield break;

        Vector3 targetEuler = Quaternion.LookRotation(direction).eulerAngles;
        Quaternion targetRotation = Quaternion.Euler(targetEuler.x, targetEuler.y, 0f);
        Quaternion startRotation = playerCamera.transform.rotation;

        float elapsed = 0f;
        while (elapsed < cameraTurnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / cameraTurnDuration;
            playerCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        playerCamera.transform.rotation = targetRotation;
    }

    private IEnumerator ZoomCamera()
    {
        float startFOV = playerCamera.fieldOfView;
        float elapsed = 0f;

        while (elapsed < cameraZoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / cameraZoomDuration;
            playerCamera.fieldOfView = Mathf.Lerp(startFOV, jumpscareZoomFOV, t);
            yield return null;
        }

        playerCamera.fieldOfView = jumpscareZoomFOV;
    }

    private IEnumerator MoveEnemyToPlayer()
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        Vector3 targetPosition = player.transform.position;

        targetPosition.y += targetHeightOffsetAfterJump;

        Vector3 direction = targetPosition - startPosition;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        float elapsed = 0f;
        float moveDuration = 0.15f;

        AudioManager.Instance.PlaySFX3D(voiceSource, boomClip, boomVolume);
        animator.SetTrigger("Attack");

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;

            float t = moveCurve.Evaluate(elapsed / moveDuration);

            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );

            transform.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                t
            );

            yield return null;
        }

        transform.SetPositionAndRotation(targetPosition, targetRotation);

        if (JumpscareController.Instance != null)
            JumpscareController.Instance.Play();
        else
            Debug.LogError("[EnemyAI] Brak JumpscareController w scenie - nie mogę odtworzyć jumpscare'a.", this);

        RequestDespawn();
    }

    private bool TryGetRandomPatrolPoint(out Vector3 point)
    {
        bool foundReachablePoint = false;
        Vector3 bestPoint = Vector3.zero;
        float bestClearance = -1f;

        for (int attempt = 0; attempt < maxPatrolPointAttempts; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            Vector3 randomPoint = patrolCenter + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (!NavMesh.SamplePosition(randomPoint, out NavMeshHit navHit, patrolRadius, navMeshAreaMask))
                continue;

            if ((navHit.position - transform.position).sqrMagnitude < minPatrolPointDistance * minPatrolPointDistance)
                continue;

            if (!NavMesh.CalculatePath(transform.position, navHit.position, navMeshAreaMask, patrolPath)
                || patrolPath.status != NavMeshPathStatus.PathComplete)
                continue;

            float clearance = NavMesh.FindClosestEdge(navHit.position, out NavMeshHit edgeHit, navMeshAreaMask)
                ? edgeHit.distance
                : float.MaxValue;

            if (clearance >= minWallClearance)
            {
                point = navHit.position;
                return true;
            }

            if (clearance > bestClearance)
            {
                bestClearance = clearance;
                bestPoint = navHit.position;
                foundReachablePoint = true;
            }
        }

        point = bestPoint;
        return foundReachablePoint;
    }

    private void GoToRandomPatrolPoint()
    {
        if (TryGetRandomPatrolPoint(out Vector3 point))
        {
            agent.SetDestination(point);
        }
        else
        {
            isWaitingAtPoint = true;
            waitTimer = waitTimeAtPoint;
        }
    }

    private Vector3 GetPlayerTargetPoint()
    {
        return playerCamera != null ? playerCamera.transform.position : player.position + Vector3.up;
    }

    private Vector3 GetPlayerSamplePoint(int index)
    {
        Vector3 head = GetPlayerTargetPoint();

        switch (index)
        {
            case 0: return head;
            case 1: return Vector3.Lerp(player.position, head, 0.5f);
            default: return player.position + Vector3.up * 0.3f;
        }
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector3 origin = eyes.position;

        if (!IsInViewCone(origin, GetPlayerTargetPoint()))
            return false;

        for (int i = 0; i < PlayerSamplePointCount; i++)
        {
            if (!IsBlocked(origin, GetPlayerSamplePoint(i), out _))
                return true;
        }

        return false;
    }

    private bool IsInViewCone(Vector3 origin, Vector3 target)
    {
        Vector3 toTarget = target - origin;

        if (toTarget.sqrMagnitude > viewRadius * viewRadius)
            return false;

        Vector3 flatToTarget = Vector3.ProjectOnPlane(toTarget, Vector3.up);
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

        return flatToTarget.sqrMagnitude <= 0.0001f ||
               Vector3.Angle(flatForward, flatToTarget) <= viewAngle * 0.5f;
    }

    private bool IsBlocked(Vector3 origin, Vector3 target, out RaycastHit blocker)
    {
        Vector3 toTarget = target - origin;
        int hitCount = Physics.RaycastNonAlloc(origin, toTarget, sightHits, toTarget.magnitude,
            obstacleLayerMask, QueryTriggerInteraction.Ignore);

        bool blocked = false;
        blocker = default;

        for (int i = 0; i < hitCount; i++)
        {
            Transform hitTransform = sightHits[i].transform;

            if (hitTransform.IsChildOf(transform) || hitTransform.IsChildOf(player))
                continue;

            if (!blocked || sightHits[i].distance < blocker.distance)
            {
                blocker = sightHits[i];
                blocked = true;
            }
        }

        return blocked;
    }

    public bool IsRunning => state == State.Chasing || state == State.Redirecting || state == State.Approaching;

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        float targetValue;

        if (IsRunning)
            targetValue = runAnimValue;
        else if (state == State.Minigame || state == State.Caught)
            targetValue = idleAnimValue;
        else if (state == State.Patrolling && isWaitingAtPoint)
            targetValue = lookBehindAnimValue;
        else
            targetValue = walkAnimValue;

        animator.SetFloat(speedParameter, targetValue, animationDamping, Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = eyes.position;
        Vector3 forward = transform.forward;
        Vector3 center = patrolAreaCenter != null ? patrolAreaCenter.position : transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, viewRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(center, patrolRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, minPatrolPointDistance);

        Gizmos.color = Color.yellow;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-viewAngle / 2f, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(viewAngle / 2f, Vector3.up);
        Gizmos.DrawRay(origin, leftRayRotation * forward * viewRadius);
        Gizmos.DrawRay(origin, rightRayRotation * forward * viewRadius);

        if (Application.isPlaying && player != null)
            DrawSightGizmos(origin);
    }

    private void DrawSightGizmos(Vector3 origin)
    {
        if (!IsInViewCone(origin, GetPlayerTargetPoint()))
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawLine(origin, GetPlayerTargetPoint());
            return;
        }

        for (int i = 0; i < PlayerSamplePointCount; i++)
        {
            Vector3 point = GetPlayerSamplePoint(i);

            if (IsBlocked(origin, point, out RaycastHit blocker))
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(origin, blocker.point);

                Gizmos.color = new Color(1f, 0.5f, 0f);
                Gizmos.DrawWireSphere(blocker.point, 0.1f);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(blocker.point,
                    $"{blocker.collider.name} [{LayerMask.LayerToName(blocker.collider.gameObject.layer)}]");
#endif
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(origin, point);
            }
        }
    }
}