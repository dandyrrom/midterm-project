using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
public class ZombieRoam : MonoBehaviour
{
    [Header("Roam Area")]
    public float roamRadius = 15f;

    [Header("Timing")]
    public float minIdleTime = 2f;
    public float maxIdleTime = 5f;

    [Header("Hearing")]
    [Tooltip("How fast the agent moves when chasing a heard noise.")]
    public float chaseSpeed = 0.5f;
    [Tooltip("How long to wait at the noise before roaming again.")]
    public float investigateTime = 3f;

    NavMeshAgent agent;
    Animator animator;

    static readonly int SpeedHash = Animator.StringToHash("Speed");

    float idleUntil;
    bool hasDestination;
    bool isChasing;
    float roamSpeed;
    float investigateUntil;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        NoiseEvents.OnNoise += HandleNoise;
    }

    void OnDisable()
    {
        NoiseEvents.OnNoise -= HandleNoise;
    }

    void Start()
    {
        roamSpeed = agent.speed;
        idleUntil = Time.time + Random.Range(minIdleTime, maxIdleTime);
    }

    void Update()
    {
        if (animator != null)
            animator.SetFloat(SpeedHash, agent.velocity.magnitude);

        if (isChasing)
        {
            UpdateChase();
            return;
        }

        UpdateRoam();
    }

    void UpdateRoam()
    {
        if (Time.time < idleUntil)
            return;

        if (!hasDestination)
        {
            if (TryPickDestination(out Vector3 destination))
            {
                agent.SetDestination(destination);
                hasDestination = true;
            }
            else
            {
                idleUntil = Time.time + Random.Range(minIdleTime, maxIdleTime);
            }

            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            hasDestination = false;
            idleUntil = Time.time + Random.Range(minIdleTime, maxIdleTime);
        }
    }

    void UpdateChase()
    {
        if (agent.pathPending)
            return;

        if (agent.remainingDistance > agent.stoppingDistance + 0.1f)
            return;

        if (investigateUntil <= 0f)
        {
            investigateUntil = Time.time + investigateTime;
            agent.ResetPath();
            return;
        }

        if (Time.time < investigateUntil)
            return;

        EndChase();
    }

    void HandleNoise(Vector3 position, float radius)
    {
        float distance = Vector3.Distance(transform.position, position);
        if (distance > radius)
            return;

        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            return;

        isChasing = true;
        hasDestination = false;
        investigateUntil = 0f;
        idleUntil = 0f;
        agent.speed = chaseSpeed;
        agent.SetDestination(hit.position);
    }

    void EndChase()
    {
        isChasing = false;
        hasDestination = false;
        investigateUntil = 0f;
        agent.speed = roamSpeed;
        agent.ResetPath();
        idleUntil = Time.time + Random.Range(minIdleTime, maxIdleTime);
    }

    bool TryPickDestination(out Vector3 destination)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 offset = Random.insideUnitCircle * roamRadius;
            Vector3 randomPoint = transform.position + new Vector3(offset.x, 0f, offset.y);

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, roamRadius, NavMesh.AllAreas))
            {
                destination = hit.position;
                return true;
            }
        }

        destination = transform.position;
        return false;
    }
}
