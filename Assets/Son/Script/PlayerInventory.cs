using UnityEngine;
using TMPro;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;

    public int totalCoins = 0;
    public TextMeshProUGUI coinTextUI;

    private const string COIN_KEY = "TotalCoins"; // Key lưu coin trong PlayerPrefs

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        LoadData(); // Load dữ liệu khi game bắt đầu
    }

    // Thêm coin và lưu lại
    public void AddCoin(int amount)
    {
        totalCoins += amount;
        UpdateUI();
        SaveData();
    }

    // Trừ coin nếu đủ và lưu lại
    public bool TrySpendCoins(int amount)
    {
        if (totalCoins >= amount)
        {
            totalCoins -= amount;
            UpdateUI();
            SaveData();
            return true;
        }
        return false;
    }

    public void UpdateUI()
    {
        if (coinTextUI != null)
            coinTextUI.text = $"{totalCoins}";
    }

    // Lưu dữ liệu vào PlayerPrefs
    public void SaveData()
    {
        PlayerPrefs.SetInt(COIN_KEY, totalCoins);
        PlayerPrefs.Save();
    }

    // Load dữ liệu từ PlayerPrefs
    public void LoadData()
    {
        if (PlayerPrefs.HasKey(COIN_KEY))
        {
            totalCoins = PlayerPrefs.GetInt(COIN_KEY);
            UpdateUI();
        }
    }
}
