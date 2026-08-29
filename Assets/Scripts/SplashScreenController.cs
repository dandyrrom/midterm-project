using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class SplashScreenController : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] string nextScene = "Backstory";
    [SerializeField, Min(1f)] float displayDuration = 6f;
    [SerializeField, Min(0f)] float inputDelay = 1.25f;
    [SerializeField, Min(0.1f)] float fadeDuration = 0.8f;

    [Header("Camera")]
    [SerializeField] float cameraDrift = 0.35f;
    [SerializeField] float cameraDriftSpeed = 0.22f;

    Texture2D darkTexture;
    Texture2D goldTexture;
    GUIStyle titleStyle;
    GUIStyle subtitleStyle;
    GUIStyle loreStyle;
    GUIStyle promptStyle;
    Vector3 cameraStart;
    float startedAt;
    bool isLoading;

    void Awake()
    {
        startedAt = Time.unscaledTime;
        if (Camera.main != null)
            cameraStart = Camera.main.transform.position;

        darkTexture = MakeTexture(new Color(0.015f, 0.012f, 0.018f, 0.92f));
        goldTexture = MakeTexture(new Color(0.78f, 0.58f, 0.24f, 1f));
    }

    void Update()
    {
        float elapsed = Time.unscaledTime - startedAt;
        if (Camera.main != null)
        {
            Vector3 drift = Vector3.right * (Mathf.Sin(elapsed * cameraDriftSpeed) * cameraDrift);
            Camera.main.transform.position = cameraStart + drift;
        }

        Keyboard keyboard = Keyboard.current;
        bool skipPressed = keyboard != null &&
            (keyboard.anyKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame);

        if (!isLoading && (elapsed >= displayDuration || (elapsed >= inputDelay && skipPressed)))
            BeginTransition();
    }

    void BeginTransition()
    {
        isLoading = true;
        if (Application.CanStreamedLevelBeLoaded(nextScene))
            SceneManager.LoadScene(nextScene);
        else
            Debug.LogWarning($"Splash screen could not load scene '{nextScene}'. Add it to Build Settings.");
    }

    void OnGUI()
    {
        EnsureStyles();

        float width = Screen.width;
        float height = Screen.height;
        float scale = Mathf.Clamp(Mathf.Min(width / 1920f, height / 1080f), 0.65f, 1.5f);
        float elapsed = Time.unscaledTime - startedAt;
        float fadeIn = Mathf.Clamp01(elapsed / fadeDuration);
        float fadeOut = Mathf.Clamp01((displayDuration - elapsed) / fadeDuration);
        float alpha = Mathf.Min(fadeIn, fadeOut);

        Color previousColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);

        GUI.DrawTexture(new Rect(0f, 0f, width * 0.62f, height), darkTexture);
        GUI.DrawTexture(new Rect(width * 0.075f, height * 0.22f, 92f * scale, 3f * scale), goldTexture);

        Rect content = new Rect(width * 0.075f, height * 0.26f, width * 0.48f, height * 0.6f);
        titleStyle.fontSize = Mathf.RoundToInt(94f * scale);
        subtitleStyle.fontSize = Mathf.RoundToInt(23f * scale);
        loreStyle.fontSize = Mathf.RoundToInt(25f * scale);
        promptStyle.fontSize = Mathf.RoundToInt(18f * scale);

        GUI.Label(new Rect(content.x, content.y, content.width, 120f * scale), "LUNAS", titleStyle);
        GUI.Label(
            new Rect(content.x + 5f * scale, content.y + 104f * scale, content.width, 40f * scale),
            "A FILIPINO FOLK-HORROR STEALTH ADVENTURE",
            subtitleStyle);
        GUI.Label(
            new Rect(content.x + 5f * scale, content.y + 195f * scale, content.width * 0.88f, 150f * scale),
            "On the night of Sherall's wedding,\nsomething answered the church bells.\n\nComplete the rite. Save what remains.",
            loreStyle);

        string prompt = isLoading ? "ENTERING THE KASAL..." : "PRESS ANY KEY";
        GUI.Label(
            new Rect(content.x + 5f * scale, height - 105f * scale, content.width, 40f * scale),
            prompt,
            promptStyle);

        GUI.color = previousColor;
    }

    void EnsureStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.9f, 0.76f, 0.48f) }
        };
        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.82f, 0.76f, 0.66f) }
        };
        loreStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            wordWrap = true,
            normal = { textColor = new Color(0.93f, 0.9f, 0.84f) }
        };
        promptStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.78f, 0.58f, 0.24f) }
        };
    }

    static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    void OnDestroy()
    {
        if (darkTexture != null)
            Destroy(darkTexture);
        if (goldTexture != null)
            Destroy(goldTexture);
    }
}
