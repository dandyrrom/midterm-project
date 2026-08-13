using UnityEngine;

public class LunasProjectile : MonoBehaviour
{
    public float lifetime = 3f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter(Collider other)
    {
        var aswang = other.GetComponentInParent<AswangController>();
        if (aswang == null)
            return;

        aswang.Banish();
        Destroy(gameObject);
    }
}
