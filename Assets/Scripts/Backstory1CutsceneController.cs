using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class Backstory1CutsceneController : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] string gameplayScene = "Level1";
    [SerializeField, Min(0.1f)] float transitionDuration = 0.8f;

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

    [Header("Bridal Entrance")]
    [SerializeField] bool allowBrideControlBeforeCeremony = true;
    [SerializeField] Vector3 brideEntryPosition = new Vector3(303.46f, 2.53f, 58f);
    [SerializeField] Vector3 brideCeremonyPosition = new Vector3(296.72f, 3.025f, 39.454f);
    [SerializeField] Vector3 brideCeremonyEuler = new Vector3(0f, 124.775f, 0f);
    [SerializeField, Min(0.1f)] float ceremonyFadeDuration = 0.45f;
    [SerializeField, Min(0.5f)] float ceremonyTriggerDistance = 2.2f;

    [Header("Wedding Candlelight")]
    [SerializeField] bool createWarmCandleGlow = true;
    [SerializeField] Color candleGlowColor = new Color(1f, 0.58f, 0.24f, 1f);
    [SerializeField, Min(0f)] float candleGlowIntensity = 1.6f;
    [SerializeField, Min(0.1f)] float candleGlowRange = 5f;

    [Header("Lively Wedding Lighting")]
    [SerializeField] bool createCeilingWeddingLights = true;
    [SerializeField] Color ceilingLightColor = new Color(1f, 0.88f, 0.68f, 1f);
    [SerializeField, Min(0f)] float ceilingLightIntensity = 4.5f;
    [SerializeField, Min(0.1f)] float ceilingLightRange = 12f;

    [Header("Blocking")]
    [SerializeField] Vector3 groomDisturbedPosition = new Vector3(298.5f, 3.07f, 37.8f);
    [SerializeField] Vector3 elderStartPosition = new Vector3(303.46f, 2.53f, 58.2f);
    [SerializeField] Vector3 elderRearAisleWaypoint = new Vector3(303.46f, 2.53f, 52f);
    [SerializeField] Vector3 elderFrontAisleWaypoint = new Vector3(303.46f, 2.53f, 45.5f);
    [SerializeField] Vector3 elderDestination = new Vector3(300.2f, 2.53f, 41.1f);
    [SerializeField] Vector3 elderWatchCameraPosition = new Vector3(298.6f, 4.15f, 37.4f);
    [SerializeField] Vector3 aswangDestination = new Vector3(309f, 2.53f, 48f);
    [SerializeField, Min(0.1f)] float elderWalkDuration = 7.2f;
    [SerializeField, Min(0.1f)] float aswangWalkDuration = 3f;

    [Header("Church Ground")]
    [SerializeField] float churchFloorY = 2.53f;
    [SerializeField] float altarFloorY = 3.03f;
    [SerializeField] float sittingRootY = 2.94f;
    [SerializeField] float sittingHipY = 3.16f;
    [SerializeField] float sittingSeatClearance = 0.14f;
    [SerializeField] float standingHipHeight = 0.9f;
    [SerializeField] float altarFrontZ = 41.4f;
    [SerializeField] float pewSeatOffset = 0.04f;
    [SerializeField] float pewBackOffset = 0.05f;

    [Header("Infection Reactions")]
    [SerializeField, Min(0.1f)] float groomHitHoldDuration = 2.35f;
    [SerializeField, Min(0.1f)] float brideShockStepDuration = 1.35f;
    [SerializeField, Min(0.2f)] float brideShockStepDistance = 0.75f;

    [Header("Guests")]
    [SerializeField] Transform[] sittingGuests;
    [SerializeField] Transform[] guestSources;
    [SerializeField] Transform[] infectedRearGuests;
    [SerializeField] RuntimeAnimatorController guestSitController;
    [SerializeField] bool spawnSittingGuests = true;
    [SerializeField] bool spawnInfectedRearGuests = true;
    [SerializeField] Vector3[] infectedRearStartPositions =
    {
        new Vector3(300.4f, 2.53f, 57.2f),
        new Vector3(306.3f, 2.53f, 56.8f)
    };
    [SerializeField] Vector3[] infectedRearDestinations =
    {
        new Vector3(301f, 2.53f, 49.6f),
        new Vector3(306.9f, 2.53f, 50.1f)
    };

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int HitHash = Animator.StringToHash("Hit");
    static readonly int KickHash = Animator.StringToHash("Kick");
    const string KickState = "kick-to-the-groin";
    const string GettingHitState = "getting-hit";
    const string WalkBackState = "walking-backwards";
    static readonly string[] SittingStates =
    {
        "female-sitting-pose",
        "sitting-and-pointing",
        "sitting-rubbing-arm"
    };

    Animator sherallAnimator;
    Animator groomAnimator;
    Animator priestAnimator;
    Animator elderAnimator;
    Animator aswangAnimator;
    Animator[] infectedRearAnimators;
    Vector3[] seatedGuestPositions;
    float[] seatedGuestYaws;
    float[] seatedGuestHipYs;
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
    float screenFade;
    bool isTransitioning;
    bool sequenceComplete;
    bool awaitingCeremonyStart;

    void Awake()
    {
        awaitingCeremonyStart = allowBrideControlBeforeCeremony;
        SetPlayerControl(awaitingCeremonyStart);
        if (cinemachineBrain != null)
            cinemachineBrain.enabled = awaitingCeremonyStart;

        if (awaitingCeremonyStart && sherall != null)
        {
            sherall.position = SnapToChurchFloor(brideEntryPosition);
            sherall.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        sherallAnimator = FindAnimator(sherall);
        groomAnimator = FindAnimator(groom);
        priestAnimator = FindAnimator(priest);
        elderAnimator = FindAnimator(elder);
        aswangAnimator = FindAnimator(aswangGuest);
        EnsureEnvironmentColliders();
        EnsurePewSeatColliders();
        EnsureChurchFloor();
        EnsureCharacterColliders();
        OpenExteriorChurchOpenings();

        if (elder != null)
            elder.gameObject.SetActive(false);

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

        if (createWarmCandleGlow)
            CreateCandleGlow();
        if (createCeilingWeddingLights)
            CreateCeilingLights();

        SpawnSittingGuests();
        SpawnInfectedRearGuests();
        OpenAisleForBride();
        LockAllAnimatorsToGround();
    }

    IEnumerator Start()
    {
        if (awaitingCeremonyStart)
            yield return WaitForBrideToBeginCeremony();
        else
            TakeCinematicControl();

        FaceEachOther(sherall, groom);
        SetShot(
            Midpoint(sherall, groom, 0f) + new Vector3(0f, 2.4f, 4.8f),
            Midpoint(sherall, groom, 1.35f),
            42f);

        yield return ShowLine(
            "NARRATION",
            "Before the altar, Sherall and her groom stood one vow away from becoming husband and wife.",
            4.5f);
        yield return ShowCharacterLine(
            "OFFICIANT",
            "Sherall, do you take him as your husband—in joy, in hardship, and for all your days?",
            4.8f,
            priest,
            1f);
        yield return ShowCharacterLine(
            "SHERALL",
            "I do. Buong puso at buong buhay.",
            3.2f,
            sherall,
            -1f);

        yield return ShowCharacterLine(
            "OFFICIANT",
            "And do you take Sherall as your wife?",
            3.3f,
            priest,
            -1f);
        yield return ShowCharacterLine("GROOM", "I... do.", 2.8f, groom, 1f);

        Coroutine groomActing = StartCoroutine(ActStrangely());
        yield return MoveCamera(
            groom.position + new Vector3(2.4f, 1.65f, 3f),
            groom.position + Vector3.up * 1.45f,
            0.8f,
            36f);
        yield return ShowLine(
            "NARRATION",
            "His hand tightened around hers. His breathing changed, and his eyes followed a sound no one else could hear.",
            5.2f);
        yield return ShowLine("GROOM", "The bells... make them stop. They can hear us.", 3.8f);
        yield return groomActing;

        yield return StepBackInShock();
        yield return ShowCharacterLine(
            "SHERALL",
            "What is happening to you?",
            3f,
            sherall,
            -1f);
        yield return ShowCharacterLine(
            "GROOM",
            "Sherall... get away from me.",
            3.2f,
            groom,
            1f);

        PlaceElderAtAisleStart();
        if (elder != null)
            elder.gameObject.SetActive(true);

        if (elderAnimator != null)
        {
            elderAnimator.Rebind();
            elderAnimator.Update(0f);
            SetSpeed(elderAnimator, 0f);
        }

        RevealInfectedRearGuests();
        StartCoroutine(WalkInfectedRearGuests());

        yield return MoveCamera(
            elderWatchCameraPosition,
            ElderApproachLookTarget(),
            0.85f,
            48f);
        Coroutine elderWalk = StartCoroutine(
            MoveCharacterAlongPath(
                elder,
                new[] { elderRearAisleWaypoint, elderFrontAisleWaypoint, elderDestination },
                elderWalkDuration,
                elderAnimator,
                0.85f,
                true));
        Coroutine elderCamera = StartCoroutine(
            WatchApproachFromAltar(elder, elderWalkDuration, 48f));
        yield return ShowLine(
            "NARRATION",
            "The church doors opened. An elder hurried down the aisle as the guests began to turn.",
            4.4f);
        yield return elderWalk;
        yield return elderCamera;
        if (elder != null && sherall != null)
            FaceEachOther(elder, sherall);
        yield return ShowCharacterLine(
            "ELDER",
            "Sherall! Huwag mong tapusin ang seremonya!",
            3.2f,
            elder,
            1f);

        FaceEachOther(elder, sherall);
        yield return ShowCharacterLine(
            "SHERALL",
            "Lolo, please—what is happening to him?",
            3.4f,
            sherall,
            -1f);
        yield return ShowCharacterLine(
            "ELDER",
            "Hindi na sila ang mga bisita ninyo. The aswang hunt by sound—keep your voice low.",
            5.2f,
            elder,
            1f);
        yield return ShowCharacterLine(
            "ELDER",
            "Find bawang and a candle. Their smoke and flame can destroy an aswang.",
            5f,
            elder,
            -1f);
        yield return ShowCharacterLine(
            "ELDER",
            "Use both against every creature on these grounds. Do not leave a single aswang alive.",
            5.2f,
            elder,
            1f);

        yield return ShowCharacterLine(
            "SHERALL",
            "Then I will find them and kill every aswang before they hurt anyone else.",
            3.4f,
            sherall,
            -1f);
        yield return ShowCharacterLine(
            "ELDER",
            "Move quietly, gather the bawang and candle, and strike before they surround you.",
            4.5f,
            elder,
            1f);

        yield return MoveCamera(
            aswangGuest.position + new Vector3(-4f, 2.1f, 4.5f),
            aswangGuest.position + Vector3.up * 1.4f,
            0.8f,
            42f);
        Coroutine aswangWalk = StartCoroutine(
            MoveCharacter(
                aswangGuest,
                aswangDestination,
                aswangWalkDuration,
                aswangAnimator,
                1f));
        Coroutine aswangCamera = StartCoroutine(
            FollowCharacterCamera(
                aswangGuest,
                new Vector3(-4f, 2.1f, 4.5f),
                aswangWalkDuration,
                42f));
        yield return ShowLine(
            "NARRATION",
            "Behind them, one of the wedding guests answered the groom's whisper with an inhuman step.",
            4.8f);
        yield return aswangWalk;
        yield return aswangCamera;

        yield return ShowCharacterLine(
            "ELDER",
            "Go. Find the bawang and candle. Kill every last aswang.",
            3.8f,
            elder,
            -1f);
        yield return MoveCamera(
            Midpoint(sherall, elder, 0f) + new Vector3(0f, 2.5f, 5f),
            Midpoint(sherall, elder, 1.35f),
            0.8f,
            42f);
        yield return ShowLine(
            "OBJECTIVE",
            "Collect bawang and a candle. Kill all aswangs.",
            4.5f);

        yield return MoveCamera(
            sherall.position + new Vector3(-2.5f, 1.8f, 4f),
            sherall.position + Vector3.up * 1.4f,
            1.2f,
            45f);
        BeginGameplayTransition();
    }

    IEnumerator WaitForBrideToBeginCeremony()
    {
        while (!BrideReachedGroom())
            yield return null;

        awaitingCeremonyStart = false;
        TakeCinematicControl();
        yield return FadeScreen(1f, ceremonyFadeDuration);

        if (sherall != null)
        {
            CharacterController controller = sherall.GetComponent<CharacterController>();
            bool controllerWasEnabled = controller != null && controller.enabled;
            if (controllerWasEnabled)
                controller.enabled = false;

            sherall.position = SnapToChurchFloor(brideCeremonyPosition);
            sherall.rotation = Quaternion.Euler(brideCeremonyEuler);

            if (controllerWasEnabled)
                controller.enabled = true;
        }

        FaceEachOther(sherall, groom);
        SetShot(
            Midpoint(sherall, groom, 0f) + new Vector3(0f, 2.4f, 4.8f),
            Midpoint(sherall, groom, 1.35f),
            42f);
        yield return WaitUnscaled(0.15f);
        yield return FadeScreen(0f, ceremonyFadeDuration);
    }

    bool BrideReachedGroom()
    {
        if (sherall == null)
            return false;

        Vector3 bride = sherall.position;
        if (groom != null && HorizontalDistanceSq(bride, groom.position) <=
            ceremonyTriggerDistance * ceremonyTriggerDistance)
            return true;

        if (HorizontalDistanceSq(bride, brideCeremonyPosition) <=
            ceremonyTriggerDistance * ceremonyTriggerDistance)
            return true;

        bool inAisle = Mathf.Abs(bride.x - brideEntryPosition.x) <= 3.6f;
        bool atFront = bride.z <= altarFrontZ + 2.4f && bride.z >= altarFrontZ - 5.5f;
        return inAisle && atFront;
    }

    void TakeCinematicControl()
    {
        SetPlayerControl(false);
        SetSpeed(sherallAnimator, 0f);
        if (cinemachineBrain != null)
            cinemachineBrain.enabled = false;
    }

    IEnumerator FadeScreen(float target, float duration)
    {
        float start = screenFade;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            screenFade = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        screenFade = target;
    }

    IEnumerator ShowCharacterLine(
        string speaker,
        string dialogue,
        float duration,
        Transform subject,
        float side)
    {
        if (subject != null)
        {
            yield return MoveCamera(
                subject.position + new Vector3(2.35f * side, 1.7f, 3f),
                subject.position + Vector3.up * 1.4f,
                0.65f,
                36f);
        }

        yield return ShowLine(speaker, dialogue, duration);
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
                BeginGameplayTransition();
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

        FaceEachOther(groom, sherall);
        PlayState(groomAnimator, KickState);
        if (groomAnimator != null)
            groomAnimator.SetTrigger(KickHash);
        yield return WaitUnscaled(groomHitHoldDuration);
        PlayState(groomAnimator, GettingHitState);
        TriggerReaction(groomAnimator);
        yield return WaitUnscaled(1.4f);
        FaceEachOther(groom, sherall);
    }

    IEnumerator StepBackInShock()
    {
        if (sherall == null || groom == null)
            yield break;

        Vector3 start = sherall.position;
        Vector3 away = start - groom.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f)
            away = -sherall.forward;

        Vector3 destination = SnapToChurchFloor(start + away.normalized * brideShockStepDistance);
        FaceTarget(sherall, groom.position);

        CharacterController controller = sherall.GetComponent<CharacterController>();
        bool controllerWasEnabled = controller != null && controller.enabled;
        if (controllerWasEnabled)
            controller.enabled = false;

        PlayState(sherallAnimator, WalkBackState);

        float elapsed = 0f;
        while (elapsed < brideShockStepDuration && !sequenceComplete)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / brideShockStepDuration));
            sherall.position = Vector3.Lerp(start, destination, t);
            FaceTarget(sherall, groom.position);
            yield return null;
        }

        sherall.position = destination;
        RestoreAnimatorPlayback(sherallAnimator);
        FaceTarget(sherall, groom.position);

        if (controllerWasEnabled)
            controller.enabled = true;
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
        CharacterController characterController = character.GetComponent<CharacterController>();
        while (elapsed < duration && !sequenceComplete)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            Vector3 nextPosition = SnapToChurchFloor(Vector3.Lerp(start, destination, t));
            MoveWithCollision(character, characterController, nextPosition);
            yield return null;
        }

        MoveWithCollision(character, characterController, SnapToChurchFloor(destination));
        SetSpeed(animator, 0f);
    }

    IEnumerator MoveCharacterAlongPath(
        Transform character,
        Vector3[] waypoints,
        float duration,
        Animator animator,
        float speed,
        bool ignoreCollision = false)
    {
        if (character == null || waypoints == null || waypoints.Length == 0)
            yield break;

        float totalDistance = 0f;
        Vector3 previous = character.position;
        foreach (Vector3 waypoint in waypoints)
        {
            totalDistance += Vector3.Distance(previous, waypoint);
            previous = waypoint;
        }

        if (totalDistance <= 0.001f)
            yield break;

        CharacterController characterController = character.GetComponent<CharacterController>();
        bool controllerWasEnabled = characterController != null && characterController.enabled;
        if (ignoreCollision && controllerWasEnabled)
            characterController.enabled = false;

        SetSpeed(animator, speed);

        foreach (Vector3 waypoint in waypoints)
        {
            Vector3 start = character.position;
            Vector3 direction = waypoint - start;
            direction.y = 0f;
            float segmentDistance = Vector3.Distance(start, waypoint);
            float segmentDuration = duration * segmentDistance / totalDistance;
            Quaternion targetRotation = direction.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(direction)
                : character.rotation;
            Quaternion startRotation = character.rotation;
            float elapsed = 0f;

            while (elapsed < segmentDuration && !sequenceComplete)
            {
                elapsed += Time.unscaledDeltaTime;
                float linearT = Mathf.Clamp01(elapsed / Mathf.Max(segmentDuration, 0.01f));
                float smoothT = Mathf.SmoothStep(0f, 1f, linearT);
                character.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothT);
                Vector3 nextPosition = SnapToChurchFloor(Vector3.Lerp(start, waypoint, smoothT));
                if (ignoreCollision)
                    character.position = nextPosition;
                else
                    MoveWithCollision(character, characterController, nextPosition);
                yield return null;
            }

            Vector3 groundedWaypoint = SnapToChurchFloor(waypoint);
            if (ignoreCollision)
                character.position = groundedWaypoint;
            else
                MoveWithCollision(character, characterController, groundedWaypoint);
        }

        SetSpeed(animator, 0f);
        if (ignoreCollision && controllerWasEnabled)
            characterController.enabled = false;
    }

    void MoveWithCollision(
        Transform character,
        CharacterController characterController,
        Vector3 destination)
    {
        if (characterController != null && characterController.enabled)
            characterController.Move(destination - character.position);
        else
            character.position = destination;

        if (character != null)
        {
            Vector3 grounded = SnapToChurchFloor(character.position);
            if (Mathf.Abs(character.position.y - grounded.y) > 0.001f)
                character.position = grounded;
        }
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

    IEnumerator WatchApproachFromAltar(Transform subject, float duration, float fieldOfView)
    {
        if (mainCamera == null || subject == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration && !sequenceComplete)
        {
            elapsed += Time.unscaledDeltaTime;
            SetShot(
                elderWatchCameraPosition,
                Vector3.Lerp(subject.position + Vector3.up * 1.45f, ElderApproachLookTarget(), 0.35f),
                fieldOfView);
            yield return null;
        }
    }

    Vector3 ElderApproachLookTarget()
    {
        if (elder == null)
            return elderWatchCameraPosition + Vector3.forward;

        Vector3 elderChest = elder.position + Vector3.up * 1.45f;
        Vector3 aisleAhead = new Vector3(elderStartPosition.x, elderChest.y, elder.position.z);
        return Vector3.Lerp(elderChest, aisleAhead, 0.12f);
    }

    void PlaceElderAtAisleStart()
    {
        if (elder == null)
            return;

        CharacterController controller = elder.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        elder.position = SnapToChurchFloor(elderStartPosition);
        elder.rotation = Quaternion.Euler(0f, 180f, 0f);
    }

    IEnumerator FollowCharacterCamera(
        Transform subject,
        Vector3 offset,
        float duration,
        float fieldOfView)
    {
        if (mainCamera == null || subject == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration && !sequenceComplete)
        {
            elapsed += Time.unscaledDeltaTime;
            SetShot(
                subject.position + offset,
                subject.position + Vector3.up * 1.4f,
                fieldOfView);
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
        RestoreAnimatorPlayback(sherallAnimator);
        RestoreAnimatorPlayback(groomAnimator);
        RestoreAnimatorPlayback(priestAnimator);
        RestoreAnimatorPlayback(elderAnimator);
        RestoreAnimatorPlayback(aswangAnimator);
        if (infectedRearAnimators != null)
        {
            for (int i = 0; i < infectedRearAnimators.Length; i++)
                RestoreAnimatorPlayback(infectedRearAnimators[i]);
        }

        if (sceneCamera != null)
            sceneCamera.fieldOfView = gameplayFieldOfView;
        if (cinemachineBrain != null)
            cinemachineBrain.enabled = true;
        SetPlayerControl(true);
    }

    void BeginGameplayTransition()
    {
        if (isTransitioning)
            return;

        isTransitioning = true;
        sequenceComplete = true;
        currentSpeaker = "";
        currentDialogue = "";
        dialogueAlpha = 0f;
        SetPlayerControl(false);
        StopAllCoroutines();
        StartCoroutine(LoadGameplay());
    }

    IEnumerator LoadGameplay()
    {
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            screenFade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));
            yield return null;
        }

        if (Application.CanStreamedLevelBeLoaded(gameplayScene))
        {
            SceneManager.LoadScene(gameplayScene);
            yield break;
        }

        Debug.LogError($"Cannot load '{gameplayScene}'. Add the scene to Build Settings.");
        screenFade = 0f;
        isTransitioning = false;
        sequenceComplete = false;
        FinishSequence();
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

    void EnsureEnvironmentColliders()
    {
        MeshFilter[] meshFilters = FindObjectsByType<MeshFilter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter == null || meshFilter.sharedMesh == null)
                continue;

            string objectName = meshFilter.gameObject.name.ToLowerInvariant();
            bool needsCollider =
                objectName.Contains("owenground") ||
                objectName.Contains("ground") ||
                objectName.Contains("floor");
            if (!needsCollider || meshFilter.GetComponent<Collider>() != null)
                continue;

            MeshCollider meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false;
        }
    }

    void EnsureCharacterColliders()
    {
        EnsureCharacterController(sherall);
        EnsureCharacterController(groom);
        EnsureCharacterController(priest);
        EnsureCharacterController(elder);
        EnsureCharacterController(aswangGuest);
    }

    void EnsurePewSeatColliders()
    {
        Transform[] pews = FindOriginalWeddingPews();
        for (int i = 0; i < pews.Length; i++)
        {
            Transform pew = pews[i];
            if (pew.Find("PewSeatCollider") != null)
                continue;

            GameObject seat = new GameObject("PewSeatCollider");
            seat.transform.SetParent(pew, false);
            seat.transform.localPosition = Vector3.zero;
            seat.transform.localRotation = Quaternion.identity;
            BoxCollider box = seat.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.02f, 0.02f);
            box.size = new Vector3(1.6f, 0.42f, 0.1f);
            box.isTrigger = true;
        }

        Physics.SyncTransforms();
    }

    static void AttachSittingCollider(Transform guest)
    {
        if (guest == null)
            return;

        DisableCharacterController(guest);
        CapsuleCollider capsule = guest.GetComponent<CapsuleCollider>();
        if (capsule == null)
            capsule = guest.gameObject.AddComponent<CapsuleCollider>();

        capsule.direction = 1;
        capsule.center = new Vector3(0f, 0.92f, 0.02f);
        capsule.height = 0.95f;
        capsule.radius = 0.2f;
        capsule.isTrigger = true;
        capsule.enabled = true;
    }

    void SpawnSittingGuests()
    {
        if (!spawnSittingGuests)
            return;

        GuestSeat[] seats = BuildPewSeats();
        if (sittingGuests != null && sittingGuests.Length > 0)
        {
            seatedGuestPositions = new Vector3[sittingGuests.Length];
            seatedGuestYaws = new float[sittingGuests.Length];
            seatedGuestHipYs = new float[sittingGuests.Length];
            for (int i = 0; i < sittingGuests.Length; i++)
            {
                Transform guest = sittingGuests[i];
                if (guest == null)
                    continue;

                GuestSeat seat = seats[i % seats.Length];
                ApplySittingPose(guest, seat, i);
            }

            return;
        }

        if (guestSources == null || guestSources.Length == 0)
            return;

        for (int i = 0; i < seats.Length; i++)
        {
            Transform source = guestSources[i % guestSources.Length];
            if (IsForbiddenGuestSource(source))
                continue;

            Transform guest = InstantiateGuest(source, $"Wedding Guest {i + 1}", WeddingGuestScale());
            if (guest == null)
                continue;

            ApplySittingPose(guest, seats[i], i);
        }
    }

    GuestSeat[] BuildPewSeats()
    {
        Physics.SyncTransforms();
        var seats = new List<GuestSeat>(40);
        Transform[] pews = FindOriginalWeddingPews();
        int sitIndex = 0;
        for (int p = 0; p < pews.Length && seats.Count < 40; p++)
        {
            Transform pew = pews[p];
            Vector3 along = Horizontal(pew.right);
            Vector3 face = Horizontal(pew.up);
            if (face.sqrMagnitude < 0.0001f)
                face = Horizontal(-pew.forward);
            along.Normalize();
            face.Normalize();
            float yaw = Quaternion.LookRotation(face).eulerAngles.y;
            bool longPew = pew.lossyScale.x >= 1.05f && pew.position.z >= 43.2f;
            int guestCount = longPew ? 2 : 1;
            if (seats.Count + guestCount > 40)
                guestCount = 40 - seats.Count;

            for (int i = 0; i < guestCount; i++)
            {
                float slot = guestCount == 1 ? 0f : (i - (guestCount - 1) * 0.5f) * 0.38f;
                Vector3 position = pew.position + along * slot + face * pewSeatOffset - face * pewBackOffset;
                if (Mathf.Abs(position.x - brideEntryPosition.x) < 1.2f)
                    continue;

                position.y = sittingRootY;
                float hipY = pew.position.y + sittingSeatClearance;
                if (hipY < sittingHipY)
                    hipY = sittingHipY;
                seats.Add(new GuestSeat(position, yaw, hipY, sitIndex++));
            }
        }

        return seats.ToArray();
    }

    Transform[] FindOriginalWeddingPews()
    {
        var pews = new List<Transform>();
        Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Transform candidate = all[i];
            if (candidate == null || !candidate.name.StartsWith("Bench 2"))
                continue;

            Vector3 position = candidate.position;
            if (position.x < 290f || position.x > 320f || position.z < 35f || position.z > 65f || position.y < 2f)
                continue;

            pews.Add(candidate);
        }

        pews.Sort((a, b) =>
        {
            int z = a.position.z.CompareTo(b.position.z);
            return z != 0 ? z : a.position.x.CompareTo(b.position.x);
        });
        return pews.ToArray();
    }

    float WeddingGuestScale()
    {
        float scale = 0f;
        int count = 0;
        if (sherall != null)
        {
            scale += sherall.localScale.y;
            count++;
        }

        if (groom != null)
        {
            scale += groom.localScale.y;
            count++;
        }

        return count > 0 ? scale / count : 1.25f;
    }

    static Vector3 Horizontal(Vector3 value)
    {
        value.y = 0f;
        return value;
    }

    void ApplySittingPose(Transform guest, GuestSeat seat, int index)
    {
        guest.gameObject.SetActive(true);
        DisableCharacterController(guest);
        float weddingScale = WeddingGuestScale();
        guest.localScale = new Vector3(weddingScale, weddingScale, weddingScale);
        Vector3 seated = new Vector3(seat.position.x, sittingRootY, seat.position.z);
        guest.position = seated;
        guest.rotation = Quaternion.Euler(0f, seat.yaw, 0f);
        AttachSittingCollider(guest);
        if (seatedGuestPositions != null && index < seatedGuestPositions.Length)
        {
            seatedGuestPositions[index] = seated;
            seatedGuestYaws[index] = seat.yaw;
            seatedGuestHipYs[index] = seat.hipY;
        }

        Animator animator = FindAnimator(guest);
        if (animator == null)
            return;

        RuntimeAnimatorController controller = guestSitController;
        if (controller == null && sherallAnimator != null)
            controller = sherallAnimator.runtimeAnimatorController;
        if (controller != null)
            animator.runtimeAnimatorController = controller;

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.speed = 0.9f + (index % 4) * 0.05f;
        animator.Play(SittingStates[seat.sitIndex % SittingStates.Length], 0, 0.2f + (index * 0.07f) % 0.6f);
        animator.Update(0f);
    }

    void SpawnInfectedRearGuests()
    {
        if (!spawnInfectedRearGuests)
            return;

        if (infectedRearGuests != null && infectedRearGuests.Length >= 2)
        {
            infectedRearAnimators = new Animator[infectedRearGuests.Length];
            for (int i = 0; i < infectedRearGuests.Length; i++)
            {
                Transform guest = infectedRearGuests[i];
                if (guest == null)
                    continue;

                guest.gameObject.SetActive(false);
                infectedRearAnimators[i] = FindAnimator(guest);
                SetSpeed(infectedRearAnimators[i], 0f);
            }

            return;
        }

        if (aswangGuest == null)
            return;

        int startCount = infectedRearStartPositions != null ? infectedRearStartPositions.Length : 0;
        int destinationCount = infectedRearDestinations != null ? infectedRearDestinations.Length : 0;
        int count = Mathf.Min(startCount, destinationCount);
        if (count < 2)
            return;
        infectedRearGuests = new Transform[count];
        infectedRearAnimators = new Animator[count];

        for (int i = 0; i < count; i++)
        {
            Transform guest = InstantiateGuest(aswangGuest, $"Infected Rear Guest {i + 1}", 1f);
            if (guest == null)
                continue;

            guest.position = infectedRearStartPositions[i];
            Vector3 look = infectedRearDestinations[i] - infectedRearStartPositions[i];
            look.y = 0f;
            if (look.sqrMagnitude > 0.001f)
                guest.rotation = Quaternion.LookRotation(look);

            infectedRearGuests[i] = guest;
            infectedRearAnimators[i] = FindAnimator(guest);
            SetSpeed(infectedRearAnimators[i], 0f);
            guest.gameObject.SetActive(false);
        }
    }

    void RevealInfectedRearGuests()
    {
        if (infectedRearGuests == null)
            return;

        for (int i = 0; i < infectedRearGuests.Length; i++)
        {
            if (infectedRearGuests[i] != null)
                infectedRearGuests[i].gameObject.SetActive(true);
        }
    }

    IEnumerator WalkInfectedRearGuests()
    {
        if (infectedRearGuests == null)
            yield break;

        List<Coroutine> walks = new List<Coroutine>();
        for (int i = 0; i < infectedRearGuests.Length; i++)
        {
            if (infectedRearGuests[i] == null)
                continue;

            walks.Add(StartCoroutine(
                MoveCharacter(
                    infectedRearGuests[i],
                    infectedRearDestinations[i],
                    aswangWalkDuration + 0.8f + i * 0.35f,
                    infectedRearAnimators[i],
                    0.55f)));
        }

        for (int i = 0; i < walks.Count; i++)
            yield return walks[i];
    }

    Transform InstantiateGuest(Transform source, string guestName, float uniformScale)
    {
        if (source == null)
            return null;

        GameObject clone = Instantiate(source.gameObject);
        clone.name = guestName;
        clone.tag = "Untagged";
        clone.SetActive(true);
        StripGameplayFromGuest(clone);

        Transform guest = clone.transform;
        guest.SetParent(transform, true);
        guest.localScale = Vector3.one * uniformScale;
        return guest;
    }

    static void StripGameplayFromGuest(GameObject guest)
    {
        foreach (CharacterController controller in guest.GetComponentsInChildren<CharacterController>(true))
            Destroy(controller);

        foreach (Collider collider in guest.GetComponentsInChildren<Collider>(true))
        {
            if (collider == null || collider is CharacterController)
                continue;
            collider.isTrigger = true;
        }

        foreach (Camera camera in guest.GetComponentsInChildren<Camera>(true))
            Destroy(camera);

        foreach (AudioListener listener in guest.GetComponentsInChildren<AudioListener>(true))
            Destroy(listener);

        Behaviour[] behaviours = guest.GetComponentsInChildren<Behaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            Behaviour behaviour = behaviours[i];
            if (behaviour == null || behaviour is Animator)
                continue;

            string typeName = behaviour.GetType().Name;
            if (typeName.Contains("Player") ||
                typeName.Contains("Input") ||
                typeName.Contains("Cinemachine") ||
                typeName.Contains("Starter") ||
                typeName.Contains("Brain"))
            {
                behaviour.enabled = false;
            }
        }
    }

    bool IsForbiddenGuestSource(Transform source)
    {
        if (source == null)
            return true;

        return source == sherall ||
            source == groom ||
            source == elder ||
            source == aswangGuest ||
            source == priest;
    }

    struct GuestSeat
    {
        public Vector3 position;
        public float yaw;
        public float hipY;
        public int sitIndex;

        public GuestSeat(Vector3 position, float yaw, float hipY, int sitIndex)
        {
            this.position = position;
            this.yaw = yaw;
            this.hipY = hipY;
            this.sitIndex = sitIndex;
        }
    }

    void LateUpdate()
    {
        if (sequenceComplete && !awaitingCeremonyStart)
            return;

        if (!awaitingCeremonyStart)
            KeepStandingOnFloor(sherall, standingHipHeight);
        KeepStandingOnFloor(groom, standingHipHeight);
        KeepStandingOnFloor(priest, standingHipHeight);
        KeepStandingOnFloor(elder, standingHipHeight);
        KeepStandingOnFloor(aswangGuest, standingHipHeight);
        if (sittingGuests != null)
        {
            for (int i = 0; i < sittingGuests.Length; i++)
                KeepSittingOnBench(sittingGuests[i], i);
        }

        if (infectedRearGuests != null)
        {
            for (int i = 0; i < infectedRearGuests.Length; i++)
                KeepStandingOnFloor(infectedRearGuests[i], standingHipHeight);
        }
    }

    void EnsureChurchFloor()
    {
        CreateChurchGround(
            "Church Ceremony Floor",
            new Vector3(303.46f, churchFloorY, 47f),
            new Vector3(22f, 0.25f, 32f));
    }

    void OpenAisleForBride()
    {
        if (sherall == null)
            return;

        CharacterController bride = sherall.GetComponent<CharacterController>();
        if (bride == null)
            return;

        Transform[] pews = FindOriginalWeddingPews();
        for (int i = 0; i < pews.Length; i++)
        {
            IgnoreColliders(bride, pews[i]);
            DisableSolidColliders(pews[i]);
        }

        Transform[] props = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < props.Length; i++)
        {
            Transform prop = props[i];
            if (prop == null || !InWeddingNave(prop.position))
                continue;

            string objectName = prop.name.ToLowerInvariant();
            if (objectName.Contains("flower") ||
                objectName.Contains("foliage") ||
                objectName.Contains("plant") ||
                objectName.Contains("bench"))
            {
                IgnoreColliders(bride, prop);
                DisableSolidColliders(prop);
            }
        }

        if (sittingGuests != null)
        {
            for (int i = 0; i < sittingGuests.Length; i++)
                IgnoreColliders(bride, sittingGuests[i]);
        }

        IgnoreColliders(bride, groom);
        IgnoreColliders(bride, priest);
    }

    static bool InWeddingNave(Vector3 position)
    {
        return position.x >= 290f && position.x <= 320f &&
            position.z >= 35f && position.z <= 65f &&
            position.y >= 2f && position.y <= 6f;
    }

    static void IgnoreColliders(CharacterController bride, Transform root)
    {
        if (bride == null || root == null)
            return;

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider other = colliders[i];
            if (other == null || other == bride)
                continue;
            Physics.IgnoreCollision(bride, other, true);
        }
    }

    static void DisableSolidColliders(Transform root)
    {
        if (root == null)
            return;

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider other = colliders[i];
            if (other == null || other.isTrigger || other is CharacterController)
                continue;
            other.enabled = false;
        }
    }

    static float HorizontalDistanceSq(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    void CreateChurchGround(string objectName, Vector3 position, Vector3 size)
    {
        GameObject ground = new GameObject(objectName);
        ground.layer = 0;
        ground.transform.SetParent(transform, false);
        ground.transform.position = position;
        BoxCollider collider = ground.AddComponent<BoxCollider>();
        collider.size = size;
        collider.center = new Vector3(0f, -size.y * 0.5f + 0.01f, 0f);
    }

    void LockAllAnimatorsToGround()
    {
        LockAnimator(sherallAnimator);
        LockAnimator(groomAnimator);
        LockAnimator(priestAnimator);
        LockAnimator(elderAnimator);
        LockAnimator(aswangAnimator);
        if (sittingGuests != null)
        {
            for (int i = 0; i < sittingGuests.Length; i++)
                LockAnimator(FindAnimator(sittingGuests[i]));
        }

        if (infectedRearGuests != null)
        {
            for (int i = 0; i < infectedRearGuests.Length; i++)
                LockAnimator(FindAnimator(infectedRearGuests[i]));
        }
    }

    static void LockAnimator(Animator animator)
    {
        if (animator == null)
            return;

        animator.applyRootMotion = false;
        animator.updateMode = AnimatorUpdateMode.Normal;
    }

    float GetChurchFloorY(Vector3 position)
    {
        return position.z <= altarFrontZ ? altarFloorY : churchFloorY;
    }

    Vector3 SnapToChurchFloor(Vector3 position)
    {
        position.y = GetChurchFloorY(position);
        return position;
    }

    void KeepStandingOnFloor(Transform character, float hipHeight)
    {
        if (character == null || !character.gameObject.activeInHierarchy)
            return;

        float floorY = GetChurchFloorY(character.position);
        Vector3 position = character.position;
        position.y = floorY;
        character.position = position;
        LiftHipsToMinimum(character, floorY + hipHeight);
    }

    void KeepSittingOnBench(Transform character, int index)
    {
        if (character == null || !character.gameObject.activeInHierarchy)
            return;

        DisableCharacterController(character);
        Vector3 seated = character.position;
        float yaw = character.eulerAngles.y;
        if (seatedGuestPositions != null && index < seatedGuestPositions.Length && seatedGuestPositions[index] != Vector3.zero)
        {
            seated = seatedGuestPositions[index];
            yaw = seatedGuestYaws[index];
        }

        seated.y = sittingRootY;
        character.position = seated;
        character.rotation = Quaternion.Euler(0f, yaw, 0f);
        float hipY = sittingHipY;
        if (seatedGuestHipYs != null && index < seatedGuestHipYs.Length && seatedGuestHipYs[index] > 0.1f)
            hipY = seatedGuestHipYs[index];
        PinHipsTo(character, hipY);
    }

    static void LiftHipsToMinimum(Transform character, float minHipY)
    {
        Transform hips = GetHips(character);
        if (hips != null)
        {
            float lift = minHipY - hips.position.y;
            if (lift > 0.01f)
                character.position += Vector3.up * lift;
            return;
        }

        LiftMeshAbove(character, minHipY - 0.9f);
    }

    static void PinHipsTo(Transform character, float hipY)
    {
        Transform hips = GetHips(character);
        if (hips != null)
        {
            character.position += Vector3.up * (hipY - hips.position.y);
            return;
        }

        LiftMeshAbove(character, hipY - 0.45f);
    }

    static void LiftMeshAbove(Transform character, float minY)
    {
        SkinnedMeshRenderer renderer = character.GetComponentInChildren<SkinnedMeshRenderer>();
        if (renderer == null)
            return;

        float lift = minY - renderer.bounds.min.y;
        if (lift > 0.01f)
            character.position += Vector3.up * lift;
    }

    static Transform GetHips(Transform character)
    {
        Animator animator = FindAnimator(character);
        if (animator == null)
            return null;

        animator.applyRootMotion = false;
        return animator.GetBoneTransform(HumanBodyBones.Hips);
    }

    static void DisableCharacterController(Transform character)
    {
        if (character == null)
            return;

        CharacterController controller = character.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;
    }

    static void EnsureCharacterController(Transform character)
    {
        if (character == null || character.GetComponent<CharacterController>() != null)
            return;

        CharacterController controller = character.gameObject.AddComponent<CharacterController>();
        controller.center = new Vector3(0f, 1f, 0f);
        controller.height = 2f;
        controller.radius = 0.3f;
        controller.slopeLimit = 45f;
        controller.stepOffset = 0.25f;
        controller.skinWidth = 0.06f;
        controller.minMoveDistance = 0.001f;
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

    static void PlayState(Animator animator, string stateName)
    {
        if (animator != null && !string.IsNullOrEmpty(stateName))
            animator.Play(stateName, 0, 0f);
    }

    static void RestoreAnimatorPlayback(Animator animator)
    {
        if (animator == null)
            return;

        animator.speed = 1f;
        animator.SetFloat(SpeedHash, 0f);
        animator.Play("Blend Tree", 0, 0f);
    }

    static void FaceTarget(Transform character, Vector3 worldTarget)
    {
        if (character == null)
            return;

        Vector3 direction = worldTarget - character.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            character.rotation = Quaternion.LookRotation(direction);
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
        if (screenFade > 0f)
        {
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, screenFade);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), panelTexture);
            GUI.color = previous;
        }

        if (awaitingCeremonyStart)
        {
            DrawCeremonyPrompt();
            return;
        }

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

    void DrawCeremonyPrompt()
    {
        EnsureStyles();
        float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1920f, Screen.height / 1080f), 0.7f, 1.4f);
        float panelWidth = Mathf.Min(Screen.width * 0.62f, 980f * scale);
        float panelHeight = 108f * scale;
        float panelX = (Screen.width - panelWidth) * 0.5f;
        float panelY = Screen.height - panelHeight - 48f * scale;

        GUI.DrawTexture(new Rect(panelX, panelY, panelWidth, panelHeight), panelTexture);
        GUI.DrawTexture(new Rect(panelX, panelY, 6f * scale, panelHeight), accentTexture);

        speakerStyle.fontSize = Mathf.RoundToInt(20f * scale);
        dialogueStyle.fontSize = Mathf.RoundToInt(24f * scale);
        float left = panelX + 34f * scale;
        GUI.Label(
            new Rect(left, panelY + 14f * scale, panelWidth - 60f * scale, 28f * scale),
            "BRIDAL ENTRANCE",
            speakerStyle);
        GUI.Label(
            new Rect(left, panelY + 44f * scale, panelWidth - 60f * scale, 50f * scale),
            "Use WASD to walk Sherall down the flower aisle. The ceremony begins when you reach the front, before the groom.",
            dialogueStyle);
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

    void OpenExteriorChurchOpenings()
    {
        Transform[] sceneTransforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (Transform sceneTransform in sceneTransforms)
        {
            if (sceneTransform == null)
                continue;

            string objectName = sceneTransform.name;
            bool isExteriorDoorLeaf =
                objectName == "Front_Door_L" ||
                objectName == "Front_Door_R" ||
                objectName == "Front_Door_L_1" ||
                objectName == "Front_Door_R_1";
            bool isExteriorWindow =
                objectName == "OwenDoor_Bigwindow" ||
                objectName == "Graveyard From";
            if (!isExteriorDoorLeaf && !isExteriorWindow)
                continue;

            sceneTransform.gameObject.SetActive(false);
        }
    }

    void CreateCandleGlow()
    {
        Vector3[] positions =
        {
            new Vector3(303.46f, 4.15f, 36.1f),
            new Vector3(303.46f, 3.65f, 42f),
            new Vector3(303.46f, 3.65f, 47.5f),
            new Vector3(303.46f, 3.65f, 53.5f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject glow = new GameObject($"Wedding Candle Glow {i + 1}");
            glow.transform.SetParent(transform, false);
            glow.transform.position = positions[i];

            Light candleLight = glow.AddComponent<Light>();
            candleLight.type = LightType.Point;
            candleLight.color = candleGlowColor;
            candleLight.intensity = candleGlowIntensity;
            candleLight.range = candleGlowRange;
            candleLight.shadows = LightShadows.None;
        }
    }

    void CreateCeilingLights()
    {
        Transform ceilingFixture = null;
        Transform[] sceneTransforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (Transform sceneTransform in sceneTransforms)
        {
            if (!sceneTransform.name.Contains("Ceiling_lamp"))
                continue;

            ceilingFixture = sceneTransform;
            Renderer fixtureRenderer = sceneTransform.GetComponent<Renderer>();
            if (fixtureRenderer != null)
                fixtureRenderer.enabled = true;
            break;
        }

        Vector3[] positions =
        {
            new Vector3(298.5f, 7.2f, 39f),
            new Vector3(300f, 7.2f, 44f),
            new Vector3(302f, 7.2f, 49f),
            new Vector3(304f, 7.2f, 54f),
            new Vector3(305.5f, 7.2f, 58f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject ceilingLightObject = new GameObject($"Wedding Ceiling Light {i + 1}");
            Transform lightTransform = ceilingLightObject.transform;
            lightTransform.SetPositionAndRotation(positions[i], Quaternion.Euler(90f, 0f, 0f));
            lightTransform.SetParent(ceilingFixture != null ? ceilingFixture : transform, true);

            Light ceilingLight = ceilingLightObject.AddComponent<Light>();
            ceilingLight.type = LightType.Spot;
            ceilingLight.color = ceilingLightColor;
            ceilingLight.intensity = ceilingLightIntensity;
            ceilingLight.range = ceilingLightRange;
            ceilingLight.spotAngle = 75f;
            ceilingLight.innerSpotAngle = 48f;
            ceilingLight.shadows = LightShadows.None;
        }
    }

    void OnDisable()
    {
        if (!sequenceComplete && !isTransitioning)
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
