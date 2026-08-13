using UnityEngine;

public static class SoftSfx
{
    public static void Play(Vector3 position, float frequency, float volume = 0.35f)
    {
        var clip = MakeTone(frequency, 0.12f);
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    static AudioClip MakeTone(float frequency, float duration)
    {
        const int sampleRate = 22050;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        var clip = AudioClip.Create("tone", samples, 1, sampleRate, false);
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = 1f - (i / (float)samples);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;
        }
        clip.SetData(data, 0);
        return clip;
    }
}
