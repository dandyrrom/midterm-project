using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KnockableProp : MonoBehaviour
{
    public float noiseLoudness = 0.7f;
    public float minImpact = 0.8f;

    Rigidbody body;
    float cooldown;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.mass = 0.7f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    void Update()
    {
        cooldown -= Time.deltaTime;
    }

    public void Nudge(Vector3 direction, float force)
    {
        body.AddForce((direction + Vector3.up * 0.35f).normalized * force, ForceMode.Impulse);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (cooldown > 0f)
            return;
        if (collision.relativeVelocity.magnitude < minImpact)
            return;

        cooldown = 0.25f;
        NoiseBus.Emit(transform.position, noiseLoudness);
        SoftSfx.Play(transform.position, 180f, 0.4f);
    }
}
