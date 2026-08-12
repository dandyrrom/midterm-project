using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(Animator), typeof(PlayerInput))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 5.0f;
    public float rotationSmoothTime = 0.1f;

    private CharacterController controller;
    private Animator animator;
    private PlayerInput playerInput;
    private Transform mainCameraTransform;

    private InputAction moveAction;
    private InputAction sprintAction;

    private float currentAngle;
    private float currentSpeed;
    private float verticalVelocity;
    private readonly float gravity = -9.81f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        mainCameraTransform = Camera.main.transform;

        // Fetching the exact actions from your configured Input System
        moveAction = playerInput.actions["Move"];
        sprintAction = playerInput.actions["Sprint"];

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        CalculateMovement();
    }

    void CalculateMovement()
    {
        // 1. Read input values
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;
        bool isSprinting = sprintAction.IsPressed();

        // 2. Determine animation and movement speed
        float targetSpeed = isSprinting ? runSpeed : walkSpeed;
        if (input.magnitude == 0) targetSpeed = 0f;

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f);

        // This sends the speed value back to your Locomotion Blend Tree!
        animator.SetFloat("Speed", currentSpeed / runSpeed);

        // 3. Gravity
        if (controller.isGrounded)
        {
            verticalVelocity = -0.5f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // 4. Calculate rotation and movement relative to the camera
        Vector3 moveDir = Vector3.zero;

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + mainCameraTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref currentAngle, rotationSmoothTime);

            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }

        // 5. Apply movement
        Vector3 finalMovement = moveDir * targetSpeed + new Vector3(0.0f, verticalVelocity, 0.0f);
        controller.Move(finalMovement * Time.deltaTime);
    }
}