using UnityEngine;

public class AswangController : MonoBehaviour
{
    public float walkSpeed = 1.35f;
    public float chaseSpeed = 3.1f;
    public float hearRange = 14f;
    public float attackRange = 1.55f;
    public float attackCooldown = 1.25f;
    public float forgetTime = 4f;

    enum State { Idle, Chase, Attack, Banished }

    State state = State.Idle;
    Transform target;
    float lastHeardTime = -999f;
    Vector3 lastHeardPos;
    float nextAttack;
    Renderer[] renderers;

    public bool IsBanished => state == State.Banished;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        var bus = NoiseBus.Instance;
        if (bus != null)
            bus.Heard += OnHeard;
    }

    void OnDestroy()
    {
        if (NoiseBus.Instance != null)
            NoiseBus.Instance.Heard -= OnHeard;
    }

    void OnHeard(Vector3 position, float loudness)
    {
        if (state == State.Banished)
            return;

        float range = hearRange * Mathf.Clamp(loudness, 0.04f, 1.5f);
        if (Vector3.Distance(transform.position, position) > range)
            return;

        lastHeardPos = position;
        lastHeardTime = Time.time;
        if (state == State.Idle)
            state = State.Chase;
    }

    void Update()
    {
        if (state == State.Banished)
            return;

        if (PlayerCombatResources.Instance != null && PlayerCombatResources.Instance.IsDead)
        {
            state = State.Idle;
            return;
        }

        if (ThirdPersonController.Player != null)
            target = ThirdPersonController.Player.transform;

        if (state == State.Chase && Time.time - lastHeardTime > forgetTime)
            state = State.Idle;

        switch (state)
        {
            case State.Idle:
                break;
            case State.Chase:
                Chase();
                break;
            case State.Attack:
                TryAttack();
                break;
        }
    }

    void Chase()
    {
        if (target == null)
            return;

        Vector3 goal = Vector3.Distance(transform.position, target.position) < 8f ? target.position : lastHeardPos;
        goal.y = transform.position.y;
        Vector3 to = goal - transform.position;
        to.y = 0f;

        if (to.sqrMagnitude > 0.05f)
        {
            Quaternion look = Quaternion.LookRotation(to.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 8f * Time.deltaTime);
            transform.position += to.normalized * chaseSpeed * Time.deltaTime;
        }

        if (target != null && Vector3.Distance(Flat(transform.position), Flat(target.position)) <= attackRange)
            state = State.Attack;
    }

    void TryAttack()
    {
        if (target == null)
        {
            state = State.Idle;
            return;
        }

        float dist = Vector3.Distance(Flat(transform.position), Flat(target.position));
        if (dist > attackRange * 1.25f)
        {
            state = State.Chase;
            lastHeardTime = Time.time;
            lastHeardPos = target.position;
            return;
        }

        Vector3 to = target.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(to.normalized);

        if (Time.time >= nextAttack)
        {
            nextAttack = Time.time + attackCooldown;
            PlayerCombatResources.Instance?.TakeHit();
        }
    }

    public void Banish()
    {
        state = State.Banished;
        SoftSfx.Play(transform.position, 880f, 0.45f);
        foreach (var r in renderers)
        {
            if (r != null)
                r.material.color = new Color(0.4f, 0.45f, 0.35f, 0.35f);
        }
        Destroy(gameObject, 0.4f);
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
}
