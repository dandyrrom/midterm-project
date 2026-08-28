using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BawangHUD : MonoBehaviour
{
    public BawangInventory inventory;
    public Image icon;
    public TMP_Text countText;

    [Header("Empty")]
    [Range(0f, 1f)] public float emptyAlpha = 0.55f;

    [Header("Full Feedback")]
    public float flashDuration = 0.35f;
    public float flashScale = 1.15f;
    public Color flashCountColor = new Color(1f, 0.35f, 0.35f, 1f);

    RectTransform iconRect;
    Vector3 iconBaseScale = Vector3.one;
    Color countBaseColor = Color.white;
    Coroutine flashRoutine;

    void Awake()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<BawangInventory>();

        if (icon != null)
        {
            iconRect = icon.rectTransform;
            iconBaseScale = iconRect.localScale;
        }

        if (countText != null)
            countBaseColor = countText.color;
    }

    void OnEnable()
    {
        if (inventory != null)
            inventory.OnCountChanged += Refresh;
    }

    void OnDisable()
    {
        if (inventory != null)
            inventory.OnCountChanged -= Refresh;
    }

    void Start() => Refresh(inventory != null ? inventory.count : 0);

    public void ShowFullFeedback()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashFullRoutine());
    }

    void Refresh(int amount)
    {
        int max = inventory != null ? inventory.maxCount : 5;

        if (icon != null)
        {
            Color c = icon.color;
            c.a = amount > 0 ? 1f : emptyAlpha;
            icon.color = c;
        }

        if (countText != null)
            countText.text = $"{amount}/{max}";
    }

    IEnumerator FlashFullRoutine()
    {
        float half = flashDuration * 0.5f;
        float elapsed = 0f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            if (iconRect != null)
                iconRect.localScale = Vector3.Lerp(iconBaseScale, iconBaseScale * flashScale, t);
            if (countText != null)
                countText.color = Color.Lerp(countBaseColor, flashCountColor, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            if (iconRect != null)
                iconRect.localScale = Vector3.Lerp(iconBaseScale * flashScale, iconBaseScale, t);
            if (countText != null)
                countText.color = Color.Lerp(flashCountColor, countBaseColor, t);
            yield return null;
        }

        if (iconRect != null)
            iconRect.localScale = iconBaseScale;
        if (countText != null)
            countText.color = countBaseColor;

        flashRoutine = null;
    }
}
