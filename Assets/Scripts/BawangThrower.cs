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
    public float landNoiseRadius = 3f;
    public AudioClip landClip;

    BawangInventory inventory;
    PlayerHealth health;
    Animator animator;
    float releaseAt = -1f;
    bool throwPending;
    bool isThrowing;

    static readonly int ThrowHash = Animator.StringToHash("Throw");
    static readonly int ThrowLayer = 0;

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

        if (isThrowing)
        {
            if (IsThrowAnimActive() || throwPending)
                return;

            isThrowing = false;
        }

        if (Keyboard.current == null || !Keyboard.current[throwKey].wasPressedThisFrame)
            return;

        TryStartThrow();
    }

    bool IsThrowAnimActive()
    {
        if (animator == null)
            return throwPending;

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(ThrowLayer);

        if (current.IsName("throw"))
            return true;

        if (animator.IsInTransition(ThrowLayer))
        {
            if (current.IsName("throw"))
                return true;

            if (animator.GetNextAnimatorStateInfo(ThrowLayer).IsName("throw"))
                return true;
        }

        return false;
    }

    void TryStartThrow()
    {
        if (isThrowing || IsThrowAnimActive())
            return;
        if (inventory == null || inventory.count <= 0)
            return;

        if (!inventory.TrySpendBawang())
            return;

        if (animator != null)
        {
            animator.ResetTrigger(ThrowHash);
            animator.SetTrigger(ThrowHash);
        }

        isThrowing = true;
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
            bawang.Launch(velocity, landClip, landNoiseRadius);
    }
}
