using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class Backstory1CutsceneController : MonoBehaviour
{
    [Header("Cast")]
    [SerializeField] Transform sherall;
    [SerializeField] Transform elder;
    [SerializeField] Transform infectedHusband;

    [Header("Control")]
    [SerializeField] Transform mainCamera;
    [SerializeField] Behaviour cinemachineBrain;
    [SerializeField] Behaviour[] playerControls;

    [Header("Blocking")]
    [SerializeField] Vector3 elderDestination = new Vector3(301.8f, 2.53f, 64f);
    [SerializeField] Vector3 infectedDestination = new Vector3(308.3f, 2.53f, 53.5f);
    [SerializeField, Min(0.1f)] float elderWalkDuration = 3.5f;
    [SerializeField, Min(0.1f)] float infectedWalkDuration = 3f;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int HitHash = Animator.StringToHash("Hit");

    Animator sherallAnimator;
    Animator elderAnimator;
    Animator infectedAnimator;
    Texture2D panelTexture;
    Texture2D accentTexture;
    GUIStyle speakerStyle;
    GUIStyle dialogueStyle;
    GUIStyle promptStyle;
    string currentSpeaker = "";
    string currentDialogue = "";
    bool sequenceComplete;

    void Awake()
    {
        SetPlayerControl(false);
        if (cinemachineBrain != null)
            cinemachineBrain.enabled = false;

        sherallAnimator = FindAnimator(sherall);
        elderAnimator = FindAnimator(elder);
        infectedAnimator = FindAnimator(infectedHusband);

        SetSpeed(sherallAnimator, 0f);
        SetSpeed(elderAnimator, 0f);
        SetSpeed(infectedAnimator, 0f);

        panelTexture = MakeTexture(new Color(0.015f, 0.012f, 0.018f, 0.92f));
        accentTexture = MakeTexture(new Color(0.78f, 0.58f, 0.24f, 1f));
    }

    IEnumerator Start()
    {
        SetShot(new Vector3(312f, 8.2f, 75f), new Vector3(304f, 3.4f, 54f));

        yield return ShowLine(
            "NARRATION",
            "The wedding bells rang, but the vows were never finished.",
            3.2f);

        Coroutine elderWalk = StartCoroutine(
            MoveCharacter(elder, elderDestination, elderWalkDuration, elderAnimator, 0.5f));
        yield return ShowLine("ELDER", "Sherall! Huwag mong tapusin ang seremonya!", 3.1f);
        yield return elderWalk;

        yield return MoveCamera(
            new Vector3(307.5f, 4.7f, 69.5f),
            Midpoint(sherall, elder, 1.5f),
            1.2f);

        TriggerReaction(sherallAnimator);
        yield return ShowLine("SHERALL", "Lolo? What happened to the guests?", 3f);

        FaceEachOther(elder, sherall);
        yield return ShowLine(
            "ELDER",
            "Hindi na sila ang mga bisita ninyo. Aswang hunt by sound. Keep your voice low.",
            5.2f);
        yield return ShowLine(
            "ELDER",
            "Only the orasyon, asin, bawang, and holy water can hold them.",
            4.6f);
        yield return ShowLine(
            "ELDER",
            "Gather the three wards. Complete the lunas at the altar—and do not call them to you.",
            5.4f);

        yield return ShowLine("SHERALL", "And my husband?", 2.6f);
        yield return ShowLine(
            "ELDER",
            "He is among them. Finish the rite before he is lost.",
            4.2f);

        yield return MoveCamera(
            new Vector3(302f, 4.1f, 60f),
            infectedDestination + Vector3.up * 1.4f,
            0.8f);
        Coroutine infectedWalk = StartCoroutine(
            MoveCharacter(
                infectedHusband,
                infectedDestination,
                infectedWalkDuration,
                infectedAnimator,
                1f));
        yield return ShowLine(
            "NARRATION",
            "A familiar shape moved beyond the altar. It still wore her husband's face.",
            4.7f);
        yield return infectedWalk;

        yield return ShowLine("ELDER", "Go. Tapusin mo ang lunas. I will draw it away.", 3.8f);
        yield return ShowLine(
            "OBJECTIVE",
            "Find holy water, asin, and bawang. Return to the altar.",
            4.5f);

        FinishSequence();
    }

    IEnumerator ShowLine(string speaker, string dialogue, float duration)
    {
        currentSpeaker = speaker;
        currentDialogue = dialogue;

        float started = Time.unscaledTime;
        while (Time.unscaledTime - started < duration)
        {
            if (SkipSequencePressed())
            {
                FinishSequence();
                yield break;
            }

            if (Time.unscaledTime - started > 0.35f && AdvancePressed())
                break;

            yield return null;
        }

        currentSpeaker = "";
        currentDialogue = "";
    }

    IEnumerator MoveCharacter(
        Transform character,
        Vector3 destination,
        float duration,
        Animator animator,
        float speed)
    {
        if (character == null)
            yield break;

        Vector3 start = character.position;
        Vector3 direction = destination - start;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            character.rotation = Quaternion.LookRotation(direction);

        SetSpeed(animator, speed);
        float elapsed = 0f;
        while (elapsed < duration && !sequenceComplete)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            character.position = Vector3.Lerp(start, destination, t);
            yield return null;
        }

        character.position = destination;
        SetSpeed(animator, 0f);
    }

    IEnumerator MoveCamera(Vector3 destination, Vector3 lookTarget, float duration)
    {
        if (mainCamera == null)
            yield break;

        Vector3 startPosition = mainCamera.position;
        Quaternion startRotation = mainCamera.rotation;
        Quaternion endRotation = Quaternion.LookRotation(lookTarget - destination);
        float elapsed = 0f;

        while (elapsed < duration && !sequenceComplete)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            mainCamera.position = Vector3.Lerp(startPosition, destination, t);
            mainCamera.rotation = Quaternion.Slerp(startRotation, endRotation, t);
            yield return null;
        }
    }

    void SetShot(Vector3 position, Vector3 lookTarget)
    {
        if (mainCamera == null)
            return;

        mainCamera.position = position;
        mainCamera.rotation = Quaternion.LookRotation(lookTarget - position);
    }

    void FinishSequence()
    {
        if (sequenceComplete)
            return;

        sequenceComplete = true;
        StopAllCoroutines();
        currentSpeaker = "";
        currentDialogue = "";
        SetSpeed(elderAnimator, 0f);
        SetSpeed(infectedAnimator, 0f);

        if (cinemachineBrain != null)
            cinemachineBrain.enabled = true;
        SetPlayerControl(true);
    }

    void SetPlayerControl(bool enabled)
    {
        if (playerControls == null)
            return;

        foreach (Behaviour control in playerControls)
        {
            if (control != null)
                control.enabled = enabled;
        }
    }

    static Animator FindAnimator(Transform character)
    {
        return character != null ? character.GetComponentInChildren<Animator>() : null;
    }

    static void SetSpeed(Animator animator, float speed)
    {
        if (animator != null)
            animator.SetFloat(SpeedHash, speed);
    }

    static void TriggerReaction(Animator animator)
    {
        if (animator != null)
            animator.SetTrigger(HitHash);
    }

    static void FaceEachOther(Transform first, Transform second)
    {
        if (first == null || second == null)
            return;

        Vector3 firstDirection = second.position - first.position;
        firstDirection.y = 0f;
        if (firstDirection.sqrMagnitude > 0.001f)
            first.rotation = Quaternion.LookRotation(firstDirection);

        Vector3 secondDirection = first.position - second.position;
        secondDirection.y = 0f;
        if (secondDirection.sqrMagnitude > 0.001f)
            second.rotation = Quaternion.LookRotation(secondDirection);
    }

    static Vector3 Midpoint(Transform first, Transform second, float height)
    {
        if (first == null || second == null)
            return Vector3.up * height;
        return (first.position + second.position) * 0.5f + Vector3.up * height;
    }

    static bool AdvancePressed()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null &&
            (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame);
    }

    static bool SkipSequencePressed()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
    }

    void OnGUI()
    {
        if (sequenceComplete || string.IsNullOrEmpty(currentDialogue))
            return;

        EnsureStyles();
        float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1920f, Screen.height / 1080f), 0.7f, 1.4f);
        float panelWidth = Mathf.Min(Screen.width * 0.78f, 1380f * scale);
        float panelHeight = 190f * scale;
        float panelX = (Screen.width - panelWidth) * 0.5f;
        float panelY = Screen.height - panelHeight - 42f * scale;

        GUI.DrawTexture(new Rect(panelX, panelY, panelWidth, panelHeight), panelTexture);
        GUI.DrawTexture(new Rect(panelX, panelY, 6f * scale, panelHeight), accentTexture);

        speakerStyle.fontSize = Mathf.RoundToInt(22f * scale);
        dialogueStyle.fontSize = Mathf.RoundToInt(28f * scale);
        promptStyle.fontSize = Mathf.RoundToInt(16f * scale);

        float left = panelX + 38f * scale;
        GUI.Label(
            new Rect(left, panelY + 22f * scale, panelWidth - 70f * scale, 32f * scale),
            currentSpeaker,
            speakerStyle);
        GUI.Label(
            new Rect(left, panelY + 58f * scale, panelWidth - 76f * scale, 92f * scale),
            currentDialogue,
            dialogueStyle);
        GUI.Label(
            new Rect(left, panelY + 151f * scale, panelWidth - 76f * scale, 24f * scale),
            "SPACE / ENTER  Continue     ESC  Skip",
            promptStyle);
    }

    void EnsureStyles()
    {
        if (speakerStyle != null)
            return;

        speakerStyle = CreateStyle(FontStyle.Bold, new Color(0.9f, 0.7f, 0.36f));
        dialogueStyle = CreateStyle(FontStyle.Normal, new Color(0.96f, 0.94f, 0.9f));
        dialogueStyle.wordWrap = true;
        dialogueStyle.alignment = TextAnchor.UpperLeft;
        promptStyle = CreateStyle(FontStyle.Normal, new Color(0.62f, 0.58f, 0.54f));
    }

    static GUIStyle CreateStyle(FontStyle fontStyle, Color color)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontStyle = fontStyle
        };
        style.normal.textColor = color;
        return style;
    }

    static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    void OnDisable()
    {
        if (!sequenceComplete)
            FinishSequence();
    }

    void OnDestroy()
    {
        if (panelTexture != null)
            Destroy(panelTexture);
        if (accentTexture != null)
            Destroy(accentTexture);
    }
}
