using UnityEngine;

public class ZombieHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 2;

    [Header("Death")]
    [Tooltip("Seconds before the corpse is destroyed. Match death clip length.")]
    public float deathDestroyDelay = 3f;

    int currentHealth;
    ZombieHitIndicator indicator;

    public int CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;

    static readonly int DieHash = Animator.StringToHash("Die");

    void Awake()
    {
        currentHealth = maxHealth;
        indicator = GetComponent<ZombieHitIndicator>();
        if (indicator == null)
            indicator = gameObject.AddComponent<ZombieHitIndicator>();
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        indicator?.Flash();

        if (IsDead)
        {
            Die();
            return;
        }

        GetComponent<ZombieRoam>()?.ReactToBawangHit();
    }

    void Die()
    {
        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        ZombieRoam roam = GetComponent<ZombieRoam>();
        if (roam != null)
            roam.StopForDeath();

        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
            agent.enabled = false;

        Animator anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();
        if (anim != null)
            anim.SetTrigger(DieHash);

        ZombieKillScore score = FindFirstObjectByType<ZombieKillScore>();
        score?.RegisterKill();

        // Keep Animator enabled so Death plays; destroy after clip.
        Destroy(gameObject, deathDestroyDelay);
    }
}
