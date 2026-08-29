using System;
using UnityEngine;

public class PlayerLives : MonoBehaviour
{
    [Header("Lives")]
    public int maxLives = 3;

    int currentLives;

    public int CurrentLives => currentLives;
    public int MaxLives => maxLives;

    public event Action<int, int> OnLivesChanged;

    void Awake()
    {
        currentLives = Mathf.Max(1, maxLives);
    }

    void Start()
    {
        OnLivesChanged?.Invoke(currentLives, maxLives);
    }

    /// <summary>
    /// Spends one life. Returns true if she can soft-respawn (lives still &gt; 0).
    /// Returns false if that was the last life (game over).
    /// </summary>
    public bool TrySpendLife()
    {
        if (currentLives <= 0)
            return false;

        currentLives--;
        OnLivesChanged?.Invoke(currentLives, maxLives);
        return currentLives > 0;
    }
}
