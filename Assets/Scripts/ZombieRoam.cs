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

    NavMeshAgent agent;
    Animator animator;

    static readonly int SpeedHash = Animator.StringToHash("Speed");

    float idleUntil;
    bool hasDestination;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        idleUntil = Time.time + Random.Range(minIdleTime, maxIdleTime);
    }

    void Update()
    {
        if (animator != null)
            animator.SetFloat(SpeedHash, agent.velocity.magnitude);

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
