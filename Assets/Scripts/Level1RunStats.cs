using System;
using UnityEngine;

/// <summary>
/// Tracks run stats for the Level 1 end / game-over screen.
/// "Hits missed" = times the MC took damage from aswang attacks.
/// </summary>
public class Level1RunStats : MonoBehaviour
{
    public int HitsMissed { get; private set; }

    public event Action<int> OnHitsMissedChanged;

    PlayerHealth health;

    void Awake()
    {
        health = FindFirstObjectByType<PlayerHealth>();
    }

    void OnEnable()
    {
        if (health != null)
            health.OnDamaged += HandleDamaged;
    }

    void OnDisable()
    {
        if (health != null)
            health.OnDamaged -= HandleDamaged;
    }

    void HandleDamaged(int amount)
    {
        if (amount <= 0)
            return;

        HitsMissed++;
        OnHitsMissedChanged?.Invoke(HitsMissed);
    }

    public int ComputeTotalScore(int aswangKilled, int livesRemaining, int pointsPerKill, int hitPenalty, int lifeBonus)
    {
        return Mathf.Max(0, aswangKilled * pointsPerKill - HitsMissed * hitPenalty + livesRemaining * lifeBonus);
    }
}
