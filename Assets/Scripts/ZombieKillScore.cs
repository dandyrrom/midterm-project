using System;
using UnityEngine;

public class ZombieKillScore : MonoBehaviour
{
    public int Killed { get; private set; }
    public int Total { get; private set; }

    public event Action<int, int> OnScoreChanged;

    void Start()
    {
        Total = FindObjectsByType<ZombieHealth>(FindObjectsSortMode.None).Length;
        Killed = 0;
        OnScoreChanged?.Invoke(Killed, Total);
    }

    public void RegisterKill()
    {
        Killed = Mathf.Min(Killed + 1, Total > 0 ? Total : Killed + 1);
        OnScoreChanged?.Invoke(Killed, Total);
    }
}
