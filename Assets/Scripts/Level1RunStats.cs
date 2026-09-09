using System;
using UnityEngine;

/// <summary>
/// Tracks run stats for the Level 1 end / game-over screen.
/// "Missed throws" = bawang/candle throws that expire or land without hitting an aswang.
/// </summary>
public class Level1RunStats : MonoBehaviour
{
    public int MissedThrows { get; private set; }

    public event Action<int> OnMissedThrowsChanged;

    public void RegisterMissedThrow()
    {
        MissedThrows++;
        OnMissedThrowsChanged?.Invoke(MissedThrows);
    }

    /// <summary>
    /// Perfect clear (all aswangs dead, no missed throws, no lives lost) = perfectScore.
    /// Partial progress on game over scales the base by kills/total.
    /// </summary>
    public int ComputeTotalScore(
        int aswangKilled,
        int aswangTotal,
        int livesRemaining,
        int maxLives,
        int perfectScore,
        int missedThrowPenalty,
        int lifeLostPenalty)
    {
        float progress = aswangTotal > 0
            ? Mathf.Clamp01((float)aswangKilled / aswangTotal)
            : 0f;

        int livesLost = Mathf.Max(0, maxLives - livesRemaining);
        int score = Mathf.RoundToInt(perfectScore * progress)
            - MissedThrows * missedThrowPenalty
            - livesLost * lifeLostPenalty;

        return Mathf.Max(0, score);
    }
}
