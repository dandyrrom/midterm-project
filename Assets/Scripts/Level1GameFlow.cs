using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Level 1 objective flip: kill all aswangs → looping church bell → go to bloody krus → end panel.
/// Game over panel when lives run out before clearing.
/// Assign Objective Text in the Inspector so you can move/font/style it in the scene.
/// </summary>
public class Level1GameFlow : MonoBehaviour
{
    public static Level1GameFlow Instance { get; private set; }

    [Header("Objectives")]
    [Tooltip("Written into Objective Text at start / when phase A is active.")]
    public string phaseAObjective = "Kill all aswangs";
    [Tooltip("Written into Objective Text when all aswangs are dead.")]
    public string phaseBObjective = "Go to the bloody krus inside the old church";
    [Tooltip("Drag the scene ObjectiveText (TMP) here. Edit that object for position/font/color.")]
    public TMP_Text objectiveText;

    [Header("Objective beat (Phase B)")]
    public bool beatObjectiveInPhaseB = true;
    public float beatSpeed = 3.2f;
    [Range(0f, 0.25f)] public float beatScaleAmount = 0.1f;
    [Range(0.2f, 1f)] public float beatAlphaMin = 0.55f;

    [Header("Audio")]
    public AudioClip churchBellClip;
    [Range(0f, 1f)] public float churchBellVolume = 0.85f;

    [Header("Score")]
    public int pointsPerKill = 100;
    public int hitMissedPenalty = 25;
    public int pointsPerLifeRemaining = 50;

    [Header("Continue")]
    [Tooltip("Scene loaded after SPACE on the end / game-over panel.")]
    public string continueSceneName = "SplashScreen";

    [Header("Krus Marker")]
    public Vector3 krusTriggerSize = new Vector3(2.5f, 3f, 2.5f);
    public Color markerColor = new Color(0.7f, 0f, 0.05f, 0.35f);

    [Header("Optional refs (auto-found if empty)")]
    public ZombieKillScore killScore;
    public PlayerLives lives;
    public Level1RunStats runStats;
    public Canvas hudCanvas;
    public Transform krusAnchor;

    GameObject endPanel;
    TMP_Text endTitleText;
    TMP_Text endBodyText;
    Image endDimmer;

    AudioSource bellSource;
    GameObject krusMarker;
    bool phaseB;
    bool panelOpen;
    bool waitingForSpace;
    bool beating;

    Vector3 objectiveBaseScale = Vector3.one;
    Color objectiveBaseColor = Color.white;

    public bool IsPhaseB => phaseB;
    public bool IsPanelOpen => panelOpen;

    void Awake()
    {
        Instance = this;

        if (killScore == null)
            killScore = FindFirstObjectByType<ZombieKillScore>();
        if (lives == null)
            lives = FindFirstObjectByType<PlayerLives>();
        if (runStats == null)
            runStats = FindFirstObjectByType<Level1RunStats>();
        if (runStats == null)
            runStats = gameObject.AddComponent<Level1RunStats>();

        if (hudCanvas == null)
        {
            GameObject hud = GameObject.Find("HUD");
            if (hud != null)
                hudCanvas = hud.GetComponent<Canvas>();
        }

        if (objectiveText == null)
        {
            GameObject existing = GameObject.Find("ObjectiveText");
            if (existing != null)
                objectiveText = existing.GetComponent<TMP_Text>();
        }

        bellSource = gameObject.AddComponent<AudioSource>();
        bellSource.playOnAwake = false;
        bellSource.spatialBlend = 0f;
        bellSource.loop = true;
        bellSource.volume = churchBellVolume;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        StopChurchBell();
        Time.timeScale = 1f;
    }

    void OnEnable()
    {
        if (killScore != null)
            killScore.OnScoreChanged += HandleScoreChanged;
    }

    void OnDisable()
    {
        if (killScore != null)
            killScore.OnScoreChanged -= HandleScoreChanged;
    }

    void Start()
    {
        CacheObjectiveVisuals();
        BuildEndPanelIfNeeded();
        SetupKrusMarker();
        SetObjective(phaseAObjective);
        if (krusMarker != null)
            krusMarker.SetActive(false);
    }

    void Update()
    {
        UpdateObjectiveBeat();

        if (!waitingForSpace || Keyboard.current == null)
            return;

        if (!Keyboard.current.spaceKey.wasPressedThisFrame)
            return;

        waitingForSpace = false;
        StartCoroutine(ContinueRoutine());
    }

