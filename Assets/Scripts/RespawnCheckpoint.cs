using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RespawnCheckpoint : MonoBehaviour
{
    [Tooltip("Where she teleports. Defaults to this transform.")]
    public Transform spawnPoint;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        if (spawnPoint == null)
            spawnPoint = transform;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<ThirdPersonController>() == null)
            return;

        RespawnManager manager = FindFirstObjectByType<RespawnManager>();
        manager?.SetCheckpoint(spawnPoint);
    }
}
