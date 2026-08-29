using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZombieKillHUD : MonoBehaviour
{
    public ZombieKillScore score;
    public Image icon;
    public TMP_Text countText;

    void Awake()
    {
        if (score == null)
            score = FindFirstObjectByType<ZombieKillScore>();
    }

    void OnEnable()
    {
        if (score != null)
            score.OnScoreChanged += Refresh;
    }

    void OnDisable()
    {
        if (score != null)
            score.OnScoreChanged -= Refresh;
    }

    void Start()
    {
        if (score != null)
            Refresh(score.Killed, score.Total);
        else if (countText != null)
            countText.text = "0/0";
    }

    void Refresh(int killed, int total)
    {
        if (countText != null)
            countText.text = $"{killed}/{total}";
    }
}
