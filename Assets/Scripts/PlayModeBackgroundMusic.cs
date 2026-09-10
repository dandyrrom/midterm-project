using System.Collections;
using UnityEngine;

/// <summary>
/// Loops ambient environment audio in Play Mode. Tune clip, volume, fade, and pitch in the Inspector.
/// Fades out when Level 1 end / game-over panel opens.
/// </summary>
[DisallowMultipleComponent]
public class PlayModeBackgroundMusic : MonoBehaviour
{
    [Header("Clip")]
    [Tooltip("Ambient loop for Level 1 play (e.g. crickets-at-night-raw-sound).")]
    public AudioClip musicClip;

    [Header("Playback")]
    [Tooltip("Start music when Play Mode begins.")]
    public bool playOnStart = true;
    public bool loop = true;
    [Range(0f, 1f)] public float volume = 0.35f;
    [Range(0.5f, 1.5f)] public float pitch = 1f;
    [Tooltip("Mute without clearing the clip.")]
    public bool muted;

    [Header("Fade")]
    public bool fadeInOnStart = true;
    [Min(0f)] public float fadeInDuration = 2f;
    [Min(0f)] public float fadeOutDuration = 1.25f;

    [Header("End of level")]
    [Tooltip("Fade out when Level 1 end / game-over panel opens.")]
    public bool fadeOutOnLevelEnd = true;

    AudioSource source;
    Coroutine fadeRoutine;
    float targetVolume;

    public bool IsPlaying => source != null && source.isPlaying;

    void Awake()
    {
        source = gameObject.GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = loop;
        source.pitch = pitch;
        source.volume = 0f;
        ApplyClip();
    }

    void Start()
    {
        if (playOnStart)
            Play();
    }

    void OnDestroy()
    {
        StopFade();
        if (source != null && source.isPlaying)
            source.Stop();
    }

    void OnValidate()
    {
        targetVolume = muted ? 0f : Mathf.Clamp01(volume);
        if (source == null)
            return;

        source.loop = loop;
        source.pitch = pitch;
        if (fadeRoutine == null && Application.isPlaying)
            source.volume = targetVolume;
    }

    public void Play()
    {
        if (source == null || musicClip == null)
            return;

        ApplyClip();
        source.loop = loop;
        source.pitch = pitch;
        targetVolume = muted ? 0f : Mathf.Clamp01(volume);

        if (!source.isPlaying)
            source.Play();

        if (fadeInOnStart && fadeInDuration > 0f)
            StartFade(0f, targetVolume, fadeInDuration);
        else
            source.volume = targetVolume;
    }

    public void StopImmediate()
    {
        StopFade();
        if (source != null && source.isPlaying)
            source.Stop();
        if (source != null)
            source.volume = 0f;
    }

    public void FadeOutAndStop()
    {
        if (source == null || !source.isPlaying)
            return;

        float duration = Mathf.Max(0f, fadeOutDuration);
        if (duration <= 0f)
        {
            StopImmediate();
            return;
        }

        StartFade(source.volume, 0f, duration, stopWhenDone: true);
    }

    public void NotifyLevelEnded()
    {
        if (fadeOutOnLevelEnd)
            FadeOutAndStop();
    }

    void ApplyClip()
    {
        if (source == null || musicClip == null)
            return;

        if (source.clip != musicClip)
            source.clip = musicClip;
    }

    void StartFade(float from, float to, float duration, bool stopWhenDone = false)
    {
        StopFade();
        fadeRoutine = StartCoroutine(FadeRoutine(from, to, duration, stopWhenDone));
    }

    void StopFade()
    {
        if (fadeRoutine == null)
            return;

        StopCoroutine(fadeRoutine);
        fadeRoutine = null;
    }

    IEnumerator FadeRoutine(float from, float to, float duration, bool stopWhenDone)
    {
        source.volume = from;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            source.volume = Mathf.Lerp(from, to, t);
            yield return null;
        }

        source.volume = to;
        fadeRoutine = null;

        if (stopWhenDone && source.isPlaying)
            source.Stop();
    }
}
