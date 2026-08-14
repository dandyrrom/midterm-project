using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(Animator), typeof(PlayerInput))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 5.0f;
    public float rotationSmoothTime = 0.1f;

    [Header("Jump")]
    public float jumpHeight = 1.2f;
    [Tooltip("How long she crouches in place before the hop. Raise this for a longer pause.")]
    public float jumpTakeoffDelay = 0.4f;
    [Tooltip("If on, WASD is ignored during the crouch pause. After she leaves the ground she can move again.")]
    public bool pauseDuringCrouch = true;
    [Tooltip("How long she stands still after landing.")]
    public float landPause = 0.25f;
    [Tooltip("If on, WASD is ignored for Land Pause seconds after she hits the ground.")]
    public bool pauseOnLanding = true;
    [Tooltip("How long she stands still after Hit (H). Raise this if walk/run starts before the hit clip ends.")]
    public float hitPause = 0.7f;
    [Tooltip("If Space is pressed a moment before she is counted as grounded, still accept the jump.")]
    public float jumpBufferTime = 0.2f;
    [Tooltip("How much WASD steers her in the air. 1 = same as walking.")]
    [Range(0f, 1f)]
    public float airControl = 1f;

    CharacterController controller;
    Animator animator;
    PlayerInput playerInput;
    Transform mainCameraTransform;

    InputAction moveAction;
    InputAction sprintAction;
    InputAction jumpAction;

    float currentAngle;
    float currentSpeed;
    float verticalVelocity;
    readonly float gravity = -9.81f;
    readonly float groundedStick = -2f;
    bool isDead;
    float hitUntil;
    bool jumpPending;
    float jumpTakeoffAt;
    float jumpBufferUntil;
    bool jumpAirborne;
    bool jumpedFromHop;
    float landLockedUntil;
    bool wasMoveLocked;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int JumpHash = Animator.StringToHash("Jump");
    static readonly int HitHash = Animator.StringToHash("Hit");
    static readonly int DieHash = Animator.StringToHash("Die");

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        if (Camera.main != null)
            mainCameraTransform = Camera.main.transform;

        moveAction = playerInput.actions["Move"];
        sprintAction = playerInput.actions["Sprint"];
        jumpAction = playerInput.actions["Jump"];

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (Keyboard.current != null && !isDead)
        {
            if (Keyboard.current.hKey.wasPressedThisFrame)
                TriggerHit();
            if (Keyboard.current.kKey.wasPressedThisFrame)
                TriggerDeath();
        }

        CalculateMovement();
    }

    void TriggerHit()
    {
        if (Time.time < hitUntil)
            return;

        hitUntil = Time.time + hitPause;
        if (animator != null)
            animator.SetTrigger(HitHash);
    }

    void TriggerDeath()
    {
        isDead = true;
        if (animator != null)
            animator.SetTrigger(DieHash);
    }

    void CalculateMovement()
    {
        if (isDead)
        {
            if (controller.isGrounded)
                verticalVelocity = groundedStick;
            else
                verticalVelocity += gravity * Time.deltaTime;

            controller.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
            if (animator != null)
                animator.SetFloat(SpeedHash, 0f);
            return;
        }

        Vector2 input = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;
        bool isSprinting = sprintAction != null && sprintAction.IsPressed();
        bool isHitLocked = Time.time < hitUntil;
        bool grounded = controller.isGrounded;

        if (jumpAction != null && jumpAction.WasPressedThisFrame() && !isHitLocked)
            jumpBufferUntil = Time.time + jumpBufferTime;

        bool wantsJump = Time.time < jumpBufferUntil;
        if (!jumpPending && !jumpAirborne && wantsJump && grounded && !isHitLocked && Time.time >= landLockedUntil)
        {
            jumpPending = true;
            jumpTakeoffAt = Time.time + jumpTakeoffDelay;
            jumpBufferUntil = 0f;
            if (animator != null)
            {
                animator.ResetTrigger(JumpHash);
                animator.SetTrigger(JumpHash);
            }
        }

        if (jumpPending && Time.time >= jumpTakeoffAt)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpPending = false;
            jumpAirborne = true;
            jumpedFromHop = true;
        }
        else if (grounded && !jumpAirborne)
        {
            verticalVelocity = groundedStick;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        if (jumpAirborne && grounded && verticalVelocity <= 0f && !jumpPending)
        {
            jumpAirborne = false;
            if (pauseOnLanding && jumpedFromHop)
                landLockedUntil = Time.time + landPause;
            jumpedFromHop = false;
        }

        bool crouchLocked = pauseDuringCrouch && jumpPending;
        bool landLocked = pauseOnLanding && Time.time < landLockedUntil;
        bool moveLocked = isHitLocked || crouchLocked || landLocked;

        float targetSpeed = isSprinting ? runSpeed : walkSpeed;
        if (input.magnitude < 0.1f || moveLocked)
            targetSpeed = 0f;
        else if (jumpAirborne)
            targetSpeed *= airControl;

        if (moveLocked)
            currentSpeed = 0f;
        else if (wasMoveLocked && input.magnitude >= 0.1f)
            currentSpeed = targetSpeed;
        else
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f);

        wasMoveLocked = moveLocked;

        if (animator != null)
            animator.SetFloat(SpeedHash, runSpeed > 0f ? currentSpeed / runSpeed : 0f);

        Vector3 moveDir = Vector3.zero;
        Transform cam = mainCameraTransform != null ? mainCameraTransform : (Camera.main != null ? Camera.main.transform : null);

        if (!moveLocked && direction.magnitude >= 0.1f)
        {
            float cameraYaw = cam != null ? cam.eulerAngles.y : transform.eulerAngles.y;
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraYaw;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref currentAngle, rotationSmoothTime);

            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }

        Vector3 finalMovement = moveDir * currentSpeed + new Vector3(0f, verticalVelocity, 0f);
        controller.Move(finalMovement * Time.deltaTime);
    }
}
