using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class SplashScreenController : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] string nextScene = "Backstory1";
    [SerializeField, Min(1f)] float displayDuration = 3.5f;
    [SerializeField, Min(0f)] float inputDelay = 0.6f;
    [SerializeField, Min(0.1f)] float fadeDuration = 0.8f;

    [Header("Camera")]
    [SerializeField] float cameraDrift = 0.35f;
    [SerializeField] float cameraDriftSpeed = 0.22f;

    enum ScreenState
    {
        Splash,
        Menu,
        HowToPlay,
        Loading
    }

    readonly string[] menuItems = { "BEGIN THE STORY", "HOW TO PLAY", "QUIT" };

    Texture2D darkTexture;
    Texture2D panelTexture;
    Texture2D goldTexture;
    GUIStyle titleStyle;
    GUIStyle subtitleStyle;
    GUIStyle menuStyle;
    GUIStyle selectedMenuStyle;
    GUIStyle bodyStyle;
    GUIStyle promptStyle;
    Vector3 cameraStart;
    ScreenState state;
    float stateStartedAt;
    int selectedItem;

    void Awake()
    {
        state = ScreenState.Splash;
        stateStartedAt = Time.unscaledTime;
        if (Camera.main != null)
            cameraStart = Camera.main.transform.position;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        darkTexture = MakeTexture(new Color(0.008f, 0.006f, 0.012f, 0.97f));
        panelTexture = MakeTexture(new Color(0.015f, 0.012f, 0.02f, 0.88f));
        goldTexture = MakeTexture(new Color(0.78f, 0.58f, 0.24f, 1f));
    }

    void Update()
    {
        float elapsed = Time.unscaledTime - stateStartedAt;
        if (Camera.main != null)
        {
            Vector3 drift = Vector3.right * (Mathf.Sin(elapsed * cameraDriftSpeed) * cameraDrift);
            Camera.main.transform.position = cameraStart + drift;
        }

        Keyboard keyboard = Keyboard.current;
        if (state == ScreenState.Splash)
        {
            bool keyboardSkip = keyboard != null && keyboard.anyKey.wasPressedThisFrame;
            bool mouseSkip = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (elapsed >= displayDuration || (elapsed >= inputDelay && (keyboardSkip || mouseSkip)))
                ShowMenu();
            return;
        }

        if (state == ScreenState.Menu && keyboard != null)
        {
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
                selectedItem = (selectedItem + menuItems.Length - 1) % menuItems.Length;
            if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
                selectedItem = (selectedItem + 1) % menuItems.Length;
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                ActivateMenuItem(selectedItem);
        }
        else if (state == ScreenState.HowToPlay && keyboard != null &&
                 (keyboard.escapeKey.wasPressedThisFrame ||
                  keyboard.enterKey.wasPressedThisFrame ||
                  keyboard.spaceKey.wasPressedThisFrame))
        {
            ShowMenu();
        }
    }

    void ShowMenu()
    {
        state = ScreenState.Menu;
        stateStartedAt = Time.unscaledTime;
    }

    void ActivateMenuItem(int index)
    {
        selectedItem = index;
        switch (index)
        {
            case 0:
                StartCoroutine(BeginStory());
                break;
            case 1:
                state = ScreenState.HowToPlay;
                stateStartedAt = Time.unscaledTime;
                break;
            case 2:
                QuitGame();
                break;
        }
    }

    IEnumerator BeginStory()
    {
        if (state == ScreenState.Loading)
            yield break;

        state = ScreenState.Loading;
        stateStartedAt = Time.unscaledTime;
        yield return new WaitForSecondsRealtime(0.35f);

        if (Application.CanStreamedLevelBeLoaded(nextScene))
            SceneManager.LoadScene(nextScene);
        else
        {
            Debug.LogError($"Cannot load '{nextScene}'. Add the scene to Build Settings.");
            ShowMenu();
        }
    }

    static void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("Quit selected. Application.Quit only closes a built game.");
#else
        Application.Quit();
