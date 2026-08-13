using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(Animator), typeof(PlayerInput))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 5.0f;
    public float turnSpeed = 120f;

    private CharacterController controller;
    private Animator animator;
    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction sprintAction;

    private float currentSpeed;
    private float verticalVelocity;
    private readonly float gravity = -9.81f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();

        moveAction = playerInput.actions["Move"];
        sprintAction = playerInput.actions["Sprint"];

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        CalculateMovement();
    }

    void CalculateMovement()
    {
        Vector2 input = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        bool isSprinting = sprintAction != null && sprintAction.IsPressed();

        float turn = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.qKey.isPressed)
                turn -= 1f;
            if (Keyboard.current.eKey.isPressed)
                turn += 1f;
        }

        if (Mathf.Abs(turn) > 0.01f)
            transform.Rotate(0f, turn * turnSpeed * Time.deltaTime, 0f);

        float targetSpeed = isSprinting ? runSpeed : walkSpeed;
        if (input.sqrMagnitude < 0.01f)
            targetSpeed = 0f;

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 10f);

        if (animator != null)
            animator.SetFloat("Speed", runSpeed > 0f ? currentSpeed / runSpeed : 0f);

        if (controller.isGrounded)
            verticalVelocity = -0.5f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        Vector3 moveDir = transform.right * input.x + transform.forward * input.y;
        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        Vector3 finalMovement = moveDir * targetSpeed + new Vector3(0f, verticalVelocity, 0f);
        controller.Move(finalMovement * Time.deltaTime);
    }
}
