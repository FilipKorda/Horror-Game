using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference sprintAction;
    [SerializeField] private InputActionReference slowWalkAction;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 6.5f;
    [SerializeField] private float slowWalkSpeed = 1.5f;
    [SerializeField] private float acceleration = 12f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -19.6f;
    [SerializeField] private float groundedStickForce = -2f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivityX = 0.1f;
    [SerializeField] private float mouseSensitivityY = 0.1f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private bool lockCursor = true;

    private CharacterController controller;
    [Header("Properties")]
    [SerializeField] private SnapAudioListener snapAudioListener;

    private Vector3 currentVelocity;
    private float verticalVelocity;
    private float pitch;

    private bool sprintHeld;
    private bool slowWalkHeld;

    private bool movementLocked;
    private bool lookLocked;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        lookAction.action.Enable();

        sprintAction.action.Enable();
        sprintAction.action.performed += OnSprintPerformed;
        sprintAction.action.canceled += OnSprintCanceled;

        slowWalkAction.action.Enable();
        slowWalkAction.action.performed += OnSlowWalkPerformed;
        slowWalkAction.action.canceled += OnSlowWalkCanceled;

        snapAudioListener.SnapThisAudioListenerToPlayer();
    }

    private void OnDisable()
    {
        sprintAction.action.performed -= OnSprintPerformed;
        sprintAction.action.canceled -= OnSprintCanceled;

        slowWalkAction.action.performed -= OnSlowWalkPerformed;
        slowWalkAction.action.canceled -= OnSlowWalkCanceled;

        moveAction.action.Disable();
        lookAction.action.Disable();
        sprintAction.action.Disable();
        slowWalkAction.action.Disable();
    }

    private void OnSprintPerformed(InputAction.CallbackContext ctx) => sprintHeld = true;
    private void OnSprintCanceled(InputAction.CallbackContext ctx) => sprintHeld = false;
    private void OnSlowWalkPerformed(InputAction.CallbackContext ctx) => slowWalkHeld = true;
    private void OnSlowWalkCanceled(InputAction.CallbackContext ctx) => slowWalkHeld = false;

    private void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        if (!lookLocked)
            HandleMouseLook();

        if (!movementLocked)
            HandleMovement();
    }

    public void SetMovementLocked(bool locked) => movementLocked = locked;

    public void SetLookLocked(bool locked) => lookLocked = locked;

    public bool IsSprinting => sprintHeld;
    public bool IsSlowWalking => slowWalkHeld;
    public bool IsMovementLocked => movementLocked;
    public float WalkSpeed => walkSpeed;
    public float SprintSpeed => sprintSpeed;
    public float SlowWalkSpeed => slowWalkSpeed;

    private void HandleMouseLook()
    {
        Vector2 look = lookAction.action.ReadValue<Vector2>();

        float mouseX = look.x * mouseSensitivityX;
        float mouseY = look.y * mouseSensitivityY * (invertY ? 1f : -1f);

        transform.Rotate(Vector3.up * mouseX);

        pitch += mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void HandleMovement()
    {
        bool grounded = controller.isGrounded;

        if (grounded && verticalVelocity < 0f)
            verticalVelocity = groundedStickForce;

        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
        Vector3 inputDir = (transform.right * moveInput.x + transform.forward * moveInput.y);
        inputDir = Vector3.ClampMagnitude(inputDir, 1f);

        float targetSpeed = walkSpeed;
        if (sprintHeld && moveInput.y > 0f)
            targetSpeed = sprintSpeed;
        else if (slowWalkHeld)
            targetSpeed = slowWalkSpeed;

        Vector3 targetVelocity = inputDir * targetSpeed;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, acceleration * Time.deltaTime);

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 motion = currentVelocity + Vector3.up * verticalVelocity;
        controller.Move(motion * Time.deltaTime);
    }
}