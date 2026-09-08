using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Makes Candle01b_on instances in the open scene pickable.
/// Menu: Tools &gt; Midterm &gt; Make Candle01b_on Pickable
/// </summary>
public static class Candle01bOnPickupSetup
{
#if UNITY_EDITOR
    const string CandleNamePrefix = "Candle01b_on";

    [MenuItem("Tools/Midterm/Make Candle01b_on Pickable")]
    static void MakePickable()
    {
        int added = 0;
        int refreshed = 0;

        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t == null || t.gameObject == null)
                continue;

            string name = t.gameObject.name;
            if (!name.StartsWith(CandleNamePrefix))
                continue;

            if (t.parent != null && t.parent.name.StartsWith(CandleNamePrefix))
                continue;

            Undo.RegisterFullObjectHierarchyUndo(t.gameObject, "Make Candle01b_on Pickable");

            CandlePickup pickup = t.GetComponent<CandlePickup>();
            if (pickup == null)
            {
                pickup = Undo.AddComponent<CandlePickup>(t.gameObject);
                added++;
            }
            else
            {
                refreshed++;
            }

            pickup.faceDotThreshold = 0.1f;
            pickup.pickupRadius = 2.25f;

            // Mesh collider fights pickup on these rotated props.
            foreach (MeshCollider mesh in t.GetComponents<MeshCollider>())
                mesh.enabled = false;

            SphereCollider sphere = t.GetComponent<SphereCollider>();
            if (sphere == null)
                sphere = Undo.AddComponent<SphereCollider>(t.gameObject);

            float avgScale = (Mathf.Abs(t.lossyScale.x) +
                              Mathf.Abs(t.lossyScale.y) +
                              Mathf.Abs(t.lossyScale.z)) / 3f;
            if (avgScale < 0.01f)
                avgScale = 1f;

            sphere.isTrigger = true;
            sphere.enabled = true;
            sphere.radius = Mathf.Max(1.2f, pickup.pickupRadius * 0.55f) / avgScale;
            // Candle mesh is -90 X; local Z is world up.
            sphere.center = new Vector3(0f, 0f, 0.35f / avgScale);

            EditorUtility.SetDirty(t.gameObject);
        }

        Debug.Log($"Candle01b_on: added {added}, refreshed {refreshed}. Face + E to grab (max 1). Throw with G.");
    }
#endif
}