    void HandleScoreChanged(int killed, int total)
    {
        if (phaseB || panelOpen)
            return;

        if (total <= 0 || killed < total)
            return;

        EnterPhaseB();
    }

    void EnterPhaseB()
    {
        if (phaseB)
            return;

        phaseB = true;
        StartChurchBellLoop();
        SetObjective(phaseBObjective);
        CacheObjectiveVisuals();
        beating = beatObjectiveInPhaseB;

        if (krusMarker != null)
            krusMarker.SetActive(true);
    }

    void StartChurchBellLoop()
    {
        if (bellSource == null || churchBellClip == null)
            return;

        bellSource.clip = churchBellClip;
        bellSource.loop = true;
        bellSource.volume = churchBellVolume;
        if (!bellSource.isPlaying)
            bellSource.Play();
    }

    void StopChurchBell()
    {
        if (bellSource == null)
            return;

        if (bellSource.isPlaying)
            bellSource.Stop();
        bellSource.loop = false;
    }

    public void NotifyKrusReached()
    {
        if (!phaseB || panelOpen)
            return;

        ShowEndPanel(victory: true);
    }

    public void NotifyGameOver()
    {
        if (panelOpen)
            return;

        ShowEndPanel(victory: false);
    }

    void ShowEndPanel(bool victory)
    {
        panelOpen = true;
        waitingForSpace = true;
        beating = false;
        StopChurchBell();
        RestoreObjectiveVisuals();

        FreezeGameplay(true);

        int killed = killScore != null ? killScore.Killed : 0;
        int hits = runStats != null ? runStats.HitsMissed : 0;
        int livesLeft = lives != null ? lives.CurrentLives : 0;
        int totalScore = runStats != null
            ? runStats.ComputeTotalScore(killed, livesLeft, pointsPerKill, hitMissedPenalty, pointsPerLifeRemaining)
            : killed * pointsPerKill;

        if (endTitleText != null)
            endTitleText.text = victory ? "Level 1 Complete" : "Game Over";

        if (endBodyText != null)
        {
            if (victory)
            {
                endBodyText.text =
                    $"Aswang killed: {killed}\n" +
                    $"Hits missed: {hits}\n" +
                    $"Remaining lives: {livesLeft}\n" +
                    $"Total score: {totalScore}\n\n" +
                    "Press SPACE to continue";
            }
            else
            {
                endBodyText.text =
                    $"Hits missed: {hits}\n" +
                    $"Remaining lives: {livesLeft}\n" +
                    $"Total score: {totalScore}\n\n" +
                    "Press SPACE to continue";
            }
        }

        if (endPanel != null)
            endPanel.SetActive(true);
    }

