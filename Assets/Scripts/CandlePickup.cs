using UnityEngine;

/// <summary>
/// World pickup for Candle01b_on. Face the candle and press Interact (E) to grab.
/// Uses world distance (not only trigger overlap) so rotated/shelf candles still work.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CandlePickup : MonoBehaviour
{
    [Tooltip("How much the player must face the candle (1 = dead-on, 0 = sideways OK).")]
    public float faceDotThreshold = 0.1f;

    [Tooltip("Max distance from player feet/body to candle to allow pickup.")]
    public float pickupRadius = 2.25f;

    bool collected;
    SphereCollider pickupSphere;

    void Awake()
    {
        EnsurePickupSphere();

        // Mesh collider is tiny and oriented with the -90° candle mesh; don't use it for pickup.
        foreach (Collider col in GetComponents<Collider>())
        {
            if (col == pickupSphere)
                continue;

            if (col is MeshCollider)
                col.enabled = false;
        }
    }

    void EnsurePickupSphere()
    {
        pickupSphere = GetComponent<SphereCollider>();
        if (pickupSphere == null)
            pickupSphere = gameObject.AddComponent<SphereCollider>();

        pickupSphere.isTrigger = true;
        pickupSphere.enabled = true;

        // Candle01b_on is rotated -90 X, so height is along local Z — keep a generous world-ish radius.
        // Local radius is scaled by lossyScale, so bump it for small candles.
        float avgScale = (Mathf.Abs(transform.lossyScale.x) +
                          Mathf.Abs(transform.lossyScale.y) +
                          Mathf.Abs(transform.lossyScale.z)) / 3f;
        if (avgScale < 0.01f)
            avgScale = 1f;

        pickupSphere.radius = Mathf.Max(1.2f, pickupRadius * 0.55f) / avgScale;
        // Offset along local Z (up in world for these candles).
        pickupSphere.center = new Vector3(0f, 0f, 0.35f / avgScale);
    }

    public bool CanCollectFrom(Transform player)
    {
        if (collected || player == null)
            return false;

        Vector3 toPickup = transform.position - player.position;
        if (toPickup.sqrMagnitude > pickupRadius * pickupRadius)
            return false;

        Vector3 flat = toPickup;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.01f)
            return true;

        return Vector3.Dot(player.forward, flat.normalized) >= faceDotThreshold;
    }

    public bool CompletePickup(CandleInventory inventory)
    {
        if (collected || inventory == null || inventory.IsFull)
            return false;

        if (!inventory.TryAddCandle())
            return false;

        collected = true;
        gameObject.SetActive(false);
        return true;
    }
}
