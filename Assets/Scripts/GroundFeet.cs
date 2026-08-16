using UnityEngine;

/// <summary>
/// Snaps a feet-at-pivot humanoid to the ground collider under them.
/// Use on characters whose Transform Y should sit on the terrain (Mixamo-style).
/// </summary>
public static class GroundFeet
{
    public static bool Snap(Transform target, float probeUp = 0.6f, float probeDown = 2.5f)
    {
        if (target == null)
            return false;

        Vector3 origin = target.position + Vector3.up * probeUp;
        float maxDistance = probeUp + probeDown;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
            return false;

        if (hit.collider != null && hit.collider.transform != null &&
            (hit.collider.transform == target || hit.collider.transform.IsChildOf(target)))
            return false;

        CharacterController controller = target.GetComponent<CharacterController>();
        Vector3 pos = target.position;
        pos.y = hit.point.y;

        if (controller != null)
        {
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            target.position = pos;
            controller.enabled = wasEnabled;
        }
        else
        {
            target.position = pos;
        }

        return true;
    }

    /// <summary>
    /// Keeps Skin Width small so Play Mode does not hover the mesh above the ground.
    /// Leaves Center/Height alone (prefer Center Y = 1 for this project).
    /// </summary>
    public static void AlignCapsuleToFeet(CharacterController controller, float skinWidth = 0.02f)
    {
        if (controller == null)
            return;

        controller.skinWidth = Mathf.Clamp(skinWidth, 0.001f, 0.08f);
    }
}
