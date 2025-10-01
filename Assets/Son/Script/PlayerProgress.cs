using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[Serializable]
public class PlayerProgressData
{
    public int level = 1;
    public int currentExp = 0;
    public int maxExp = 100;
    public int coins = 0;

    public List<string> unlockedFeatures = new List<string>();
    public List<string> unlockedCharacterIds = new List<string>();
    public List<string> claimableCharacterIds = new List<string>();

    // NEW: Booyah Pass claim tracking
    public List<int> claimedBooyahLevels = new List<int>();

    public int version = 2; // bump version
}

public class PlayerProgress : MonoBehaviour
{
    public static PlayerProgress Instance;

    [Header("EXP Settings")]
    public int expGrowthStep = 50;
    public int MaxLevel = 60; // NEW

    [Header("UI References")]
    public Slider expBar;
    public TextMeshProUGUI expText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI coinsText;

    [Header("Debug")]
    [SerializeField] private bool logIO = true;

    private string savePath;
    private string legacyPath;

    public PlayerProgressData Data { get; private set; } = new PlayerProgressData();

    // Events
    public event Action OnExpChanged;
    public event Action<int> OnLevelUp; // gửi level mới
    public event Action OnCoinsChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            savePath = Path.Combine(Application.persistentDataPath, "playerexp.json");
            legacyPath = Path.Combine(Application.persistentDataPath, "playerdata.json");

            if (logIO) Debug.Log("[PlayerProgress] Save path: " + savePath + "  Legacy path: " + legacyPath);

            Load();

            Data.level = Mathf.Clamp(Data.level, 1, MaxLevel);
            Data.maxExp = Mathf.Max(1, Data.maxExp);

            Save();
            UpdateUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        // Nếu đã max level -> không cộng nữa
        if (Data.level >= MaxLevel)
        {
            Data.level = MaxLevel;
            Data.currentExp = Data.maxExp; // giữ full thanh
            Save();
            OnExpChanged?.Invoke();
            UpdateUI();
            if (logIO) Debug.Log($"[PlayerProgress] Reached MaxLevel {MaxLevel}. EXP won’t increase.");
            return;
        }

        Data.currentExp += amount;

        bool leveledUp = false;
        while (Data.currentExp >= Data.maxExp && Data.level < MaxLevel)
        {
            Data.currentExp -= Data.maxExp;
            Data.level++;
            // Nếu vừa đạt MaxLevel -> khóa EXP ở full
            if (Data.level >= MaxLevel)
            {
                Data.level = MaxLevel;
                Data.currentExp = Data.maxExp;
                leveledUp = true;
                break;
            }
            Data.maxExp += expGrowthStep;
            leveledUp = true;
        }

        Save();
        OnExpChanged?.Invoke();
        UpdateUI();
        if (leveledUp) OnLevelUp?.Invoke(Data.level);

        if (logIO) Debug.Log($"[PlayerProgress] AddExp {amount} -> {Data.currentExp}/{Data.maxExp} Level {Data.level}");
    }

    public void AddCoins(int amount)
    {
        Data.coins += amount;
        Save();
        OnCoinsChanged?.Invoke();
        UpdateUI();

        if (logIO) Debug.Log($"[PlayerProgress] AddCoins {amount} -> {Data.coins}");
    }

    public void ResetProgress()
    {
        Data = new PlayerProgressData();
        Save();
        OnExpChanged?.Invoke();
        OnCoinsChanged?.Invoke();
        OnLevelUp?.Invoke(Data.level);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (expBar != null)
        {
            expBar.maxValue = Data.maxExp;
            expBar.value = Data.currentExp;
        }
        if (expText != null)
            expText.text = $"{Data.currentExp}/{Data.maxExp}";
        if (levelText != null)
            levelText.text = $"Level {Data.level}";
        if (coinsText != null)
            coinsText.text = $"{Data.coins}";
    }

    public void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(Data, true);
            File.WriteAllText(savePath, json);
            if (logIO) Debug.Log("[PlayerProgress] Saved JSON:\n" + json);
        }
        catch (Exception e)
        {
            Debug.LogError("[PlayerProgress] Save error: " + e);
        }
    }

    public void Load()
    {
        try
        {
            if (File.Exists(savePath))
            {
                string json = File.ReadAllText(savePath);
                var loaded = JsonUtility.FromJson<PlayerProgressData>(json);
                if (loaded != null) Data = loaded;
                if (logIO) Debug.Log("[PlayerProgress] Loaded JSON:\n" + json);
            }
            else if (File.Exists(legacyPath))
            {
                if (logIO) Debug.Log("[PlayerProgress] Found legacy playerdata.json -> migrating.");
                string legacyJson = File.ReadAllText(legacyPath);
                try
                {
                    var legacy = JsonUtility.FromJson<LegacyPlayerData>(legacyJson);
                    if (legacy != null)
                    {
                        Data.level = Mathf.Max(1, legacy.level);
                        Data.currentExp = legacy.currentExp;
                        Data.maxExp = legacy.maxExp;
                        Data.coins = legacy.coins;
                        Data.unlockedCharacterIds = legacy.unlockedCharacterIds ?? new List<string>();
                        Data.claimableCharacterIds = legacy.claimableCharacterIds ?? new List<string>();

                        // NEW: init empty list for booyah claimed
                        Data.claimedBooyahLevels = new List<int>();

                        if (logIO) Debug.Log("[PlayerProgress] Migration success.");
                        Save();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[PlayerProgress] Migration failed: " + ex);
                    Data = new PlayerProgressData();
                    Save();
                }
            }
            else
            {
                Data = new PlayerProgressData();
                Save();
                if (logIO) Debug.Log("[PlayerProgress] No saved file found. Created default.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[PlayerProgress] Load error: " + e);
            Data = new PlayerProgressData();
        }
    }

    [Serializable]
    private class LegacyPlayerData
    {
        public int level;
        public int currentExp;
        public int maxExp;
        public int coins;
        public List<string> unlockedCharacterIds;
        public List<string> claimableCharacterIds;
    }
}
