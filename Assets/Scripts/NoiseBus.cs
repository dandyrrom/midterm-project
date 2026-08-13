using System;
using UnityEngine;

/// <summary>
/// Global sound events. Rain reduces how far a noise travels.
/// </summary>
public class NoiseBus : MonoBehaviour
{
    public static NoiseBus Instance { get; private set; }

    public event Action<Vector3, float> Heard;

    [Range(0f, 1f)]
    public float rainMask = 1f;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void Emit(Vector3 worldPosition, float loudness)
    {
        if (Instance == null || loudness <= 0.01f)
            return;

        float masked = loudness * Mathf.Lerp(1f, 0.28f, Instance.rainMask);
        Instance.Heard?.Invoke(worldPosition, masked);
    }
}
