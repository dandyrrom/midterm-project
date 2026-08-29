using UnityEngine;
using UnityEngine.UI;

public class LivesHUD : MonoBehaviour
{
    public PlayerLives lives;
    [Tooltip("Life icons left-to-right. Index 0 = first life.")]
    public Image[] lifeIcons;

    [Range(0f, 1f)]
    public float emptyAlpha = 0.25f;

    void Awake()
    {
        if (lives == null)
            lives = FindFirstObjectByType<PlayerLives>();
    }

    void OnEnable()
    {
        if (lives != null)
            lives.OnLivesChanged += Refresh;
    }

    void OnDisable()
    {
        if (lives != null)
            lives.OnLivesChanged -= Refresh;
    }

    void Start()
    {
        if (lives != null)
            Refresh(lives.CurrentLives, lives.MaxLives);
    }

    void Refresh(int current, int max)
    {
        if (lifeIcons == null)
            return;

        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null)
                continue;

            bool filled = i < current;
            Color c = lifeIcons[i].color;
            c.a = filled ? 1f : emptyAlpha;
            lifeIcons[i].color = c;
            lifeIcons[i].enabled = true;
        }
    }
}
