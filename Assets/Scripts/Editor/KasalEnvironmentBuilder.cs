using UnityEditor;
using UnityEngine;

/// <summary>
/// Rebuilds the kasal town / church / cemetery layout from kit prefabs.
/// Tools &gt; Midterm &gt; Rebuild Kasal Environment
/// </summary>
public static class KasalEnvironmentBuilder
{
    const string TownRootName = "Town Barrio";
    const string ChurchRootName = "Kasal Church";
    const string CemeteryRootName = "Cemetery";

    [MenuItem("Tools/Midterm/Rebuild Kasal Environment")]
    public static void Rebuild()
    {
        DestroyRoot(TownRootName);
        DestroyRoot(ChurchRootName);
        DestroyRoot(CemeteryRootName);
        DestroySceneRootCopies();

        var town = GetOrCreateRoot(TownRootName);
        var church = GetOrCreateRoot(ChurchRootName);
        var cemetery = GetOrCreateRoot(CemeteryRootName);

        const float cx = 56.47531f;
        const float cy = 1.0000079f;
        const float cz = 50.91289f;

        // Church (kasal) — same Church 1 Open pose Danni already play-tested.
        Place(church, "Assets/Cemetery Kit V1.25/Prefabs/Buildings/Church 1 Open.prefab",
            "Church 1 Open", new Vector3(cx, cy, cz));
        Place(church, "Assets/Cemetery Kit V1.25/Prefabs/Church Supplies/Cross Arch.prefab",
            "Cross Arch", new Vector3(cx, 1f, cz - 9.5f));

        float[] pewZ = { -4.5f, -2f, 0.5f, 3f };
        for (int i = 0; i < pewZ.Length; i++)
        {
            Place(church, "Assets/Cemetery Kit V1.25/Prefabs/Church Supplies/Bench 1.prefab",
                $"Bench 1 L{i + 1}", new Vector3(cx - 2.6f, 1f, cz + pewZ[i]));
            Place(church, "Assets/Cemetery Kit V1.25/Prefabs/Church Supplies/Bench 1.prefab",
                $"Bench 1 R{i + 1}", new Vector3(cx + 2.6f, 1f, cz + pewZ[i]));
        }

        Place(church, "Assets/Cemetery Kit V1.25/Prefabs/Church Supplies/Bench 2.prefab",
            "Bench 2 L back", new Vector3(cx - 2.6f, 1f, cz - 6.5f));
        Place(church, "Assets/Cemetery Kit V1.25/Prefabs/Church Supplies/Bench 2.prefab",
            "Bench 2 R back", new Vector3(cx + 2.6f, 1f, cz - 6.5f));
        Place(church, "Assets/Cemetery Kit V1.25/Prefabs/Church Supplies/Stage.prefab",
            "Stage", new Vector3(cx, 1.05f, cz + 7.2f));
        Place(church, "Assets/Cemetery Kit V1.25/Prefabs/Church Supplies/Podium.prefab",
            "Podium", new Vector3(cx, 1.15f, cz + 6.2f));
        Place(church, "Assets/Cemetery Kit V1.25/Prefabs/Church Supplies/Cross.prefab",
            "Cross", new Vector3(cx, 1.4f, cz + 8.4f));

        // Town / barrio around spawn (7.63, 0, 34.32). Keep the path to the church clear.
        Place(town, "Assets/Town Creator Kit LITE/Prefabs/Town Kit LITE/House 3 and Roof.prefab",
            "House 3 and Roof", new Vector3(10.854112f, 0f, 89.574776f));
        Place(town, "Assets/Town Creator Kit LITE/Prefabs/Town Kit LITE/House 3 and Roof Alt.prefab",
            "House 3 and Roof Alt", new Vector3(-14f, 0f, 72f), 90f);
        Place(town, "Assets/Town Creator Kit LITE/Prefabs/Town Kit LITE/Town House 1.prefab",
            "Town House 1", new Vector3(-8f, 1f, 28f));
        Place(town, "Assets/Town Creator Kit LITE/Prefabs/Town Kit LITE/Town House 1 Alt.prefab",
            "Town House 1 Alt", new Vector3(24f, 1f, 20f));
        Place(town, "Assets/Town Creator Kit LITE/Prefabs/Town Kit LITE/Cellar 1.prefab",
            "Cellar 1", new Vector3(22f, 1f, 82f));
        Place(town, "Assets/Town Creator Kit LITE/Prefabs/Town Kit LITE/Lantern.prefab",
            "Lantern path 1", new Vector3(5.596274f, 0f, 72.98498f));
        Place(town, "Assets/Town Creator Kit LITE/Prefabs/Town Kit LITE/Lantern.prefab",
            "Lantern path 2", new Vector3(18f, 0f, 48f));
        Place(town, "Assets/Town Creator Kit LITE/Prefabs/Town Kit LITE/Lantern.prefab",
            "Lantern path 3", new Vector3(32f, 0f, 46.5f));
        Place(town, "Assets/Town Creator Kit LITE/Prefabs/Town Kit LITE/Lantern.prefab",
            "Lantern church yard", new Vector3(46f, 0f, 49f));
        Place(town, "Assets/Town Creator Kit LITE/Prefabs/Town Kit LITE/Storage.prefab",
            "Storage plaza", new Vector3(6.15f, 0.05f, 55.59f));
        Place(town, "Assets/Town Creator Kit LITE/Prefabs/Town Kit LITE/Storage.prefab",
            "Storage kitchen", new Vector3(20.5f, 0.05f, 80f));
        Place(town, "Assets/Town Creator Kit LITE/Prefabs/Town Kit LITE/Storage.prefab",
            "Storage path", new Vector3(9.5f, 0.05f, 41.5f));

        // Cemetery east of the church.
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Gates, Fences/Editor_Built/Fence_Gate_Group_1A.prefab",
            "Fence_Gate_Group_1A", new Vector3(67.5f, 1.23f, 51f));
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Gates, Fences/Editor_Built/Fence_Wall_Group_1A.prefab",
            "Fence_Wall_Group north", new Vector3(78f, 1.08f, 72f));
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Gates, Fences/Editor_Built/Fence_Wall_Group_1A.prefab",
            "Fence_Wall_Group east", new Vector3(90f, 1.08f, 54f), 90f);
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Gates, Fences/Editor_Built/Fence_Wall_Group_1A.prefab",
            "Fence_Wall_Group south", new Vector3(78f, 1.08f, 36f));
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Gates, Fences/Fence_1E.prefab",
            "Fence_1E west 1", new Vector3(67.8f, 1f, 44f));
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Gates, Fences/Fence_1E.prefab",
            "Fence_1E west 2", new Vector3(67.8f, 1f, 58f));
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Gates, Fences/Fence_2B.prefab",
            "Fence_2B west", new Vector3(67.8f, 1f, 40f));
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Gates, Fences/Fence_2G.prefab",
            "Fence_2G west", new Vector3(67.8f, 1f, 62f));
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Walls, Posts/Stone Wall Large.prefab",
            "Stone Wall Large", new Vector3(92f, 1f, 54f));
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Walls, Posts/Stone Wall.prefab",
            "Stone Wall", new Vector3(86f, 1f, 70f));
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Walls, Posts/Stone Wall 2.prefab",
            "Stone Wall 2", new Vector3(86f, 1f, 38f));
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Walls, Posts/Stone Post.prefab",
            "Stone Post SW", new Vector3(67.5f, 1f, 36f));
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Walls, Posts/Stone Post.prefab",
            "Stone Post NW", new Vector3(67.5f, 1f, 72f));
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Buildings/Small Tomb.prefab",
            "Small Tomb", new Vector3(84f, 1f, 64f));

        string[] graveTypes =
        {
            "Grave 1", "Grave 2", "Grave 3", "Grave 4",
            "Grave 5", "Grave 6", "Grave 1", "Grave 2",
            "Grave 3", "Grave 5", "Grave 6", "Grave 4",
            "Grave 2", "Grave 1", "Grave 6", "Grave 3"
        };
        float[] graveZ = { 40f, 45f, 50f, 55f };
        float[] graveX = { 72f, 76.5f, 81f, 85.5f };
        int n = 0;
        for (int iz = 0; iz < graveZ.Length; iz++)
        {
            for (int ix = 0; ix < graveX.Length; ix++)
            {
                string type = graveTypes[n];
                Place(cemetery, $"Assets/Cemetery Kit V1.25/Prefabs/Graves/{type}.prefab",
                    $"{type} {ix + 1}-{iz + 1}", new Vector3(graveX[ix], 1f, graveZ[iz]));
                n++;
            }
        }

        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Graves/Gravestones/Grave 1.prefab",
            "Gravestone 1", new Vector3(73f, 1f, 59.5f));
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Graves/Gravestones/Grave 7.prefab",
            "Gravestone 7", new Vector3(77.5f, 1f, 59.5f));
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Graves/Gravestones/Grave 1.prefab",
            "Gravestone 1 b", new Vector3(82f, 1f, 59.5f));
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Graves/Gravestones/Cross 1.prefab",
            "Cross 1", new Vector3(74f, 1f, 66f));
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Graves/Gravestones/Cross 3.prefab",
            "Cross 3", new Vector3(80f, 1f, 66f));
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Graves/Gravestones/Cross 1.prefab",
            "Cross 1 b", new Vector3(86f, 1f, 66f));
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Graves/Coffin Closed.prefab",
            "Coffin Closed", new Vector3(70.5f, 1f, 47f));
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Graves/Coffin Closed Standing.prefab",
            "Coffin Closed Standing", new Vector3(70.5f, 1f, 63f));
        Place(cemetery, "Assets/Cemetery Kit V1.25/Prefabs/Graves/Coffin Closed.prefab",
            "Coffin Closed b", new Vector3(88.5f, 1f, 48f));

        Undo.SetCurrentGroupName("Rebuild Kasal Environment");
        EditorUtility.DisplayDialog(
            "Kasal environment",
            "Rebuilt Town Barrio, Kasal Church, and Cemetery.\n\n" +
            "Pews/altar are placed on the church's Z nave. If they sit outside the walls, rotate or slide the Kasal Church children in the Inspector.\n" +
            "Do not put the grey Plane back under the church floor.",
            "OK");
    }

    static Transform GetOrCreateRoot(string name)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go.transform;
    }

    static void DestroyRoot(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);
    }

    static void DestroySceneRootCopies()
    {
        string[] names =
        {
            "Church 1 Open", "Cross Arch", "Stage", "Podium", "Cross",
            "House 3 and Roof", "House 3 and Roof Alt",
            "Town House 1", "Town House 1 Alt", "Cellar 1",
            "Fence_Gate_Group_1A", "Stone Wall Large", "Stone Wall", "Stone Wall 2",
            "Small Tomb", "Gravestone 1", "Gravestone 7", "Gravestone 1 b",
            "Cross 1", "Cross 3", "Cross 1 b",
            "Coffin Closed", "Coffin Closed Standing", "Coffin Closed b",
            "Stone Post SW", "Stone Post NW",
            "Fence_Wall_Group north", "Fence_Wall_Group east", "Fence_Wall_Group south",
            "Fence_1E west 1", "Fence_1E west 2", "Fence_2B west", "Fence_2G west",
            "Lantern path 1", "Lantern path 2", "Lantern path 3", "Lantern church yard",
            "Storage plaza", "Storage kitchen", "Storage path",
            "Bench 2 L back", "Bench 2 R back"
        };

        foreach (string name in names)
        {
            var go = GameObject.Find(name);
            if (go != null && go.transform.parent == null)
                Undo.DestroyObjectImmediate(go);
        }

        foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go == null || go.transform.parent != null)
                continue;
            string n = go.name;
            if (n.StartsWith("Bench 1 L") || n.StartsWith("Bench 1 R") ||
                n.StartsWith("Grave 1 ") || n.StartsWith("Grave 2 ") ||
                n.StartsWith("Grave 3 ") || n.StartsWith("Grave 4 ") ||
                n.StartsWith("Grave 5 ") || n.StartsWith("Grave 6 "))
                Undo.DestroyObjectImmediate(go);
        }
    }

    static void Place(Transform parent, string assetPath, string name, Vector3 position, float extraYaw = 0f)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogError("Missing prefab: " + assetPath);
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instance, "Place " + name);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = position;
        if (Mathf.Abs(extraYaw) > 0.01f)
            instance.transform.localRotation = Quaternion.Euler(0f, extraYaw, 0f) * instance.transform.localRotation;
    }
}
