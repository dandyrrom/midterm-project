using UnityEngine;
using UnityEngine.InputSystem;

public class BawangThrower : MonoBehaviour
{
    [Header("Throw")]
    public Key throwKey = Key.F;
    public float releaseDelay = 0.45f;
    public float throwSpeed = 9f;
    public float throwUpward = 3f;
    public Transform spawnPoint;

    [Header("Projectile")]
    public GameObject garlicProjectilePrefab;

    BawangInventory inventory;
    PlayerHealth health;
    Animator animator;
    float releaseAt = -1f;
    bool throwPending;

    static readonly int ThrowHash = Animator.StringToHash("Throw");

    void Awake()
    {
        inventory = GetComponent<BawangInventory>();
        health = GetComponent<PlayerHealth>();
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (spawnPoint == null)
            spawnPoint = transform;
    }

    void Update()
    {
        if (health != null && health.IsDead)
            return;

        if (throwPending && Time.time >= releaseAt)
        {
            throwPending = false;
            releaseAt = -1f;
            SpawnProjectile();
        }

        if (Keyboard.current == null || !Keyboard.current[throwKey].wasPressedThisFrame)
            return;

        TryStartThrow();
    }

    void TryStartThrow()
    {
        if (inventory == null || inventory.count <= 0)
            return;

        if (!inventory.TrySpendBawang())
            return;

        if (animator != null)
        {
            animator.ResetTrigger(ThrowHash);
            animator.SetTrigger(ThrowHash);
        }

        throwPending = true;
        releaseAt = Time.time + releaseDelay;
    }

    void SpawnProjectile()
    {
        if (garlicProjectilePrefab == null)
            return;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = transform.forward;

        forward.Normalize();
        Vector3 velocity = forward * throwSpeed + Vector3.up * throwUpward;
        Vector3 spawnPos = spawnPoint.position + forward * 0.35f;

        GameObject projectile = Instantiate(garlicProjectilePrefab, spawnPos, Quaternion.identity);
        BawangProjectile bawang = projectile.GetComponent<BawangProjectile>();
        if (bawang != null)
            bawang.Launch(velocity);
    }
}
