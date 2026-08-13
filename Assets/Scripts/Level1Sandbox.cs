using UnityEngine;

/// <summary>
/// Spawns the Level 1 sandbox around the Mixamo girl when Play is pressed.
/// Veil and decoy are intentionally not implemented yet.
/// </summary>
public class Level1Sandbox : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (FindFirstObjectByType<ThirdPersonController>() == null)
            return;
        if (FindFirstObjectByType<Level1Sandbox>() != null)
            return;

        var go = new GameObject("Level1Sandbox");
        go.AddComponent<Level1Sandbox>();
    }

    void Start()
    {
        var player = ThirdPersonController.Player;
        if (player == null)
            return;

        if (GetComponent<NoiseBus>() == null)
            gameObject.AddComponent<NoiseBus>();
        if (GetComponent<RainDirector>() == null)
            gameObject.AddComponent<RainDirector>();
        if (player.GetComponent<PlayerCombatResources>() == null)
            player.gameObject.AddComponent<PlayerCombatResources>();

        SpawnKnockables(player.transform.position);
        SpawnAswang(player.transform.position);
    }

    void SpawnKnockables(Vector3 origin)
    {
        Vector3[] offsets =
        {
            new Vector3(2.2f, 0.4f, 1.4f),
            new Vector3(-1.8f, 0.4f, 2.0f),
            new Vector3(3.1f, 0.4f, -1.2f),
            new Vector3(-2.6f, 0.4f, -1.8f),
            new Vector3(1.2f, 0.4f, 3.3f),
            new Vector3(-3.4f, 0.4f, 0.4f),
            new Vector3(4.0f, 0.4f, 2.4f),
            new Vector3(0.6f, 0.4f, -3.1f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"Knockable_{i + 1}";
            cube.transform.SetParent(transform, true);
            cube.transform.position = origin + offsets[i];
            cube.transform.localScale = new Vector3(0.45f, 0.55f, 0.45f);
            cube.GetComponent<Renderer>().material.color = new Color(0.55f, 0.38f, 0.22f);
            cube.AddComponent<Rigidbody>();
            cube.AddComponent<KnockableProp>();
        }
    }

    void SpawnAswang(Vector3 origin)
    {
        Vector3[] offsets =
        {
            new Vector3(8f, 0f, 3f),
            new Vector3(-7f, 0f, 6f),
            new Vector3(6f, 0f, -8f),
            new Vector3(-9f, 0f, -4f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = $"Aswang_{i + 1}";
            body.transform.SetParent(transform, true);
            body.transform.position = origin + offsets[i] + Vector3.up;
            body.transform.localScale = new Vector3(0.7f, 1f, 0.7f);
            body.GetComponent<Renderer>().material.color = new Color(0.35f, 0.08f, 0.08f);
            Object.Destroy(body.GetComponent<CapsuleCollider>());
            var col = body.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.radius = 0.4f;
            col.isTrigger = true;
            var rb = body.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            body.AddComponent<AswangController>();
        }
    }

    void OnGUI()
    {
        var combat = PlayerCombatResources.Instance;
        var motor = ThirdPersonController.Player;
        var rain = RainDirector.Instance;
        if (motor == null)
            return;

        const int w = 420;
        var box = new Rect(16, 16, w, combat != null && combat.IsDead ? 150 : 118);
        GUI.Box(box, "");
        GUILayout.BeginArea(new Rect(box.x + 10, box.y + 8, box.width - 20, box.height - 12));
        GUILayout.Label("LEVEL 1  |  morning rain  |  sound stealth");
        if (combat != null)
            GUILayout.Label($"Lives: {combat.lives}    Lunas: {combat.lunas}    Stamina: {Mathf.RoundToInt(motor.Stamina)}{(motor.IsExhausted ? "  EXHAUSTED" : "")}");
        if (rain != null)
            GUILayout.Label($"Rain mask: {Mathf.RoundToInt(rain.intensity * 100f)}%  (heavier rain = quieter)");
        GUILayout.Label("WASD move   Shift sprint   F / Enter throw lunas   R restart");
        if (combat != null && combat.IsDead)
            GUILayout.Label("YOU DIED — out of lives. Press R.");
        GUILayout.EndArea();
    }
}
