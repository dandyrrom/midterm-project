using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class PauseMenuController : MonoBehaviour
{
    const string SplashSceneName = "SplashScreen";

    [Header("Flow")]
    [SerializeField] string titleScene = SplashSceneName;

    [Header("Typography")]
    [SerializeField] Font bloodVictimZombieFont;

    readonly string[] menuItems = { "RESUME", "HOW TO PLAY", "QUIT TO TITLE" };

    Texture2D darkTexture;
    Texture2D panelTexture;
    Texture2D goldTexture;
    Texture2D discTexture;
    Texture2D ringTexture;
    GUIStyle titleStyle;
    GUIStyle subtitleStyle;
    GUIStyle menuStyle;
    GUIStyle selectedMenuStyle;
    GUIStyle bodyStyle;
    GUIStyle promptStyle;
    GUIStyle markStyle;
    bool paused;
    bool showHowToPlay;
    int selectedItem;
    CursorLockMode previousLockMode;
    bool previousCursorVisible;
    float previousTimeScale = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(sceneName) || sceneName == SplashSceneName)
            return;
        if (FindObjectOfType<PauseMenuController>() != null)
            return;

        GameObject host = new GameObject("PauseMenu");
        host.AddComponent<PauseMenuController>();
    }

    void Awake()
    {
        ResolveMenuFont();
        darkTexture = MakeTexture(new Color(0.008f, 0.006f, 0.012f, 0.97f));
        panelTexture = MakeTexture(new Color(0.035f, 0.006f, 0.012f, 0.9f));
        goldTexture = MakeTexture(new Color(0.86f, 0.16f, 0.18f, 1f));
        discTexture = MakeDisc(256, new Color(0.1f, 0.02f, 0.03f, 1f));
        ringTexture = MakeRing(256, new Color(0.92f, 0.78f, 0.55f, 1f), 0.78f, 0.97f);
    }

    void ResolveMenuFont()
    {
        if (bloodVictimZombieFont != null)
            return;

        bloodVictimZombieFont = Resources.Load<Font>("Blood Victim Zombie");
#if UNITY_EDITOR
        if (bloodVictimZombieFont == null)
        {
            string[] fontGuids = UnityEditor.AssetDatabase.FindAssets("Blood Victim Zombie t:Font");
            if (fontGuids.Length > 0)
            {
                string fontPath = UnityEditor.AssetDatabase.GUIDToAssetPath(fontGuids[0]);
                bloodVictimZombieFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            }
        }
#endif
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            if (showHowToPlay)
                showHowToPlay = false;
            else if (paused)
                Resume();
            else
                Pause();
            return;
        }

        if (!paused)
            return;

        if (showHowToPlay)
        {
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                showHowToPlay = false;
            return;
        }

        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
            selectedItem = (selectedItem + menuItems.Length - 1) % menuItems.Length;
        if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
            selectedItem = (selectedItem + 1) % menuItems.Length;
        if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
            ActivateMenuItem(selectedItem);
    }

    void Pause()
    {
        paused = true;
        showHowToPlay = false;
        selectedItem = 0;
        previousTimeScale = Time.timeScale;
        previousLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Resume()
    {
        paused = false;
        showHowToPlay = false;
        Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
        Cursor.lockState = previousLockMode;
        Cursor.visible = previousCursorVisible;
    }

    void ActivateMenuItem(int index)
    {
        selectedItem = index;
        switch (index)
        {
            case 0:
                Resume();
                break;
            case 1:
                showHowToPlay = true;
                break;
            case 2:
                QuitToTitle();
                break;
        }
    }

    void QuitToTitle()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (Application.CanStreamedLevelBeLoaded(titleScene))
            SceneManager.LoadScene(titleScene);
        else
            Debug.LogError($"Cannot load '{titleScene}'. Add the scene to Build Settings.");
    }

    void OnGUI()
    {
        if (!paused)
            return;

        EnsureStyles();
        DrawMenu();
        if (showHowToPlay)
            DrawHowToPlay();
    }

    void DrawMenu()
    {
        float width = Screen.width;
        float height = Screen.height;
        float scale = UiScale();
        float panelWidth = Mathf.Max(width * 0.46f, 520f * scale);

        GUI.DrawTexture(new Rect(0f, 0f, width, height), darkTexture);
        GUI.DrawTexture(new Rect(0f, 0f, panelWidth, height), panelTexture);
        GUI.DrawTexture(new Rect(panelWidth, 0f, 2f * scale, height), goldTexture);

        titleStyle.fontSize = Mathf.RoundToInt(148f * scale);
        subtitleStyle.fontSize = Mathf.RoundToInt(22f * scale);
        DrawMark(new Rect(68f * scale, 22f * scale, 78f * scale, 78f * scale), 78f * scale);

        GUI.Label(
            new Rect(62f * scale, 108f * scale, panelWidth - 100f * scale, 170f * scale),
            "LUNAS",
            LeftAligned(titleStyle));
        GUI.Label(
            new Rect(68f * scale, 278f * scale, panelWidth - 100f * scale, 40f * scale),
            "PAUSED",
            LeftAligned(subtitleStyle));

        float buttonY = height * 0.42f;
        float buttonHeight = 68f * scale;
        menuStyle.fontSize = Mathf.RoundToInt(32f * scale);
        selectedMenuStyle.fontSize = menuStyle.fontSize;

        for (int i = 0; i < menuItems.Length; i++)
        {
            Rect buttonRect = new Rect(
                68f * scale,
                buttonY + i * 88f * scale,
                panelWidth - 136f * scale,
                buttonHeight);

            if (i == selectedItem)
                GUI.DrawTexture(
                    new Rect(buttonRect.x - 18f * scale, buttonRect.y + 12f * scale, 4f * scale, 42f * scale),
                    goldTexture);

            GUIStyle style = i == selectedItem ? selectedMenuStyle : menuStyle;
            if (GUI.Button(buttonRect, menuItems[i], style))
                ActivateMenuItem(i);
        }

        promptStyle.fontSize = Mathf.RoundToInt(18f * scale);
        GUI.Label(
            new Rect(68f * scale, height - 74f * scale, panelWidth - 100f * scale, 34f * scale),
            "ESC  RESUME     W/S OR ↑/↓  SELECT     ENTER  CONFIRM",
            LeftAligned(promptStyle));
    }

    void DrawHowToPlay()
    {
        float width = Screen.width;
        float height = Screen.height;
        float scale = UiScale();
        Rect box = new Rect(width * 0.5f, height * 0.14f, width * 0.43f, height * 0.72f);
        GUI.DrawTexture(box, darkTexture);

        subtitleStyle.fontSize = Mathf.RoundToInt(32f * scale);
        bodyStyle.fontSize = Mathf.RoundToInt(24f * scale);
        promptStyle.fontSize = Mathf.RoundToInt(18f * scale);

        GUI.Label(
            new Rect(box.x + 38f * scale, box.y + 28f * scale, box.width - 76f * scale, 52f * scale),
            "HOW TO PLAY",
            LeftAligned(subtitleStyle));
        GUI.DrawTexture(
            new Rect(box.x + 38f * scale, box.y + 90f * scale, 80f * scale, 3f * scale),
            goldTexture);
        GUI.Label(
            new Rect(box.x + 38f * scale, box.y + 118f * scale, box.width - 76f * scale, box.height - 200f * scale),
            "WASD     Move\nSHIFT      Run\nSPACE      Jump\nMOUSE      Look\n\nStay quiet. The aswang hunt by sound.\nCollect bawang and a candle.\nUse them to kill every aswang.\n\nGOAL: KILL ALL ASWANGS.",
            bodyStyle);
        GUI.Label(
            new Rect(box.x + 38f * scale, box.yMax - 62f * scale, box.width - 76f * scale, 34f * scale),
            "ENTER / SPACE / ESC  BACK",
            LeftAligned(promptStyle));
    }

    void EnsureStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = CreateStyle(FontStyle.Normal, new Color(0.9f, 0.08f, 0.1f), TextAnchor.MiddleCenter);
        subtitleStyle = CreateStyle(FontStyle.Normal, new Color(0.92f, 0.78f, 0.7f), TextAnchor.MiddleCenter);
        menuStyle = CreateStyle(FontStyle.Normal, new Color(0.82f, 0.7f, 0.68f), TextAnchor.MiddleLeft);
        selectedMenuStyle = CreateStyle(FontStyle.Normal, new Color(1f, 0.2f, 0.22f), TextAnchor.MiddleLeft);
        bodyStyle = CreateStyle(FontStyle.Normal, new Color(0.94f, 0.91f, 0.86f), TextAnchor.UpperLeft);
        bodyStyle.wordWrap = true;
        promptStyle = CreateStyle(FontStyle.Normal, new Color(0.62f, 0.58f, 0.54f), TextAnchor.MiddleCenter);
        markStyle = CreateStyle(FontStyle.Bold, new Color(0.95f, 0.12f, 0.14f), TextAnchor.MiddleCenter);

        if (bloodVictimZombieFont != null)
        {
            titleStyle.font = bloodVictimZombieFont;
            subtitleStyle.font = bloodVictimZombieFont;
            menuStyle.font = bloodVictimZombieFont;
            selectedMenuStyle.font = bloodVictimZombieFont;
            markStyle.font = bloodVictimZombieFont;
        }
    }

    void DrawMark(Rect area, float size)
    {
        if (discTexture != null)
            GUI.DrawTexture(area, discTexture, ScaleMode.ScaleToFit, true);
        if (ringTexture != null)
            GUI.DrawTexture(area, ringTexture, ScaleMode.ScaleToFit, true);

        float cx = area.x + area.width * 0.5f;
        float cy = area.y + area.height * 0.52f;
        GUI.DrawTexture(new Rect(cx - size * 0.018f, cy - size * 0.28f, size * 0.036f, size * 0.2f), goldTexture);
        GUI.DrawTexture(new Rect(cx - size * 0.028f, cy - size * 0.34f, size * 0.056f, size * 0.06f), goldTexture);
        if (markStyle != null)
        {
            markStyle.fontSize = Mathf.RoundToInt(size * 0.52f);
            GUI.Label(area, "L", markStyle);
        }
    }

    static GUIStyle CreateStyle(FontStyle fontStyle, Color color, TextAnchor alignment)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = alignment,
            fontStyle = fontStyle
        };
        style.normal.textColor = color;
        style.hover.textColor = new Color(0.94f, 0.79f, 0.5f);
        style.active.textColor = color;
        return style;
    }

    static GUIStyle LeftAligned(GUIStyle source)
    {
        GUIStyle copy = new GUIStyle(source);
        copy.alignment = TextAnchor.MiddleLeft;
        return copy;
    }

    static float UiScale()
    {
        return Mathf.Clamp(Mathf.Min(Screen.width / 1920f, Screen.height / 1080f), 0.65f, 1.5f);
    }

    static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    static Texture2D MakeDisc(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        float center = (size - 1) * 0.5f;
        float radius = center - 1f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float t = dx * dx + dy * dy;
                texture.SetPixel(x, y, t <= radius * radius ? color : Color.clear);
            }
        }

        texture.Apply();
        return texture;
    }

    static Texture2D MakeRing(int size, Color color, float inner, float outer)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        float center = (size - 1) * 0.5f;
        float outerR = center * outer;
        float innerR = center * inner;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                texture.SetPixel(x, y, d <= outerR && d >= innerR ? color : Color.clear);
            }
        }

        texture.Apply();
        return texture;
    }

    void OnDestroy()
    {
        if (paused)
            Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;

        if (darkTexture != null)
            Destroy(darkTexture);
        if (panelTexture != null)
            Destroy(panelTexture);
        if (goldTexture != null)
            Destroy(goldTexture);
        if (discTexture != null)
            Destroy(discTexture);
        if (ringTexture != null)
            Destroy(ringTexture);
    }
}
