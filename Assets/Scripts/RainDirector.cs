using UnityEngine;

/// <summary>
/// Level 1: morning rain. Heavy rain masks noise; intensity eases at random.
/// </summary>
public class RainDirector : MonoBehaviour
{
    [Range(0f, 1f)]
    public float intensity = 1f;

    public float minIntensity = 0.2f;
    public float maxIntensity = 1f;
    public Vector2 changeEverySeconds = new Vector2(7f, 14f);
    public float lerpSpeed = 0.35f;

    ParticleSystem rain;
    float target = 1f;
    float nextChange;

    public static RainDirector Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        target = intensity;
        ScheduleNext();
    }

    void Start()
    {
        BuildRain();
        ApplySkyTint();
    }

    void Update()
    {
        if (Time.time >= nextChange)
        {
            target = Random.Range(minIntensity, maxIntensity);
            ScheduleNext();
        }

        intensity = Mathf.MoveTowards(intensity, target, lerpSpeed * Time.deltaTime);

        if (NoiseBus.Instance != null)
            NoiseBus.Instance.rainMask = intensity;

        if (rain != null)
        {
            var emission = rain.emission;
            emission.rateOverTime = Mathf.Lerp(40f, 650f, intensity);
        }
    }

    void ScheduleNext()
    {
        nextChange = Time.time + Random.Range(changeEverySeconds.x, changeEverySeconds.y);
    }

    void BuildRain()
    {
        var go = new GameObject("RainParticles");
        go.transform.SetParent(transform, false);
        rain = go.AddComponent<ParticleSystem>();

        var main = rain.main;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = 1.1f;
        main.startSpeed = 14f;
        main.startSize = 0.035f;
        main.startColor = new Color(0.7f, 0.8f, 0.9f, 0.55f);
        main.maxParticles = 4000;

        var shape = rain.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(22f, 0.2f, 22f);

        var collision = rain.collision;
        collision.enabled = false;

        go.transform.localPosition = new Vector3(0f, 8f, 0f);
    }

    void LateUpdate()
    {
        var player = ThirdPersonController.Player;
        if (player != null)
            transform.position = player.transform.position;
    }

    void ApplySkyTint()
    {
        var light = RenderSettings.sun != null ? RenderSettings.sun : FindFirstObjectByType<Light>();
        if (light != null)
        {
            light.color = new Color(0.72f, 0.78f, 0.88f);
            light.intensity = 0.85f;
        }
        RenderSettings.ambientLight = new Color(0.45f, 0.5f, 0.58f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.55f, 0.6f, 0.66f);
        RenderSettings.fogDensity = 0.02f;
    }
}
