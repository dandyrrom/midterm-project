using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator), typeof(CapsuleCollider))]
public class AswangMotor : MonoBehaviour
{
    [Header("Speeds")]
    [Tooltip("Must match how fast the Zombie Walk clip looks. Too fast = sliding/pushed look.")]
    public float walkSpeed = 1.05f;
    public float runSpeed = 3.2f;
    [Tooltip("How quickly Speed blends between idle/walk/run in the Animator.")]
    public float animatorDampTime = 0.2f;

    [Header("Play-test")]
    [Tooltip("Press G in Play Mode to path to mc-Peasant Girl (needs a baked NavMesh).")]
    public bool enableGoToPlayerHotkey = true;

    NavMeshAgent agent;
    Animator animator;
    CapsuleCollider bodyCollider;
    static readonly int SpeedHash = Animator.StringToHash("Speed");
    bool warnedOffMesh;

    public bool HasPath => agent != null && agent.hasPath;
    public bool HasArrived
    {
        get
        {
            if (agent == null || !agent.isOnNavMesh)
                return true;
            if (agent.pathPending)
                return false;
            if (agent.remainingDistance > agent.stoppingDistance)
                return false;
            return !agent.hasPath || agent.velocity.sqrMagnitude < 0.05f;
        }
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        bodyCollider = GetComponent<CapsuleCollider>();

        // NavMeshAgent moves the body; clips must stay in-place (no root motion).
        animator.applyRootMotion = false;

        agent.speed = walkSpeed;
        agent.acceleration = 2.5f;
        agent.angularSpeed = 90f;
        agent.stoppingDistance = 1.6f;
        agent.autoBraking = true;
        agent.updateRotation = true;
        agent.updateUpAxis = true;
        SyncBodyCollider();
    }

    void SyncBodyCollider()
    {
        if (bodyCollider == null || agent == null)
            return;

        // Solid collider so CharacterController cannot walk through the aswang.
        // NavMeshAgent alone does not block the player.
        bodyCollider.isTrigger = false;
        bodyCollider.direction = 1; // Y-axis
        bodyCollider.height = agent.height;
        bodyCollider.radius = agent.radius;
        bodyCollider.center = new Vector3(0f, agent.height * 0.5f, 0f);
    }

    void Update()
    {
        if (enableGoToPlayerHotkey && Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
            GoToPlayer(run: false);

        UpdateAnimatorSpeed();
    }

    public void SetDestination(Vector3 worldPosition, bool run = false)
    {
        if (agent == null)
            return;

        if (!agent.isOnNavMesh)
        {
            if (!warnedOffMesh)
            {
                Debug.LogWarning(
                    "AswangMotor: Zombieguy is not on a NavMesh. Bake a NavMeshSurface in with env1, then try again.",
                    this);
                warnedOffMesh = true;
            }
            return;
        }

        agent.isStopped = false;
        agent.speed = run ? runSpeed : walkSpeed;
        agent.SetDestination(worldPosition);
    }

    public void GoToPlayer(bool run = false)
    {
        GameObject player = GameObject.Find("mc-Peasant Girl");
        if (player == null)
        {
            Debug.LogWarning("AswangMotor: Could not find mc-Peasant Girl.", this);
            return;
        }

        SetDestination(player.transform.position, run);
    }

    public void StopMoving()
    {
        if (agent == null || !agent.isOnNavMesh)
            return;

        agent.ResetPath();
        agent.isStopped = true;
        if (animator != null)
            animator.SetFloat(SpeedHash, 0f);
    }

    void UpdateAnimatorSpeed()
    {
        if (animator == null || agent == null)
            return;

        // Blend tree: 0 = idle, 0.5 = walk, 1 = run.
        // Map real agent speed onto those anchors so walk uses the full walk clip,
        // not a half-idle blend (which looks like he is being pushed).
        float planarSpeed = new Vector3(agent.velocity.x, 0f, agent.velocity.z).magnitude;
        float speedParam;
        if (planarSpeed < 0.05f)
            speedParam = 0f;
        else if (planarSpeed <= walkSpeed)
            speedParam = Mathf.Lerp(0.35f, 0.5f, Mathf.InverseLerp(0.05f, walkSpeed, planarSpeed));
        else
            speedParam = Mathf.Lerp(0.5f, 1f, Mathf.InverseLerp(walkSpeed, runSpeed, planarSpeed));

        animator.SetFloat(SpeedHash, speedParam, animatorDampTime, Time.deltaTime);
    }
}
