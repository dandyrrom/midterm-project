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
    bool isDead;
    float hitUntil;

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

        hitUntil = Time.time + 0.7f;
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
                verticalVelocity = -0.5f;
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

        float targetSpeed = isSprinting ? runSpeed : walkSpeed;
        if (input.magnitude < 0.1f || isHitLocked)
            targetSpeed = 0f;

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f);

        if (animator != null)
            animator.SetFloat(SpeedHash, runSpeed > 0f ? currentSpeed / runSpeed : 0f);

        if (controller.isGrounded)
        {
            verticalVelocity = -0.5f;
            bool jumpPressed = jumpAction != null && jumpAction.WasPressedThisFrame();
            if (jumpPressed && !isHitLocked)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (animator != null)
                    animator.SetTrigger(JumpHash);
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 moveDir = Vector3.zero;
        Transform cam = mainCameraTransform != null ? mainCameraTransform : (Camera.main != null ? Camera.main.transform : null);

        if (!isHitLocked && direction.magnitude >= 0.1f)
        {
            float cameraYaw = cam != null ? cam.eulerAngles.y : transform.eulerAngles.y;
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraYaw;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref currentAngle, rotationSmoothTime);

            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }

        Vector3 finalMovement = moveDir * targetSpeed + new Vector3(0f, verticalVelocity, 0f);
        controller.Move(finalMovement * Time.deltaTime);
    }
}
