using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth health;
    public Image healthFill;
    public Image damageFill;

    [Header("Damage Trail")]
    public float damageTrailSpeed = 1.5f;

    void Awake()
    {
        if (health == null)
            health = GetComponent<PlayerHealth>();
    }

    void OnEnable()
    {
        if (health != null)
            health.OnHealthChanged += HandleHealthChanged;
    }

    void OnDisable()
    {
        if (health != null)
            health.OnHealthChanged -= HandleHealthChanged;
    }

    void Start()
    {
        if (health == null || healthFill == null || damageFill == null)
            return;

        float fill = (float)health.CurrentHealth / health.MaxHealth;
        healthFill.fillAmount = fill;
        damageFill.fillAmount = fill;
    }

    void Update()
    {
        if (healthFill == null || damageFill == null)
            return;

        damageFill.fillAmount = Mathf.MoveTowards(
            damageFill.fillAmount,
            healthFill.fillAmount,
            damageTrailSpeed * Time.deltaTime);
    }

    void HandleHealthChanged(int current, int max)
    {
        if (healthFill == null || damageFill == null || max <= 0)
            return;

        float newFill = (float)current / max;
        healthFill.fillAmount = newFill;

        // Keep red bar wider so the lost chunk shows as red.
        if (newFill < damageFill.fillAmount)
            return;

        damageFill.fillAmount = newFill;
    }
}