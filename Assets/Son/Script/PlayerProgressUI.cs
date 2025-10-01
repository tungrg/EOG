using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerProgressUI : MonoBehaviour
{
    [Header("EXP UI")]
    public Slider expBar;
    public TextMeshProUGUI expText;
    public TextMeshProUGUI levelText;

    [Header("Coins UI (optional)")]
    public TextMeshProUGUI coinsText;

    private void OnEnable()
    {
        if (PlayerProgress.Instance != null)
        {
            PlayerProgress.Instance.OnExpChanged += Refresh;
            PlayerProgress.Instance.OnLevelUp += OnLevelUp;
            PlayerProgress.Instance.OnCoinsChanged += Refresh;
        }
        Refresh();
    }

    private void OnDisable()
    {
        if (PlayerProgress.Instance != null)
        {
            PlayerProgress.Instance.OnExpChanged -= Refresh;
            PlayerProgress.Instance.OnLevelUp -= OnLevelUp;
            PlayerProgress.Instance.OnCoinsChanged -= Refresh;
        }
    }

    private void Start()
    {
        Refresh();
    }

    private void OnLevelUp(int newLevel)
    {
        Refresh();
    }

    private void Refresh()
    {
        var p = PlayerProgress.Instance;
        if (p == null) return;

        if (expBar != null)
        {
            expBar.maxValue = p.Data.maxExp;
            expBar.value = p.Data.currentExp;
        }

        if (expText != null) expText.text = $"{p.Data.currentExp}/{p.Data.maxExp}";
        if (levelText != null) levelText.text = $"Level {p.Data.level}";
        if (coinsText != null) coinsText.text = p.Data.coins.ToString();
    }
}
