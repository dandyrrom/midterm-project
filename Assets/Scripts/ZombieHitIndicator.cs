using System.Collections;
using UnityEngine;

public class ZombieHitIndicator : MonoBehaviour
{
    public Color hitColor = new Color(1f, 0.2f, 0.2f, 1f);
    public float flashDuration = 0.35f;

    Renderer[] renderers;
    Color[] baseColors;
    Coroutine flashRoutine;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            baseColors[i] = renderers[i].material.color;
    }

    public void Flash()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        SetColor(hitColor);

        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / flashDuration);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;

                renderers[i].material.color = Color.Lerp(baseColors[i], hitColor, t * 0.85f);
            }

            yield return null;
        }

        RestoreColors();
        flashRoutine = null;
    }

    void SetColor(Color color)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            renderers[i].material.color = color;
        }
    }

    void RestoreColors()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            renderers[i].material.color = baseColors[i];
        }
    }
}
