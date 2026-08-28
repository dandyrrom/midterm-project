using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class BawangProjectile : MonoBehaviour
{
    public int damage = 1;
    public float maxLifetime = 4f;

    Vector3 velocity;
    float spawnTime;
    bool landed;
    AudioClip landClip;
    float landNoiseRadius;
    AudioSource audioSource;

    public void Launch(Vector3 startVelocity, AudioClip landSound, float noiseRadius)
    {
        velocity = startVelocity;
        landClip = landSound;
        landNoiseRadius = noiseRadius;
        spawnTime = Time.time;
    }

    void Awake()
    {
        SphereCollider sphere = GetComponent<SphereCollider>();
        sphere.isTrigger = true;

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();

        body.isKinematic = true;
        body.useGravity = false;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (landed)
            return;

        if (Time.time - spawnTime >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        velocity += Physics.gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        if (velocity.y <= 0f &&
            Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 0.35f) &&
            hit.collider.GetComponentInParent<ZombieHealth>() == null)
        {
            LandQuietly(hit.point);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (landed)
            return;

        if (other.GetComponentInParent<ThirdPersonController>() != null)
            return;

        ZombieHealth health = other.GetComponentInParent<ZombieHealth>();
        if (health == null || health.IsDead)
            return;

        landed = true;
        health.TakeDamage(damage);
        Destroy(gameObject);
    }

    void LandQuietly(Vector3 position)
    {
        if (landed)
            return;

        landed = true;

        NoiseEvents.Emit(position, landNoiseRadius);

        if (audioSource != null && landClip != null)
            audioSource.PlayOneShot(landClip, 0.35f);

        Destroy(gameObject, landClip != null ? landClip.length + 0.1f : 0.05f);
    }
}
