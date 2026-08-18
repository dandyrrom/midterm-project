using System;
using UnityEngine;

public readonly struct NoisePulse
{
    public readonly Vector3 Position;
    public readonly float HearRadius;

    public NoisePulse(Vector3 position, float hearRadius)
    {
        Position = position;
        HearRadius = hearRadius;
    }

    public static event Action<NoisePulse> Emitted;

    public static void Emit(Vector3 position, float hearRadius)
    {
        Emitted?.Invoke(new NoisePulse(position, hearRadius));
    }
}
