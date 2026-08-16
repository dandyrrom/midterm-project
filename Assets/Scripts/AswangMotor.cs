using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator), typeof(CapsuleCollider))]
public class AswangMotor : MonoBehaviour
{
    [Header("Speeds")]
    public float walkSpeed = 1.4f;
    public float runSpeed = 3.5f;

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
        agent.speed = walkSpeed;
        agent.acceleration = 8f;
        agent.stoppingDistance = 1.6f;
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
                    "AswangMotor: Zombieguy is not on a NavMesh. Bake a NavMeshSurface in with env1 (Window > AI > Navigation), then try again.",
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

        float planarSpeed = new Vector3(agent.velocity.x, 0f, agent.velocity.z).magnitude;
        float denom = runSpeed > 0.01f ? runSpeed : 1f;
        animator.SetFloat(SpeedHash, Mathf.Clamp01(planarSpeed / denom));
    }
}