    IEnumerator ContinueRoutine()
    {
        if (endDimmer != null)
        {
            float t = 0f;
            Color c = endDimmer.color;
            while (t < 0.6f)
            {
                t += Time.unscaledDeltaTime;
                c.a = Mathf.Lerp(0.75f, 1f, t / 0.6f);
                endDimmer.color = c;
                yield return null;
            }
        }

        Time.timeScale = 1f;

        if (!string.IsNullOrWhiteSpace(continueSceneName))
            SceneManager.LoadScene(continueSceneName);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void FreezeGameplay(bool freeze)
    {
        Time.timeScale = freeze ? 0f : 1f;

        ThirdPersonController player = FindFirstObjectByType<ThirdPersonController>();
        if (player != null)
            player.enabled = !freeze;

        Cursor.visible = freeze;
        Cursor.lockState = CursorLockMode.None;
    }

    void SetObjective(string text)
    {
        if (objectiveText != null)
            objectiveText.text = text;
    }

    void CacheObjectiveVisuals()
    {
        if (objectiveText == null)
            return;

        objectiveBaseScale = objectiveText.rectTransform.localScale;
        if (objectiveBaseScale.sqrMagnitude < 0.0001f)
            objectiveBaseScale = Vector3.one;

        objectiveBaseColor = objectiveText.color;
    }

    void RestoreObjectiveVisuals()
    {
        if (objectiveText == null)
            return;

        objectiveText.rectTransform.localScale = objectiveBaseScale;
        objectiveText.color = objectiveBaseColor;
    }

    void UpdateObjectiveBeat()
    {
        if (!beating || objectiveText == null || panelOpen)
            return;

        float wave = (Mathf.Sin(Time.time * beatSpeed) + 1f) * 0.5f;
        float scale = 1f + wave * beatScaleAmount;
        objectiveText.rectTransform.localScale = objectiveBaseScale * scale;

        Color c = objectiveBaseColor;
        c.a = Mathf.Lerp(beatAlphaMin, objectiveBaseColor.a, wave);
        objectiveText.color = c;
    }

    void SetupKrusMarker()
    {
        Transform anchor = krusAnchor;
        if (anchor == null)
            anchor = FindBloodyKrusAnchor();

        if (anchor == null)
        {
            Debug.LogWarning("Level1GameFlow: Cross Arch not found — place krusAnchor manually.");
            return;
        }

        krusMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        krusMarker.name = "KrusObjectiveMarker";
        krusMarker.transform.SetParent(anchor, false);
        krusMarker.transform.localPosition = Vector3.zero;
        krusMarker.transform.localRotation = Quaternion.identity;

        Vector3 lossy = anchor.lossyScale;
        float sx = Mathf.Abs(lossy.x) < 0.01f ? 1f : lossy.x;
        float sy = Mathf.Abs(lossy.y) < 0.01f ? 1f : lossy.y;
        float sz = Mathf.Abs(lossy.z) < 0.01f ? 1f : lossy.z;
        krusMarker.transform.localScale = new Vector3(
            krusTriggerSize.x / Mathf.Abs(sx),
            krusTriggerSize.y / Mathf.Abs(sy),
            krusTriggerSize.z / Mathf.Abs(sz));

        Collider col = krusMarker.GetComponent<Collider>();
        col.isTrigger = true;

        MeshRenderer mr = krusMarker.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            Material mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", markerColor);
            else
                mat.color = markerColor;
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        KrusObjectiveTrigger trigger = krusMarker.AddComponent<KrusObjectiveTrigger>();
        trigger.flow = this;
    }

    Transform FindBloodyKrusAnchor()
    {
        GameObject church = GameObject.Find("Church 2 Open");
        Vector3 churchPos = church != null ? church.transform.position : Vector3.zero;

        Transform best = null;
        float bestDist = float.MaxValue;

        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t == null || !t.name.StartsWith("Cross Arch"))
                continue;

            float dist = church != null
                ? (t.position - churchPos).sqrMagnitude
                : t.position.sqrMagnitude;

            if (dist < bestDist)
            {
                bestDist = dist;
                best = t;
            }
        }

        return best;
    }

    void BuildEndPanelIfNeeded()
    {
        if (hudCanvas == null)
            return;

        TMP_FontAsset font = objectiveText != null && objectiveText.font != null
            ? objectiveText.font
            : TMP_Settings.defaultFontAsset;

        endPanel = new GameObject("Level1EndPanel", typeof(RectTransform), typeof(Image));
        endPanel.transform.SetParent(hudCanvas.transform, false);
        RectTransform panelRt = endPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
        endDimmer = endPanel.GetComponent<Image>();
        endDimmer.color = new Color(0f, 0f, 0f, 0.75f);
        endDimmer.raycastTarget = true;

        GameObject titleGo = new GameObject("EndTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(endPanel.transform, false);
        RectTransform titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.anchoredPosition = new Vector2(0f, 90f);
        titleRt.sizeDelta = new Vector2(600f, 50f);
        endTitleText = titleGo.GetComponent<TextMeshProUGUI>();
        endTitleText.font = font;
        endTitleText.fontSize = 32;
        endTitleText.alignment = TextAlignmentOptions.Center;
        endTitleText.color = Color.white;
        endTitleText.fontStyle = FontStyles.Bold;

        GameObject bodyGo = new GameObject("EndBody", typeof(RectTransform), typeof(TextMeshProUGUI));
        bodyGo.transform.SetParent(endPanel.transform, false);
        RectTransform bodyRt = bodyGo.GetComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0.5f, 0.5f);
        bodyRt.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRt.pivot = new Vector2(0.5f, 0.5f);
        bodyRt.anchoredPosition = new Vector2(0f, -20f);
        bodyRt.sizeDelta = new Vector2(520f, 220f);
        endBodyText = bodyGo.GetComponent<TextMeshProUGUI>();
        endBodyText.font = font;
        endBodyText.fontSize = 20;
        endBodyText.alignment = TextAlignmentOptions.Center;
        endBodyText.color = Color.white;
        endBodyText.lineSpacing = 8f;

        endPanel.SetActive(false);
        endPanel.transform.SetAsLastSibling();
    }
}
