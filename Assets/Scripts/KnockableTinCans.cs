using UnityEngine;

/// <summary>
/// Physics knock for one tin-can pile. Sit still until the peasant girl bumps them,
/// then tumble. Impacts clatter; a drop from height also alerts aswangs.
/// </summary>
[DisallowMultipleComponent]
public class KnockableTinCans : MonoBehaviour
{
    [Header("Knock")]
    [Tooltip("Impulse when mc-Peasant Girl first hits the cans.")]
    public float playerPushForce = 4f;
    [Tooltip("How close her CharacterController must be to knock them.")]
    public float wakeRadius = 0.9f;
    [Tooltip("Keep shoving while she stays in contact.")]
    public float shoveForce = 8f;

    [Header("Fall noise")]
    [Tooltip("Vertical drop (meters) that counts as falling from a height.")]
    public float fallHeightForNoise = 0.2f;
    [Tooltip("Impact speed needed with that drop before aswangs hear it.")]
    public float minImpactSpeed = 1.4f;
    [Tooltip("How far the fall-clatter carries (meters). Aswangs farther than this stay put.")]
    public float hearRadius = 18f;
    public AudioClip dropClip;
    [Tooltip("0 = 2D (always hear it). 1 = 3D at the cans.")]
    [Range(0f, 1f)]
    public float spatialBlend = 0f;
    [Range(0f, 1f)]
    public float volume = 1f;

    Rigidbody body;
    AudioSource audioSource;
    AudioClip fallbackClip;
    CharacterController playerController;
    float peakY;
    bool released;
    bool trackingFall;
    float lastNoiseTime = -999f;
    float lastClatterTime = -999f;

    void Awake()
    {
        SetupPhysics();
        SetupAudio();
    }

    void Start()
    {
        CachePlayer();
        peakY = transform.position.y;
    }

    void SetupPhysics()
    {
        foreach (MeshCollider meshCollider in GetComponentsInChildren<MeshCollider>())
            meshCollider.enabled = false;

        MeshFilter filter = GetComponentInChildren<MeshFilter>();
        if (filter != null && filter.GetComponent<BoxCollider>() == null && filter.sharedMesh != null)
        {
            BoxCollider box = filter.gameObject.AddComponent<BoxCollider>();
            box.center = filter.sharedMesh.bounds.center;
            box.size = filter.sharedMesh.bounds.size * 0.95f;
        }

        body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();

        body.mass = 0.85f;
        body.linearDamping = 0.35f;
        body.angularDamping = 0.45f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.useGravity = true;
        body.isKinematic = true;
        body.sleepThreshold = 0.02f;
    }

    void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = spatialBlend;
        audioSource.volume = volume;
        audioSource.minDistance = 8f;
        audioSource.maxDistance = 80f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.clip = ResolveClip();
    }

    AudioClip ResolveClip()
    {
        if (dropClip != null && dropClip.length > 0.05f)
            return dropClip;
        if (fallbackClip == null)
            fallbackClip = BuildFallbackClatter();
        return fallbackClip;
    }

    static AudioClip BuildFallbackClatter()
    {
        const int sampleRate = 44100;
        int samples = Mathf.RoundToInt(sampleRate * 0.45f);
        float[] data = new float[samples];
        float[] clackAt = { 0f, 0.055f, 0.11f, 0.175f, 0.26f };
        float[] clackAmp = { 1f, 0.85f, 0.7f, 0.5f, 0.35f };
        float[] clackHz = { 1700f, 1320f, 2100f, 980f, 2450f };

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float s = 0f;
            for (int c = 0; c < clackAt.Length; c++)
            {
                if (t < clackAt[c])
                    continue;
                float td = t - clackAt[c];
                float env = Mathf.Exp(-td * 18f);
                s += clackAmp[c] * env * Mathf.Sin(2f * Mathf.PI * clackHz[c] * td);
                s += clackAmp[c] * 0.35f * env * Mathf.Sin(2f * Mathf.PI * clackHz[c] * 1.6f * td);
            }

            data[i] = Mathf.Clamp(s * 0.7f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("tin_cans_drop_runtime", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void CachePlayer()
    {
        GameObject player = GameObject.Find("mc-Peasant Girl");
        if (player != null)
            playerController = player.GetComponent<CharacterController>();
    }

    void FixedUpdate()
    {
        if (body == null)
            return;

        if (playerController == null)
            CachePlayer();

        if (playerController != null && IsPlayerClose())
        {
            Vector3 playerPos = playerController.transform.position;
            if (!released)
                ReleaseAndPush(playerPos);
            else
                Shove(playerPos);
        }

        if (!released)
            return;

        float y = transform.position.y;
        if (body.linearVelocity.y < -0.25f)
        {
            trackingFall = true;
            if (y > peakY)
                peakY = y;
        }
        else if (!trackingFall)
        {
            peakY = y;
        }
    }

    bool IsPlayerClose()
    {
        Vector3 closest = playerController.ClosestPoint(transform.position);
        Collider propCollider = null;
        foreach (Collider candidate in GetComponentsInChildren<Collider>())
        {
            if (!candidate.enabled)
                continue;
            propCollider = candidate;
            break;
        }

        Vector3 propPoint = propCollider != null ? propCollider.ClosestPoint(closest) : transform.position;
        return (closest - propPoint).sqrMagnitude <= wakeRadius * wakeRadius;
    }

    void ReleaseAndPush(Vector3 playerPos)
    {
        released = true;
        transform.SetParent(null, true);
        body.isKinematic = false;

        Vector3 dir = transform.position - playerPos;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            dir = transform.forward;
        dir.Normalize();

        body.AddForce(dir * playerPushForce + Vector3.up * (playerPushForce * 0.65f), ForceMode.Impulse);
        body.AddTorque(Random.onUnitSphere * (playerPushForce * 0.35f), ForceMode.Impulse);
        peakY = transform.position.y;
        trackingFall = false;
        PlayClatter();
    }

    void Shove(Vector3 playerPos)
    {
        Vector3 dir = transform.position - playerPos;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            return;
        dir.Normalize();
        body.AddForce(dir * shoveForce, ForceMode.Force);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!released || collision.contactCount == 0)
            return;

        float drop = peakY - transform.position.y;
        float impact = collision.relativeVelocity.magnitude;
        bool hitFromAbove = Vector3.Dot(collision.GetContact(0).normal, Vector3.up) > 0.35f;

        if (impact >= 0.8f)
            PlayClatter();

        if (trackingFall && hitFromAbove && drop >= fallHeightForNoise && impact >= minImpactSpeed)
            AlertAswangs();

        trackingFall = false;
        peakY = transform.position.y;
    }

    void PlayClatter()
    {
        if (Time.time - lastClatterTime < 0.12f)
            return;
        lastClatterTime = Time.time;

        AudioClip clip = ResolveClip();
        if (clip == null || audioSource == null)
            return;

        audioSource.spatialBlend = spatialBlend;
        audioSource.volume = volume;
        audioSource.PlayOneShot(clip, volume);
    }

    void AlertAswangs()
    {
        if (Time.time - lastNoiseTime < 0.6f)
            return;
        lastNoiseTime = Time.time;
        PlayClatter();
        NoisePulse.Emit(transform.position, hearRadius);
    }
}
