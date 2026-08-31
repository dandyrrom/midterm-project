using UnityEngine;

/// <summary>
/// Applies linear scene fog at runtime (and in the Editor while tweaking values).
/// Add to an empty GameObject in Level 1 — e.g. "Environment" or "Weather".
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public class SceneFog : MonoBehaviour
{
    [Tooltip("Turn scene fog on or off.")]
    public bool enableFog = true;

    [Tooltip("Morning mist color. Match your skybox horizon for a soft blend.")]
    public Color fogColor = new Color(0.05f, 0.06f, 0.12f, 1f);

    [Tooltip("Linear fog is easiest to tune for outdoor levels.")]
    public FogMode fogMode = FogMode.Linear;

    [Tooltip("Used when Fog Mode is Exponential or Exponential Squared.")]
    [Min(0f)]
    public float fogDensity = 0.01f;

    [Tooltip("Distance where fog begins (world units).")]
    [Min(0f)]
    public float linearFogStart = 20f;

    [Tooltip("Distance where fog is fully opaque. Keep Sky Moon sky distance below this.")]
    [Min(0f)]
    public float linearFogEnd = 180f;

    void OnEnable()
    {
        Apply();
    }

    void OnValidate()
    {
        if (linearFogEnd < linearFogStart)
            linearFogEnd = linearFogStart;

        Apply();
    }

    void Apply()
    {
        RenderSettings.fog = enableFog;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogStartDistance = linearFogStart;
        RenderSettings.fogEndDistance = linearFogEnd;
    }
}
