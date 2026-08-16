using UnityEngine;

/// <summary>
/// Editor/runtime helper: snap this humanoid's feet to the ground on play.
/// Safe for NavMeshAgents and plain animated meshes (no CharacterController required).
/// </summary>
public class GroundFeetOnPlay : MonoBehaviour
{
    [Tooltip("How far above the pivot to start the ground ray.")]
    public float probeUp = 0.6f;
    [Tooltip("How far below the probe start to search for ground.")]
    public float probeDown = 2.5f;

    void Start()
    {
        GroundFeet.Snap(transform, probeUp, probeDown);
    }
}
