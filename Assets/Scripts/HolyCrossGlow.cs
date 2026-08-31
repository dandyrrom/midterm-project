using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Warm emissive glow and point light for church crosses.
/// </summary>
[DisallowMultipleComponent]
public class HolyCrossGlow : MonoBehaviour
{
    public Color glowColor = new Color(1f, 0.92f, 0.65f, 1f);

    [Range(0f, 12f)]
    public float emissionIntensity = 4f;

    public bool pulse = true;

    [Range(0f, 3f)]
    public float pulseSpeed = 0.7f;

    [Range(0f, 1f)]
    public float pulseAmount = 0.2f;

    public bool addPointLight = true;

    [Range(0f, 8f)]
    public float lightIntensity = 2f;

    [Range(0.5f, 20f)]
    public float lightRange = 5f;

    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    Renderer targetRenderer;
    Material glowMaterial;
    Light pointLight;
    float pulsePhase;

    void Awake()
    {
        SetupGlow();
    }

    void OnDestroy()
    {
        if (glowMaterial != null)
            Destroy(glowMaterial);
    }

    void Update()
    {
        if (!pulse || glowMaterial == null)
            return;

        pulsePhase += Time.deltaTime * pulseSpeed * Mathf.PI * 2f;
        float wave = Mathf.Sin(pulsePhase) * 0.5f + 0.5f;
        float factor = 1f - pulseAmount * 0.5f + pulseAmount * 0.5f * wave;
        ApplyIntensity(factor);
    }

    void SetupGlow()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (targetRenderer == null || targetRenderer.sharedMaterial == null)
            return;

        if (glowMaterial == null)
        {
            glowMaterial = new Material(targetRenderer.sharedMaterial);
            glowMaterial.EnableKeyword("_EMISSION");
            glowMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            targetRenderer.material = glowMaterial;
        }

        if (addPointLight)
        {
            pointLight = GetComponentInChildren<Light>();
            if (pointLight == null)
            {
                var lightObject = new GameObject("CrossGlowLight");
                lightObject.transform.SetParent(transform, false);
                pointLight = lightObject.AddComponent<Light>();
                pointLight.type = LightType.Point;
                pointLight.shadows = LightShadows.Soft;
            }

            pointLight.color = glowColor;
            pointLight.range = lightRange;
        }

        ApplyIntensity(1f);
    }

    void ApplyIntensity(float multiplier)
    {
        if (glowMaterial != null)
            glowMaterial.SetColor(EmissionColorId, glowColor * (emissionIntensity * multiplier));

        if (pointLight != null)
            pointLight.intensity = lightIntensity * multiplier;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!isActiveAndEnabled)
            return;

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (targetRenderer != null && glowMaterial == null && targetRenderer.sharedMaterial != null)
            SetupGlow();
        else if (glowMaterial != null)
            ApplyIntensity(1f);
    }
#endif
}
