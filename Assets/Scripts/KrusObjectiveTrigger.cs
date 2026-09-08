using UnityEngine;

/// <summary>
/// Trigger volume at the bloody krus. Only active after all aswangs are killed.
/// </summary>
[RequireComponent(typeof(Collider))]
public class KrusObjectiveTrigger : MonoBehaviour
{
    public Level1GameFlow flow;

    bool fired;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (fired || flow == null || !flow.IsPhaseB)
            return;

        if (other.GetComponentInParent<ThirdPersonController>() == null)
            return;

        fired = true;
        flow.NotifyKrusReached();
    }
}
