using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class Backstory1CutsceneController : MonoBehaviour
{
    [Header("Cast")]
    [SerializeField] Transform sherall;
    [SerializeField] Transform groom;
    [SerializeField] Transform priest;
    [SerializeField] Transform elder;
    [SerializeField] Transform aswangGuest;

    [Header("Control")]
    [SerializeField] Transform mainCamera;
    [SerializeField] Behaviour cinemachineBrain;
    [SerializeField] Behaviour[] playerControls;

    [Header("Blocking")]
    [SerializeField] Vector3 groomDisturbedPosition = new Vector3(304.8f, 2.53f, 42f);
    [SerializeField] Vector3 elderDestination = new Vector3(300.2f, 2.53f, 44.5f);
    [SerializeField] Vector3 aswangDestination = new Vector3(309f, 2.53f, 48f);
    [SerializeField, Min(0.1f)] float elderWalkDuration = 3.5f;
    [SerializeField, Min(0.1f)] float aswangWalkDuration = 3f;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int HitHash = Animator.StringToHash("Hit");

    Animator sherallAnimator;
    Animator groomAnimator;
    Animator priestAnimator;
    Animator elderAnimator;
    Animator aswangAnimator;
    Camera sceneCamera;
    float gameplayFieldOfView;
    Texture2D panelTexture;
    Texture2D accentTexture;
    GUIStyle speakerStyle;
    GUIStyle dialogueStyle;
    GUIStyle promptStyle;
    string currentSpeaker = "";
    string currentDialogue = "";
    float dialogueAlpha;
    bool sequenceComplete;

    void Awake()
    {
        SetPlayerControl(false);
        if (cinemachineBrain != null)
            cinemachineBrain.enabled = false;

        sherallAnimator = FindAnimator(sherall);
        groomAnimator = FindAnimator(groom);
        priestAnimator = FindAnimator(priest);
        elderAnimator = FindAnimator(elder);
        aswangAnimator = FindAnimator(aswangGuest);
        if (mainCamera != null)
        {
            sceneCamera = mainCamera.GetComponent<Camera>();
            if (sceneCamera != null)
                gameplayFieldOfView = sceneCamera.fieldOfView;
        }

        SetSpeed(sherallAnimator, 0f);
        SetSpeed(groomAnimator, 0f);
        SetSpeed(priestAnimator, 0f);
        SetSpeed(elderAnimator, 0f);
        SetSpeed(aswangAnimator, 0f);

        panelTexture = MakeTexture(new Color(0.015f, 0.012f, 0.018f, 0.92f));
        accentTexture = MakeTexture(new Color(0.78f, 0.58f, 0.24f, 1f));
    }

    IEnumerator Start()
    {
        FaceEachOther(sherall, groom);
        SetShot(
            Midpoint(sherall, groom, 0f) + new Vector3(3f, 4f, 9f),
            Midpoint(sherall, groom, 1.4f),
            52f);

        yield return ShowLine(
            "NARRATION",
            "Before the altar, Sherall and her groom stood one vow away from becoming husband and wife.",
            4.5f);
        yield return MoveCamera(
            priest.position + new Vector3(3.2f, 2f, 5.5f),
            priest.position + Vector3.up * 1.4f,
            1.2f,
            42f);
        yield return ShowLine(
            "OFFICIANT",
            "Sherall, do you take him as your husband—in joy, in hardship, and for all your days?",
            4.8f);
        yield return MoveCamera(
            Midpoint(sherall, groom, 0f) + new Vector3(-2.5f, 2.2f, 4.5f),
            Midpoint(sherall, groom, 1.45f),
            1.4f,
            45f);
        yield return ShowLine("SHERALL", "I do. Buong puso at buong buhay.", 3.2f);

        yield return MoveCamera(
            priest.position + new Vector3(-3f, 1.8f, 4.6f),
            priest.position + Vector3.up * 1.4f,
            1.1f,
            40f);
        yield return ShowLine(
            "OFFICIANT",
            "And do you take Sherall as your wife?",
            3.3f);
        yield return ShowLine("GROOM", "I... do.", 2.8f);

        Coroutine groomActing = StartCoroutine(ActStrangely());
        yield return MoveCamera(
            groom.position + new Vector3(2.8f, 1.4f, 3.8f),
            groom.position + Vector3.up * 1.45f,
            1.1f,
            38f);
        yield return ShowLine(
            "NARRATION",
            "His hand tightened around hers. His breathing changed, and his eyes followed a sound no one else could hear.",
            5.2f);
        yield return ShowLine("GROOM", "The bells... make them stop. They can hear us.", 3.8f);
        yield return groomActing;

        TriggerReaction(sherallAnimator);
        FaceEachOther(sherall, groom);
        yield return ShowLine("SHERALL", "What is happening to you?", 3f);
        yield return ShowLine("GROOM", "Sherall... get away from me.", 3.2f);

        yield return MoveCamera(
            elderDestination + new Vector3(4f, 3.3f, 12.5f),
            elderDestination + Vector3.up * 1.4f,
            1.5f,
            50f);
        Coroutine elderWalk = StartCoroutine(
            MoveCharacter(elder, elderDestination, elderWalkDuration, elderAnimator, 0.5f));
        yield return ShowLine(
            "NARRATION",
            "The church doors opened. An elder hurried down the aisle as the guests began to turn.",
            4.4f);
        yield return elderWalk;
        yield return ShowLine("ELDER", "Sherall! Huwag mong tapusin ang seremonya!", 3.2f);

        yield return MoveCamera(
            Midpoint(sherall, elder, 0f) + new Vector3(-3f, 2.2f, 4.5f),
            Midpoint(sherall, elder, 1.45f),
            1.3f,
            44f);
        FaceEachOther(elder, sherall);
        yield return ShowLine("SHERALL", "Lolo, please—what is happening to him?", 3.4f);
        yield return ShowLine(
            "ELDER",
            "Hindi na sila ang mga bisita ninyo. Aswang hunt by sound. Keep your voice low.",
            5.2f);
        yield return ShowLine(
            "ELDER",
            "Recite the orasyon. Find asin, bawang, and holy water. Only the lunas can hold them.",
            5f);
        yield return ShowLine(
            "ELDER",
            "Bring the three wards back to this altar. Finish the rite without calling them to you.",
            5.2f);

        yield return ShowLine("SHERALL", "Can the lunas still save my husband?", 3.4f);
        yield return ShowLine(
            "ELDER",
            "If he still knows your name, there may be time. But you must go now.",
            4.5f);

        yield return MoveCamera(
            aswangDestination + new Vector3(-5.5f, 2f, -7f),
            aswangDestination + Vector3.up * 1.4f,
            1f,
            46f);
        Coroutine aswangWalk = StartCoroutine(
            MoveCharacter(
                aswangGuest,
                aswangDestination,
                aswangWalkDuration,
                aswangAnimator,
                1f));
        yield return ShowLine(
            "NARRATION",
            "Behind them, one of the wedding guests answered the groom's whisper with an inhuman step.",
            4.8f);
        yield return aswangWalk;

        yield return ShowLine("ELDER", "Go. Tapusin mo ang lunas. I will keep it away.", 3.8f);
        yield return ShowLine(
            "OBJECTIVE",
            "Find holy water, asin, and bawang. Return to the altar.",
            4.5f);

        yield return MoveCamera(
            sherall.position + new Vector3(-2.5f, 1.8f, 4f),
            sherall.position + Vector3.up * 1.4f,
            1.2f,
            45f);
        FinishSequence();
    }

    IEnumerator ShowLine(string speaker, string dialogue, float duration)
    {
        currentSpeaker = speaker;
        currentDialogue = dialogue;
        dialogueAlpha = 0f;

        float started = Time.unscaledTime;
        while (Time.unscaledTime - started < duration)
        {
            float elapsed = Time.unscaledTime - started;
            float remaining = duration - elapsed;
            dialogueAlpha = Mathf.Min(
                Mathf.Clamp01(elapsed / 0.25f),
                Mathf.Clamp01(remaining / 0.35f));

            if (SkipSequencePressed())
            {
                FinishSequence();
                yield break;
            }

            if (elapsed > 0.35f && AdvancePressed())
            {
                yield return FadeDialogueOut(0.18f);
                break;
            }

            yield return null;
        }

        dialogueAlpha = 0f;
        currentSpeaker = "";
        currentDialogue = "";
    }

    IEnumerator FadeDialogueOut(float duration)
    {
        float startAlpha = dialogueAlpha;
        float elapsed = 0f;
        while (elapsed < duration && !sequenceComplete)
        {
            elapsed += Time.unscaledDeltaTime;
            dialogueAlpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
    }

    IEnumerator ActStrangely()
    {
        if (groom == null)
            yield break;

        TriggerReaction(groomAnimator);
        yield return WaitUnscaled(0.65f);
        yield return MoveCharacter(
            groom,
            groomDisturbedPosition,
            1.8f,
            groomAnimator,
            0.25f);

        Vector3 direction = aswangDestination - groom.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            yield return RotateCharacter(groom, Quaternion.LookRotation(direction), 1.1f);
    }

    IEnumerator RotateCharacter(Transform character, Quaternion destination, float duration)
    {
        if (character == null)
            yield break;

        Quaternion start = character.rotation;
        float elapsed = 0f;
        while (elapsed < duration && !sequenceComplete)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            character.rotation = Quaternion.Slerp(start, destination, t);
            yield return null;
        }
    }

    IEnumerator WaitUnscaled(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && !sequenceComplete)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
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

    IEnumerator MoveCamera(
        Vector3 destination,
        Vector3 lookTarget,
        float duration,
        float fieldOfView)
    {
        if (mainCamera == null)
            yield break;

        Vector3 startPosition = mainCamera.position;
        Quaternion startRotation = mainCamera.rotation;
        Quaternion endRotation = Quaternion.LookRotation(lookTarget - destination);
        float startFieldOfView = sceneCamera != null ? sceneCamera.fieldOfView : fieldOfView;
        float elapsed = 0f;

        while (elapsed < duration && !sequenceComplete)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            mainCamera.position = Vector3.Lerp(startPosition, destination, t);
            mainCamera.rotation = Quaternion.Slerp(startRotation, endRotation, t);
            if (sceneCamera != null)
                sceneCamera.fieldOfView = Mathf.Lerp(startFieldOfView, fieldOfView, t);
            yield return null;
        }
    }

    void SetShot(Vector3 position, Vector3 lookTarget, float fieldOfView)
    {
        if (mainCamera == null)
            return;

        mainCamera.position = position;
        mainCamera.rotation = Quaternion.LookRotation(lookTarget - position);
        if (sceneCamera != null)
            sceneCamera.fieldOfView = fieldOfView;
    }

    void FinishSequence()
    {
        if (sequenceComplete)
            return;

        sequenceComplete = true;
        StopAllCoroutines();
        currentSpeaker = "";
        currentDialogue = "";
        dialogueAlpha = 0f;
        SetSpeed(groomAnimator, 0f);
        SetSpeed(priestAnimator, 0f);
        SetSpeed(elderAnimator, 0f);
        SetSpeed(aswangAnimator, 0f);

        if (sceneCamera != null)
            sceneCamera.fieldOfView = gameplayFieldOfView;
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

        Color previousColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, dialogueAlpha);
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
        GUI.color = previousColor;
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
