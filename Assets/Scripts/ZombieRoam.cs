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
    [Tooltip("How long one attack swing lasts before deciding to continue or stop.")]
    public float attackDuration = 1.8f;

    [Header("Attack")]
    [Tooltip("How close the MC must be to keep getting attacked.")]
    public float attackRange = 1.8f;
    [Tooltip("Damage dealt each successful attack.")]
    public int attackDamage = 5;
    [Tooltip("When during the swing damage + MC react fire. 0.5 = middle.")]
    [Range(0.1f, 0.9f)]
    public float attackHitNormalized = 0.45f;

    [Header("Target")]
    public Transform player;

    [Header("Audio")]
    [Tooltip("Played each time an attack swing starts.")]
    public AudioClip attackClip;

    [Tooltip("Looped while chasing a heard noise.")]
    public AudioClip runClip;

    [Tooltip("Looped while idle/walking (roam).")]
    public AudioClip roamClip;

    NavMeshAgent agent;
    Animator animator;
    ThirdPersonController playerController;
    float attackStartTime;
    bool hasDealtDamageThisSwing;
    AudioSource audioSource;
    Vector3 attackAnchorPosition;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int AttackHash = Animator.StringToHash("Attack");

    float idleUntil;
    bool hasDestination;
    bool isChasing;
    float roamSpeed;
    float investigateUntil;
    bool isAttacking;
    float attackUntil;
    ZombieHealth zombieHealth;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponentInChildren<AudioSource>();

        zombieHealth = GetComponent<ZombieHealth>();
    }

    void PlayAttackAudio()
    {
        if (audioSource == null || attackClip == null)
            return;

        audioSource.clip = attackClip;
        audioSource.loop = false;
        audioSource.Play();
    }

    void StopAttackAudio()
    {
        if (audioSource == null)
            return;

        if (audioSource.clip == attackClip && audioSource.isPlaying)
            audioSource.Stop();
    }

    void PlayRunAudio()
    {
        if (audioSource == null || runClip == null)
            return;

        if (audioSource.isPlaying && audioSource.clip == runClip)
            return;

        audioSource.clip = runClip;
        audioSource.loop = true;
        audioSource.Play();
    }

    void StopRunAudio()
    {
        if (audioSource == null)
            return;

        if (audioSource.clip == runClip && audioSource.isPlaying)
            audioSource.Stop();

        audioSource.loop = false;
    }

    void PlayRoamAudio()
    {
        if (audioSource == null || roamClip == null)
            return;

        if (audioSource.isPlaying && audioSource.clip == roamClip)
            return;

        audioSource.clip = roamClip;
        audioSource.loop = true;
        audioSource.Play();
    }

    void StopRoamAudio()
    {
        if (audioSource == null)
            return;

        if (audioSource.clip == roamClip && audioSource.isPlaying)
            audioSource.Stop();

        if (!isChasing && !isAttacking)
            audioSource.loop = false;
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

        if (player == null)
        {
            var mc = FindAnyObjectByType<ThirdPersonController>();
            if (mc != null)
                player = mc.transform;
        }

        if (player != null)
            playerController = player.GetComponent<ThirdPersonController>();

        PlayRoamAudio();
    }

    void Update()
    {
        if (zombieHealth != null && zombieHealth.IsDead)
            return;

        if (animator != null && !isAttacking)
            animator.SetFloat(SpeedHash, agent.velocity.magnitude);

        if (isAttacking)
        {
            UpdateAttack();
            return;
        }

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

        StartAttack();
    }

    void StartAttack()
    {
        if (isAttacking)
            return;

        isAttacking = true;
        isChasing = false;
        hasDestination = false;
        investigateUntil = 0f;
        hasDealtDamageThisSwing = false;

        agent.ResetPath();
        agent.velocity = Vector3.zero;
        agent.speed = roamSpeed;

        attackAnchorPosition = transform.position;
        agent.isStopped = true;
        agent.updatePosition = false;
        agent.updateRotation = false;

        if (player != null)
        {
            Vector3 look = player.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(look);
        }

        if (animator != null)
        {
            animator.SetFloat(SpeedHash, 0f);
            animator.SetTrigger(AttackHash);
        }

        StopRoamAudio();
        StopRunAudio();
        PlayAttackAudio();

        attackStartTime = Time.time;
        attackUntil = Time.time + attackDuration;
    }

    /// <summary>
    /// Blind zombies still feel contact — immediate attack even if she was sneaking.
    /// </summary>
    public void NotifyTouchedByPlayer()
    {
        if (isAttacking)
            return;

        StartAttack();
    }

    void UpdateAttack()
    {
        transform.position = attackAnchorPosition;

        if (player != null && IsPlayerInAttackRange())
        {
            Vector3 look = player.position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(look),
                    Time.deltaTime * 5f);
        }

        if (!hasDealtDamageThisSwing &&
            Time.time >= attackStartTime + attackDuration * attackHitNormalized)
        {
            if (IsPlayerInAttackRange())
                DealAttackDamage();

            hasDealtDamageThisSwing = true;
        }

        if (Time.time < attackUntil)
            return;

        if (IsPlayerInAttackRange())
        {
            hasDealtDamageThisSwing = false;
            attackStartTime = Time.time;
            attackUntil = Time.time + attackDuration;

            if (animator != null)
                animator.SetTrigger(AttackHash);

            PlayAttackAudio();
            return;
        }

        isAttacking = false;
        StopAttackAudio();
        EndChase();
    }

    void DealAttackDamage()
    {
        if (playerController == null)
            return;

        playerController.TakeHit(attackDamage);
    }

    bool IsPlayerInAttackRange()
    {
        if (player == null)
            return false;

        float distance = Vector3.Distance(transform.position, player.position);
        return distance <= attackRange;
    }

    void HandleNoise(Vector3 position, float radius)
    {
        if (zombieHealth != null && zombieHealth.IsDead)
            return;

        float distance = Vector3.Distance(transform.position, position);
        if (distance > radius)
            return;

        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            return;

        StopRoamAudio();

        isChasing = true;
        hasDestination = false;
        investigateUntil = 0f;
        idleUntil = 0f;
        agent.isStopped = false;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.speed = chaseSpeed;
        agent.SetDestination(hit.position);

        PlayRunAudio();
    }

    void EndChase()
    {
        isChasing = false;
        hasDestination = false;
        investigateUntil = 0f;
        StopAttackAudio();
        StopRunAudio();
        PlayRoamAudio();
        agent.speed = roamSpeed;
        agent.ResetPath();
        agent.isStopped = false;
        agent.updatePosition = true;
        agent.updateRotation = true;
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