#endif
    }

    void OnGUI()
    {
        EnsureStyles();
        switch (state)
        {
            case ScreenState.Splash:
                DrawSplash();
                break;
            case ScreenState.Menu:
                DrawMenu();
                break;
            case ScreenState.HowToPlay:
                DrawMenu();
                DrawHowToPlay();
                break;
            case ScreenState.Loading:
                DrawLoading();
                break;
        }
    }

    void DrawSplash()
    {
        float width = Screen.width;
        float height = Screen.height;
        float scale = UiScale();
        float elapsed = Time.unscaledTime - stateStartedAt;
        float fadeIn = Mathf.Clamp01(elapsed / fadeDuration);
        float fadeOut = Mathf.Clamp01((displayDuration - elapsed) / fadeDuration);
        float alpha = Mathf.Min(fadeIn, fadeOut);

        GUI.DrawTexture(new Rect(0f, 0f, width, height), darkTexture);

        Color previous = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        titleStyle.fontSize = Mathf.RoundToInt(122f * scale);
        subtitleStyle.fontSize = Mathf.RoundToInt(22f * scale);
        promptStyle.fontSize = Mathf.RoundToInt(16f * scale);

        GUI.Label(
            new Rect(0f, height * 0.31f, width, 150f * scale),
            "LUNAS",
            titleStyle);
        GUI.DrawTexture(
            new Rect(width * 0.5f - 75f * scale, height * 0.49f, 150f * scale, 3f * scale),
            goldTexture);
        GUI.Label(
            new Rect(0f, height * 0.515f, width, 44f * scale),
            "THE WEDDING NIGHT",
            subtitleStyle);
        GUI.Label(
            new Rect(0f, height * 0.82f, width, 30f * scale),
            "ORASYON  •  ASIN  •  BAWANG  •  BANAL NA TUBIG",
            promptStyle);
        GUI.Label(
            new Rect(0f, height * 0.91f, width, 26f * scale),
            "PRESS ANY KEY",
            promptStyle);
        GUI.color = previous;
    }

    void DrawMenu()
    {
        float width = Screen.width;
        float height = Screen.height;
        float scale = UiScale();
        float panelWidth = Mathf.Max(width * 0.46f, 520f * scale);

        GUI.DrawTexture(new Rect(0f, 0f, panelWidth, height), panelTexture);
        GUI.DrawTexture(new Rect(panelWidth, 0f, 2f * scale, height), goldTexture);

        titleStyle.fontSize = Mathf.RoundToInt(82f * scale);
        subtitleStyle.fontSize = Mathf.RoundToInt(17f * scale);
        GUI.Label(
            new Rect(62f * scale, 82f * scale, panelWidth - 100f * scale, 100f * scale),
            "LUNAS",
            LeftAligned(titleStyle));
        GUI.Label(
            new Rect(68f * scale, 172f * scale, panelWidth - 100f * scale, 34f * scale),
            "A FILIPINO FOLK-HORROR STORY",
            LeftAligned(subtitleStyle));

        float buttonY = height * 0.4f;
        float buttonHeight = 58f * scale;
        menuStyle.fontSize = Mathf.RoundToInt(25f * scale);
        selectedMenuStyle.fontSize = menuStyle.fontSize;

        for (int i = 0; i < menuItems.Length; i++)
        {
            Rect buttonRect = new Rect(
                68f * scale,
                buttonY + i * 76f * scale,
                panelWidth - 136f * scale,
                buttonHeight);

            if (i == selectedItem)
                GUI.DrawTexture(
                    new Rect(buttonRect.x - 18f * scale, buttonRect.y + 9f * scale, 4f * scale, 35f * scale),
                    goldTexture);

            GUIStyle style = i == selectedItem ? selectedMenuStyle : menuStyle;
            if (GUI.Button(buttonRect, menuItems[i], style))
                ActivateMenuItem(i);
        }

        promptStyle.fontSize = Mathf.RoundToInt(15f * scale);
        GUI.Label(
            new Rect(68f * scale, height - 70f * scale, panelWidth - 100f * scale, 28f * scale),
            "W/S OR ↑/↓  SELECT     ENTER  CONFIRM",
            LeftAligned(promptStyle));
    }

    void DrawHowToPlay()
    {
        float width = Screen.width;
        float height = Screen.height;
        float scale = UiScale();
        Rect box = new Rect(width * 0.5f, height * 0.16f, width * 0.43f, height * 0.68f);
        GUI.DrawTexture(box, darkTexture);

        subtitleStyle.fontSize = Mathf.RoundToInt(25f * scale);
        bodyStyle.fontSize = Mathf.RoundToInt(20f * scale);
        promptStyle.fontSize = Mathf.RoundToInt(15f * scale);

        GUI.Label(
            new Rect(box.x + 38f * scale, box.y + 32f * scale, box.width - 76f * scale, 42f * scale),
            "HOW TO PLAY",
            LeftAligned(subtitleStyle));
        GUI.DrawTexture(
            new Rect(box.x + 38f * scale, box.y + 84f * scale, 80f * scale, 3f * scale),
            goldTexture);
        GUI.Label(
            new Rect(box.x + 38f * scale, box.y + 112f * scale, box.width - 76f * scale, box.height - 190f * scale),
            "WASD     Move\nSHIFT      Run\nSPACE      Jump\nMOUSE      Look\n\nStay quiet. The aswang hunt by sound.\nFind holy water, asin, and bawang.\nReturn to the altar and complete the lunas.",
            bodyStyle);
        GUI.Label(
            new Rect(box.x + 38f * scale, box.yMax - 58f * scale, box.width - 76f * scale, 28f * scale),
            "ENTER / SPACE / ESC  BACK",
            LeftAligned(promptStyle));
    }

    void DrawLoading()
    {
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), darkTexture);
        subtitleStyle.fontSize = Mathf.RoundToInt(22f * UiScale());
        GUI.Label(
            new Rect(0f, Screen.height * 0.48f, Screen.width, 40f * UiScale()),
            "ENTERING THE WEDDING...",
            subtitleStyle);
    }

    void EnsureStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = CreateStyle(FontStyle.Bold, new Color(0.9f, 0.76f, 0.48f), TextAnchor.MiddleCenter);
        subtitleStyle = CreateStyle(FontStyle.Bold, new Color(0.82f, 0.76f, 0.66f), TextAnchor.MiddleCenter);
        menuStyle = CreateStyle(FontStyle.Normal, new Color(0.75f, 0.71f, 0.66f), TextAnchor.MiddleLeft);
        selectedMenuStyle = CreateStyle(FontStyle.Bold, new Color(0.94f, 0.79f, 0.5f), TextAnchor.MiddleLeft);
        bodyStyle = CreateStyle(FontStyle.Normal, new Color(0.94f, 0.91f, 0.86f), TextAnchor.UpperLeft);
        bodyStyle.wordWrap = true;
        promptStyle = CreateStyle(FontStyle.Normal, new Color(0.62f, 0.58f, 0.54f), TextAnchor.MiddleCenter);
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

    void OnDestroy()
    {
        if (darkTexture != null)
            Destroy(darkTexture);
        if (panelTexture != null)
            Destroy(panelTexture);
        if (goldTexture != null)
            Destroy(goldTexture);
    }
}
