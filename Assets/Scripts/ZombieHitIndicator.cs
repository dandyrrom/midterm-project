using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieHitIndicator : MonoBehaviour
{
    public Color hitColor = new Color(1f, 0.2f, 0.2f, 1f);
    public float flashDuration = 0.35f;

    readonly List<RendererSlot> rendererSlots = new List<RendererSlot>();
    Coroutine flashRoutine;
    bool cached;

    struct RendererSlot
    {
        public Renderer renderer;
        public int materialIndex;
        public MaterialPropertyBlock block;
        public Color baseColor;
    }

    void CacheRenderers()
    {
        if (cached)
            return;

        rendererSlots.Clear();

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer == null)
                continue;

            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block, i);

                Color baseColor = ReadBaseColor(block, materials[i]);
                rendererSlots.Add(new RendererSlot
                {
                    renderer = renderer,
                    materialIndex = i,
                    block = block,
                    baseColor = baseColor
                });
            }
        }

        cached = true;
    }

    static Color ReadBaseColor(MaterialPropertyBlock block, Material sharedMaterial)
    {
        if (block.HasColor("_BaseColor"))
            return block.GetColor("_BaseColor");
        if (block.HasColor("_Color"))
            return block.GetColor("_Color");
        if (sharedMaterial != null && sharedMaterial.HasProperty("_BaseColor"))
            return sharedMaterial.GetColor("_BaseColor");
        if (sharedMaterial != null && sharedMaterial.HasProperty("_Color"))
            return sharedMaterial.GetColor("_Color");

        return Color.white;
    }

    public void Flash()
    {
        CacheRenderers();

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        ApplyColor(hitColor);

        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / flashDuration);

            for (int i = 0; i < rendererSlots.Count; i++)
            {
                RendererSlot slot = rendererSlots[i];
                Color color = Color.Lerp(slot.baseColor, hitColor, t * 0.85f);
                ApplyColorToSlot(slot, color);
            }

            yield return null;
        }

        RestoreColors();
        flashRoutine = null;
    }

    void ApplyColor(Color color)
    {
        for (int i = 0; i < rendererSlots.Count; i++)
            ApplyColorToSlot(rendererSlots[i], color);
    }

    static void ApplyColorToSlot(RendererSlot slot, Color color)
    {
        slot.block.SetColor("_BaseColor", color);
        slot.block.SetColor("_Color", color);
        slot.renderer.SetPropertyBlock(slot.block, slot.materialIndex);
    }

    void RestoreColors()
    {
        for (int i = 0; i < rendererSlots.Count; i++)
        {
            RendererSlot slot = rendererSlots[i];
            slot.renderer.SetPropertyBlock(null, slot.materialIndex);
        }
    }
}
