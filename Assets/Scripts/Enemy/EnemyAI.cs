
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

    [Header("Jumpscare Camera (złapanie bez ukrycia)")]
    [SerializeField] private float cameraTurnDuration = 0.2f;
    [SerializeField] private float cameraZoomDuration = 0.2f;
    [SerializeField] private float jumpscareZoomFOV = 35f;
    [SerializeField] private Transform targetAfterCatch;
    private Camera playerCamera;
    private PlayerHider playerHider; 

    [Header("Patrol (losowe punkty w promieniu)")]
    [SerializeField] private Transform patrolAreaCenter;
    [SerializeField] private float patrolRadius = 15f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitTimeAtPoint = 2f; 
    [SerializeField] private float minWallClearance = 1f;
    [SerializeField] private int maxPatrolPointAttempts = 10; 

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float loseSightDelay = 3f; 
    [SerializeField] private float repathThreshold = 0.5f; 
    [SerializeField] private float stuckCheckInterval = 1f; 
    [SerializeField] private float stuckDistanceThreshold = 0.2f; 
    [SerializeField] private float catchDistance = 1f; 

    [Header("Despawn")]
    [SerializeField] private float minNoInteractionDespawnTime = 20f; 
    [SerializeField] private float maxNoInteractionDespawnTime = 40f;

    [Header("Detection")]
    [SerializeField] private float viewRadius = 10f;
    [SerializeField, Range(0f, 360f)] private float viewAngle = 90f;
    [SerializeField] private LayerMask obstacleLayerMask;

    [Header("Animation")]
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private float idleAnimValue = 0f;
    [SerializeField] private float walkAnimValue = 1f;
    [SerializeField] private float runAnimValue = 2f;
    [SerializeField] private float animationDamping = 0.15f; 

    [Header("TEST START MINI GAME")]
    public InputActionReference testStartMiniGame;

    private NavMeshAgent agent;
    private State state;
    private Vector3 patrolCenter;

    private float waitTimer;
    private bool isWaitingAtPoint;
    private float lastSeenPlayerTime = -Mathf.Infinity;
    private Vector3 lastSeenPlayerPosition;
    private Vector3 lastChaseDestination;

    private float stuckTimer;
    private Vector3 lastCheckedPosition;

    private float lastPlayerInteractionTime;
    private float noInteractionDespawnTime;
    private bool isPlayerHidden;

    public event System.Action<EnemyAI> OnDespawnRequested;
    private int navMeshAreaMask;

    public void SetNavMeshAreaMask(int mask)
    {
        navMeshAreaMask = mask;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (playerHider == null && player != null)
            playerHider = player.GetComponent<PlayerHider>();

        if (playerCamera == null)
            playerCamera = player.GetComponentInChildren<Camera>();

        patrolCenter = patrolAreaCenter != null ? patrolAreaCenter.position : transform.position;
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
                UpdateChase(canSeePlayer);
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

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            isWaitingAtPoint = true;
            waitTimer = waitTimeAtPoint;
        }
    }

    private void UpdateChase(bool canSeePlayer)
    {
        if (agent.hasPath && !agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            EnterRedirectState(); 
            return;
        }

        if (canSeePlayer)
        {
            lastSeenPlayerTime = Time.time;
            lastSeenPlayerPosition = player.position;

            if (Vector3.Distance(player.position, lastChaseDestination) > repathThreshold)
            {
                lastChaseDestination = player.position;
                agent.SetDestination(player.position);
            }
        }
        else if (Time.time - lastSeenPlayerTime > loseSightDelay)
        {
            EnterPatrolState();
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
        AudioManager.Instance.StopMonsterRoar();
        OnDespawnRequested?.Invoke(this);
    }

    private void UpdateStuckDetection()
    {
        if (state != State.Chasing && state != State.Redirecting && state != State.Approaching)
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
            EnterRedirectState();
    }

    private void EnterChaseState()
    {
        state = State.Chasing;
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.stoppingDistance = 1f;
        isWaitingAtPoint = false;
        lastSeenPlayerTime = Time.time;
        lastChaseDestination = Vector3.positiveInfinity; 
        AudioManager.Instance.StartMonsterRoar(roarClips);

        stuckTimer = 0f;
        lastCheckedPosition = transform.position;
    }

    private void EnterPatrolState()
    {
        state = State.Patrolling;
        agent.isStopped = false;
        agent.stoppingDistance = 1f;
        agent.speed = patrolSpeed;
        GoToRandomPatrolPoint();
        AudioManager.Instance.StopMonsterRoar();
    }

    private void EnterRedirectState()
    {
        state = State.Redirecting;
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.stoppingDistance = 1f;
        isWaitingAtPoint = false;

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
        AudioManager.Instance.StartMonsterRoar(roarClips);

        state = State.Minigame;
        agent.isStopped = true;
        agent.ResetPath();

        if (ChaseMinigameController.Instance != null)
            ChaseMinigameController.Instance.StartMinigame(HandleMinigameWon, HandleMinigameLost);
        else
            Debug.LogWarning("[EnemyAI] Brak ChaseMinigameController w scenie - nie mogę uruchomić minigry.", this);
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
            Debug.LogWarning("[EnemyAI] Brak JumpscareController w scenie - nie mogę odtworzyć jumpscare'a.", this);

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

        AudioManager.Instance.PlaySFX(boomClip);
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
            Debug.LogWarning("[EnemyAI] Brak JumpscareController w scenie - nie mogę odtworzyć jumpscare'a.", this);

        RequestDespawn(); 
    }

    private bool TryGetRandomPatrolPoint(out Vector3 point)
    {
        bool foundAnyNavMeshPoint = false;
        Vector3 fallbackPoint = Vector3.zero;

        for (int attempt = 0; attempt < maxPatrolPointAttempts; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            Vector3 randomPoint = patrolCenter + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (!NavMesh.SamplePosition(randomPoint, out NavMeshHit navHit, patrolRadius, navMeshAreaMask))
                continue;

            foundAnyNavMeshPoint = true;
            fallbackPoint = navHit.position;

            if (NavMesh.FindClosestEdge(navHit.position, out NavMeshHit edgeHit, navMeshAreaMask) && edgeHit.distance < minWallClearance)
                continue;

            point = navHit.position;
            return true;
        }

        if (foundAnyNavMeshPoint)
        {
            point = fallbackPoint; 
            return true;
        }

        point = Vector3.zero;
        return false;
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

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector3 toPlayer = player.position - eyes.position;
        float distance = toPlayer.magnitude;

        if (distance > viewRadius)
            return false;

        float angle = Vector3.Angle(transform.forward, toPlayer); 
        if (angle > viewAngle / 2f)
            return false;

        if (Physics.Linecast(eyes.position, player.position, obstacleLayerMask))
            return false; 

        return true;
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
            targetValue = idleAnimValue; 
        else
            targetValue = walkAnimValue;

        animator.SetFloat(speedParameter, targetValue, animationDamping, Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = eyes != null ? eyes.position : transform.position;
        Vector3 forward = transform.forward; 
        Vector3 center = patrolAreaCenter != null ? patrolAreaCenter.position : transform.position;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center, patrolRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, viewRadius);

        Gizmos.color = Color.red;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-viewAngle / 2f, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(viewAngle / 2f, Vector3.up);
        Gizmos.DrawRay(origin, leftRayRotation * forward * viewRadius);
        Gizmos.DrawRay(origin, rightRayRotation * forward * viewRadius);
    }
}
