using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class BawangProjectile : MonoBehaviour
{
    public float maxLifetime = 4f;

    Vector3 velocity;
    float spawnTime;

    public void Launch(Vector3 startVelocity)
    {
        velocity = startVelocity;
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
    }

    void Update()
    {
        if (Time.time - spawnTime >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        velocity += Physics.gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        if (velocity.y <= 0f &&
            Physics.Raycast(transform.position, Vector3.down, out _, 0.35f))
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<ThirdPersonController>() != null)
            return;
    }
}
