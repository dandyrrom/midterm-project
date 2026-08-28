using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BawangHUD : MonoBehaviour
{
    public BawangInventory inventory;
    public Image icon;
    public TMP_Text countText;

    [Range(0f, 1f)] public float emptyAlpha = 0.55f;

    void Awake()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<BawangInventory>();
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

    void Refresh(int amount)
    {
        if (icon != null)
        {
            Color c = icon.color;
            c.a = amount > 0 ? 1f : emptyAlpha;
            icon.color = c;
        }

        if (countText != null)
            countText.text = amount.ToString();
    }
}