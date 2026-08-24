using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;

    int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;

    public event Action<int, int> OnHealthChanged;
    public event Action<int> OnDamaged;
    public event Action OnDied;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public bool TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead)
            return false;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        OnDamaged?.Invoke(amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log("HP: " + currentHealth + " / " + maxHealth);

        if (IsDead)
        {
            OnDied?.Invoke();
            return false;
        }

        return true;
    }

    public void Kill()
    {
        if (IsDead)
            return;

        currentHealth = 0;
        Debug.Log("HP: 0 / " + maxHealth + " (killed)");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDied?.Invoke();
    }
}