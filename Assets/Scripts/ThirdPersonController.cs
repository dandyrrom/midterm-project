using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(Animator), typeof(PlayerInput))]
public class ThirdPersonController : MonoBehaviour
{
    public static ThirdPersonController Player { get; private set; }

    [Header("Movement Speeds")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 5.0f;
    public float rotationSmoothTime = 0.1f;

    [Header("Stamina (weakness)")]
    public float maxStamina = 100f;
    public float staminaDrainPerSecond = 28f;
    public float staminaRegenPerSecond = 14f;
    public float exhaustedUntil = 35f;

    [Header("Noise")]
    public float walkNoise = 0.22f;
    public float runNoise = 0.85f;
    public float stepNoiseInterval = 0.38f;
    public float bumpForce = 4.5f;

    CharacterController controller;
    Animator animator;
    PlayerInput playerInput;
    Transform mainCameraTransform;

    InputAction moveAction;
    InputAction sprintAction;

    float currentAngle;
    float currentSpeed;
    float verticalVelocity;
    readonly float gravity = -9.81f;
    float nextStepNoise;
    bool exhausted;

    public float Stamina { get; private set; }
    public bool IsExhausted => exhausted;
    public bool IsSprinting { get; private set; }

    void Awake()
    {
        Player = this;
        Stamina = maxStamina;
    }

    void OnDestroy()
    {
        if (Player == this)
            Player = null;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        if (Camera.main != null)
            mainCameraTransform = Camera.main.transform;

        moveAction = playerInput.actions["Move"];
        sprintAction = playerInput.actions["Sprint"];

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (PlayerCombatResources.Instance != null && PlayerCombatResources.Instance.IsDead)
            return;

        CalculateMovement();
    }

    void CalculateMovement()
    {
        Vector2 input = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;
        bool wantsSprint = sprintAction != null && sprintAction.IsPressed();

        if (wantsSprint && Stamina > 0f && !exhausted && input.magnitude > 0.1f)
        {
            IsSprinting = true;
            Stamina = Mathf.Max(0f, Stamina - staminaDrainPerSecond * Time.deltaTime);
            if (Stamina <= 0f)
                exhausted = true;
        }
        else
        {
            IsSprinting = false;
            Stamina = Mathf.Min(maxStamina, Stamina + staminaRegenPerSecond * Time.deltaTime);
            if (exhausted && Stamina >= exhaustedUntil)
                exhausted = false;
        }

        float targetSpeed = IsSprinting ? runSpeed : walkSpeed;
        if (input.magnitude < 0.1f)
            targetSpeed = 0f;

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f);

        if (animator != null)
            animator.SetFloat("Speed", runSpeed > 0f ? currentSpeed / runSpeed : 0f);

        if (controller.isGrounded)
            verticalVelocity = -0.5f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        Vector3 moveDir = Vector3.zero;
        Transform cam = mainCameraTransform != null ? mainCameraTransform : (Camera.main != null ? Camera.main.transform : null);

        if (direction.magnitude >= 0.1f)
        {
            float cameraYaw = cam != null ? cam.eulerAngles.y : transform.eulerAngles.y;
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraYaw;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref currentAngle, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }

        Vector3 finalMovement = moveDir * targetSpeed + new Vector3(0f, verticalVelocity, 0f);
        controller.Move(finalMovement * Time.deltaTime);

        if (targetSpeed > 0.1f && Time.time >= nextStepNoise)
        {
            nextStepNoise = Time.time + (IsSprinting ? stepNoiseInterval * 0.7f : stepNoiseInterval);
            NoiseBus.Emit(transform.position, IsSprinting ? runNoise : walkNoise);
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        var knockable = hit.collider.GetComponent<KnockableProp>();
        if (knockable == null)
            return;

        var body = hit.collider.attachedRigidbody;
        if (body == null || body.isKinematic)
            return;

        Vector3 push = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
        if (push.sqrMagnitude < 0.01f)
            push = transform.forward;

        knockable.Nudge(push, bumpForce * (IsSprinting ? 1.4f : 1f));
    }
}
