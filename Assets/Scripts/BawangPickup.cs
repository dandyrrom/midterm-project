using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BawangPickup : MonoBehaviour
{
    public float faceDotThreshold = 0.6f;

    Transform playerInRange;
    bool collected;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        CachePlayer(other.transform);
    }

    void OnTriggerExit(Collider other)
    {
        BawangInventory inv = other.GetComponentInParent<BawangInventory>();
        if (inv != null && playerInRange == inv.transform)
            playerInRange = null;
    }

    void OnTriggerStay(Collider other)
    {
        if (collected)
            return;

        CachePlayer(other.transform);
    }

    void CachePlayer(Transform t)
    {
        BawangInventory inv = t.GetComponentInParent<BawangInventory>();
        if (inv != null)
            playerInRange = inv.transform;
    }

    public bool CanCollectFrom(Transform player)
    {
        if (collected || playerInRange != player)
            return false;

        Vector3 toPickup = transform.position - player.position;
        toPickup.y = 0f;
        if (toPickup.sqrMagnitude < 0.01f)
            return false;

        return Vector3.Dot(player.forward, toPickup.normalized) >= faceDotThreshold;
    }

    public bool CompletePickup(BawangInventory inventory)
    {
        if (collected || inventory == null || inventory.IsFull)
            return false;

        if (!inventory.TryAddBawang())
            return false;

        collected = true;
        gameObject.SetActive(false);
        return true;
    }
}
