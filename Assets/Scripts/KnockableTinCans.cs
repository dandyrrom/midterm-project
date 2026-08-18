using UnityEngine;

/// <summary>
/// Physics knock for one tin-can pile. Sit still until the peasant girl bumps them,
/// then tumble. A drop from height plays a clatter and sends aswangs to that spot.
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
    [Tooltip("Impact speed needed with that drop before the clatter plays.")]
    public float minImpactSpeed = 1.4f;
    [Tooltip("Aswangs this far from the clatter will path to it.")]
    public float hearRadius = 120f;
    public AudioClip dropClip;

    Rigidbody body;
    CharacterController playerController;
    float peakY;
    bool released;
    bool trackingFall;
    float lastNoiseTime = -999f;

    void Awake()
    {
        SetupPhysics();
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

        if (trackingFall && hitFromAbove && drop >= fallHeightForNoise && impact >= minImpactSpeed)
            PlayFallNoise();

        trackingFall = false;
        peakY = transform.position.y;
    }

    void PlayFallNoise()
    {
        if (Time.time - lastNoiseTime < 0.6f)
            return;
        lastNoiseTime = Time.time;

        Vector3 pos = transform.position;
        if (dropClip != null)
            AudioSource.PlayClipAtPoint(dropClip, pos, 1f);

        NoisePulse.Emit(pos, hearRadius);
    }
}
