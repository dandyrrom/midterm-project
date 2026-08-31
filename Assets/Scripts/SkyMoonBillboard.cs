using UnityEngine;

/// <summary>
/// Visible moon disc that stays in the sky opposite the moon directional light.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SkyMoonBillboard : MonoBehaviour
{
    [Tooltip("Directional light used as the moon. Defaults to a Light on the parent.")]
    public Light moonLight;

    [Tooltip("Camera that should see the moon. Leave empty for Main Camera.")]
    public Camera targetCamera;

    [Tooltip("How far from the camera the moon sits (keep below Fog End in Lighting settings).")]
    public float skyDistance = 120f;

    [Tooltip("Apparent width of the moon in degrees.")]
    [Range(2f, 25f)]
    public float angularSize = 14f;

    public Color moonColor = new Color(1f, 0.98f, 0.9f, 1f);

    [Tooltip("Soft glow around the moon edge.")]
    [Range(0f, 0.5f)]
    public float glowStrength = 0.18f;

    static Mesh quadMesh;
    Material moonMaterial;
    Texture2D moonTexture;

    void OnEnable()
    {
        EnsureVisuals();
    }

    void OnDestroy()
    {
        CleanupGeneratedAssets();
    }

    void LateUpdate()
    {
        Light light = ResolveMoonLight();
        Camera cam = ResolveCamera();
        if (light == null || cam == null)
            return;

        Vector3 moonDirection = -light.transform.forward;
        if (moonDirection.sqrMagnitude < 0.0001f)
            moonDirection = Vector3.up;
        else
            moonDirection.Normalize();

        transform.position = cam.transform.position + moonDirection * skyDistance;

        Vector3 faceCamera = cam.transform.position - transform.position;
        if (faceCamera.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(faceCamera, Vector3.up);

        float diameter = Mathf.Tan(angularSize * 0.5f * Mathf.Deg2Rad) * skyDistance * 2f;
        transform.localScale = Vector3.one * diameter;

        if (moonMaterial != null && moonMaterial.HasProperty("_Color"))
            moonMaterial.SetColor("_Color", moonColor);
    }

    Camera ResolveCamera()
    {
        if (targetCamera != null)
            return targetCamera;

        return Camera.main;
    }

    Light ResolveMoonLight()
    {
        if (moonLight != null)
            return moonLight;

        moonLight = GetComponentInParent<Light>();
        return moonLight;
    }

    void EnsureVisuals()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (quadMesh == null)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying)
                Destroy(temp);
            else
                DestroyImmediate(temp);
        }

        meshFilter.sharedMesh = quadMesh;

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

        if (moonMaterial == null)
        {
            Shader shader = Shader.Find("Midterm/SkyMoon");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Transparent");

            moonMaterial = new Material(shader);
            moonMaterial.name = "SkyMoon (Runtime)";
            moonMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        if (moonTexture == null)
            moonTexture = CreateMoonTexture(glowStrength);

        if (moonMaterial.HasProperty("_MainTex"))
            moonMaterial.SetTexture("_MainTex", moonTexture);
        if (moonMaterial.HasProperty("_BaseMap"))
            moonMaterial.SetTexture("_BaseMap", moonTexture);
        if (moonMaterial.HasProperty("_Color"))
            moonMaterial.SetColor("_Color", moonColor);
        if (moonMaterial.HasProperty("_BaseColor"))
            moonMaterial.SetColor("_BaseColor", moonColor);
        if (moonMaterial.HasProperty("_Cull"))
            moonMaterial.SetFloat("_Cull", 0f);

        renderer.sharedMaterial = moonMaterial;
    }

    static Texture2D CreateMoonTexture(float glow)
    {
        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        texture.name = "SkyMoon (Runtime)";
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        float center = (size - 1) * 0.5f;
        float radius = center * 0.88f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / radius;
                float dy = (y - center) / radius;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > 1f)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                float edge = Mathf.Clamp01((1f - dist) / (0.06f + glow));
                float crater = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) * 0.08f;
                float mare = Mathf.PerlinNoise(x * 0.03f + 12f, y * 0.03f + 4f) * 0.12f;
                float brightness = 0.95f - crater - mare;
                texture.SetPixel(x, y, new Color(brightness, brightness, brightness - 0.03f, edge));
            }
        }

        texture.Apply(false, false);
        return texture;
    }

    void CleanupGeneratedAssets()
    {
        if (moonMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(moonMaterial);
            else
                DestroyImmediate(moonMaterial);
            moonMaterial = null;
        }

        if (moonTexture != null)
        {
            if (Application.isPlaying)
                Destroy(moonTexture);
            else
                DestroyImmediate(moonTexture);
            moonTexture = null;
        }
    }
}
