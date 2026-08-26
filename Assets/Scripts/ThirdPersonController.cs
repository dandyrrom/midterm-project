using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(Animator), typeof(PlayerInput))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Locomotion")]
    [Tooltip("On: Mixamo root motion moves the body. NavMeshAgent only follows the path. Use on Warzombie.")]
    public bool useRootMotion = false;

    [Header("Movement Speeds")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 5.0f;
    [Tooltip("Hold Ctrl to sneak. Slower than walk; zombies cannot hear these steps.")]
    public float sneakSpeed = 1.2f;
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
    [Tooltip("How long she stands still after Hit (H).")]
    public float hitPause = 0.7f;
    [Tooltip("If Space is pressed a moment before she is counted as grounded, still accept the jump.")]
    public float jumpBufferTime = 0.2f;
    [Tooltip("How much WASD steers her in the air. 1 = same as walking.")]
    [Range(0f, 1f)]
    public float airControl = 1f;

    [Header("Jump Noise")]
    [Tooltip("Played when she lands after a Space jump.")]
    public AudioClip jumpLandClip;
    [Tooltip("How far the landing noise can travel for hearing AI.")]
    public float jumpLandNoiseRadius = 15f;

    [Header("Walk Footsteps")]
    [Tooltip("One-shot clip for a single walk step.")]
    public AudioClip walkFootstepClip;
    [Tooltip("Seconds between walk steps.")]
    public float walkStepInterval = 0.5f;
    [Tooltip("How far a walk step can be heard by zombies.")]
    public float walkNoiseRadius = 5f;

    [Header("Run Footsteps")]
    [Tooltip("One-shot clip for a single run step.")]
    public AudioClip runFootstepClip;
    [Tooltip("Seconds between run steps.")]
    public float runStepInterval = 0.32f;
    [Tooltip("How far a run step can be heard by zombies.")]
    public float runNoiseRadius = 14f;

    [Header("Sneak Footsteps")]
    [Tooltip("Optional soft one-shot for the player only. Leave empty for fully silent sneak.")]
    public AudioClip sneakFootstepClip;
    [Tooltip("Seconds between sneak steps (player-facing audio only).")]
    public float sneakStepInterval = 0.65f;

    [Header("Health")]
    [Tooltip("Damage used when testing hits with H.")]
    public int debugHitDamage = 5;

    [Header("Hurt Audio")]
    [Tooltip("Played when she takes damage (H or zombie mid-attack).")]
    public AudioClip hitClip;

    CharacterController controller;
    PlayerHealth health;
    Animator animator;
    PlayerInput playerInput;
    AudioSource audioSource;
    Transform mainCameraTransform;

    InputAction moveAction;
    InputAction sprintAction;
    InputAction sneakAction;
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
    float nextWalkStepTime;
    float nextRunStepTime;
    float nextSneakStepTime;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int SneakHash = Animator.StringToHash("Sneak");
    static readonly int JumpHash = Animator.StringToHash("Jump");
    static readonly int HitHash = Animator.StringToHash("Hit");
    static readonly int DieHash = Animator.StringToHash("Die");

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        audioSource = GetComponent<AudioSource>();

        health = GetComponent<PlayerHealth>();

        if (health != null)
            health.OnDied += HandleDeath;

        if (Camera.main != null)
            mainCameraTransform = Camera.main.transform;

        moveAction = playerInput.actions["Move"];
        sprintAction = playerInput.actions["Sprint"];
        sneakAction = playerInput.actions.FindAction("Sneak");
        jumpAction = playerInput.actions["Jump"];

        Cursor.lockState = CursorLockMode.Locked;

    }


    void OnDestroy()
    {
        if (health != null)
            health.OnDied -= HandleDeath;
    }

    void Update()
    {
        if (Keyboard.current != null && !isDead)
        {
            if (Keyboard.current.hKey.wasPressedThisFrame)
                TakeHit(debugHitDamage);
            if (Keyboard.current.kKey.wasPressedThisFrame)
                health?.Kill();
        }

        CalculateMovement();
    }

    public void TakeHit(int damage)
    {
        if (isDead || health == null)
            return;

        // Optional i-frames between hits. Use 0 if every mid-attack must always land.
        if (Time.time < hitUntil)
            return;

        if (!health.TakeDamage(damage))
            return; // died: OnDied handles death anim

        hitUntil = Time.time + hitPause;

        if (audioSource != null && hitClip != null)
            audioSource.PlayOneShot(hitClip);

        if (animator != null)
        {
            animator.ResetTrigger(HitHash);
            animator.SetTrigger(HitHash); // interrupt/replay react immediately
        }
    }

    void HandleDeath()
    {
        if (isDead)
            return;
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
            {
                animator.SetFloat(SpeedHash, 0f);
                animator.SetBool(SneakHash, false);
            }
            return;
        }

        Vector2 input = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;
        bool wantsSneak = sneakAction != null && sneakAction.IsPressed();
        if (!wantsSneak && Keyboard.current != null)
            wantsSneak = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
        // Ctrl wins over Shift so she stays quiet if both are held.
        bool isSneaking = wantsSneak;
        bool isSprinting = !isSneaking && sprintAction != null && sprintAction.IsPressed();
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
            if (jumpedFromHop)
            {
                if (pauseOnLanding)
                    landLockedUntil = Time.time + landPause;
                EmitJumpLandNoise();
            }
            jumpedFromHop = false;
        }

        bool crouchLocked = pauseDuringCrouch && jumpPending;
        bool landLocked = pauseOnLanding && Time.time < landLockedUntil;

        float targetSpeed = isSneaking ? sneakSpeed : (isSprinting ? runSpeed : walkSpeed);
        if (input.magnitude < 0.1f || isHitLocked || crouchLocked || landLocked)
            targetSpeed = 0f;
        else if (jumpAirborne)
            targetSpeed *= airControl;

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f);

        bool isMoving = input.magnitude >= 0.1f && targetSpeed > 0.1f;
        bool blocked = isHitLocked || crouchLocked || landLocked;
        bool playSneakAnim = isSneaking && isMoving && grounded && !blocked && !jumpAirborne;

        if (animator != null)
        {
            animator.SetFloat(SpeedHash, runSpeed > 0f ? currentSpeed / runSpeed : 0f);
            animator.SetBool(SneakHash, playSneakAnim);
        }

        Vector3 moveDir = Vector3.zero;
        Transform cam = mainCameraTransform != null ? mainCameraTransform : (Camera.main != null ? Camera.main.transform : null);

        if (!isHitLocked && !crouchLocked && !landLocked && direction.magnitude >= 0.1f)
        {
            float cameraYaw = cam != null ? cam.eulerAngles.y : transform.eulerAngles.y;
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraYaw;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref currentAngle, rotationSmoothTime);

            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }

        Vector3 finalMovement = moveDir * targetSpeed + new Vector3(0f, verticalVelocity, 0f);
        controller.Move(finalMovement * Time.deltaTime);
        UpdateWalkFootsteps(grounded, isMoving, isSprinting, isSneaking, blocked);
        UpdateRunFootsteps(grounded, isMoving, isSprinting, blocked);
        UpdateSneakFootsteps(grounded, isMoving, isSneaking, blocked);
    }

    void EmitJumpLandNoise()
    {
        if (audioSource != null && jumpLandClip != null)
            audioSource.PlayOneShot(jumpLandClip);

        NoiseEvents.Emit(transform.position, jumpLandNoiseRadius);
    }

    void UpdateWalkFootsteps(bool grounded, bool isMoving, bool isSprinting, bool isSneaking, bool blocked)
    {
        if (!grounded || !isMoving || isSprinting || isSneaking || blocked)
            return;

        if (Time.time < nextWalkStepTime)
            return;

        nextWalkStepTime = Time.time + walkStepInterval;

        if (audioSource != null && walkFootstepClip != null)
            audioSource.PlayOneShot(walkFootstepClip);

        NoiseEvents.Emit(transform.position, walkNoiseRadius);
    }

    void UpdateRunFootsteps(bool grounded, bool isMoving, bool isSprinting, bool blocked)
    {
        if (!grounded || !isMoving || !isSprinting || blocked)
            return;

        if (Time.time < nextRunStepTime)
            return;

        nextRunStepTime = Time.time + runStepInterval;

        if (audioSource != null && runFootstepClip != null)
            audioSource.PlayOneShot(runFootstepClip);

        NoiseEvents.Emit(transform.position, runNoiseRadius);
    }

    void UpdateSneakFootsteps(bool grounded, bool isMoving, bool isSneaking, bool blocked)
    {
        // Player-facing only — never emits NoiseEvents (zombies cannot hear sneak).
        if (!grounded || !isMoving || !isSneaking || blocked)
            return;

        if (sneakFootstepClip == null)
            return;

        if (Time.time < nextSneakStepTime)
            return;

        nextSneakStepTime = Time.time + sneakStepInterval;

        if (audioSource != null)
            audioSource.PlayOneShot(sneakFootstepClip, 0.35f);
    }
}
