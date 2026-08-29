using UnityEngine;

public class ZombieHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 2;

    int currentHealth;
    ZombieHitIndicator indicator;

    public int CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;

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
        ZombieRoam roam = GetComponent<ZombieRoam>();
        if (roam != null)
            roam.enabled = false;

        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
            agent.enabled = false;

        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        Destroy(gameObject, 1.5f);
    }
}
