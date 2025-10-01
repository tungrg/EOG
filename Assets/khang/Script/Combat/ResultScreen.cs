using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class ResultScreen : MonoBehaviour
{
    [Header("UI refs")]
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI historyText;
    [SerializeField] private Transform stonesContainer;
    [SerializeField] private Transform equipmentsContainer; // NEW
    [SerializeField] private GameObject stoneItemPrefab;
    [SerializeField] private GameObject equipmentItemPrefab; // NEW
    [SerializeField] private Button returnButton;

    [Header("Data")]
    [SerializeField] private InventoryData inventoryData; // để add item vào túi

    private void Awake()
    {
        if (returnButton != null)
        {
            returnButton.onClick.AddListener(() => SceneManager.LoadScene("Loading"));
        }
        else
        {
            DebugLogger.LogError("ReturnButton not assigned in ResultScreen Inspector.");
        }

        if (stonesContainer == null || stoneItemPrefab == null)
        {
            DebugLogger.LogError("StonesContainer or StoneItemPrefab not assigned in ResultScreen Inspector.");
        }
        if (equipmentsContainer == null || equipmentItemPrefab == null)
        {
            DebugLogger.LogError("EquipmentsContainer or EquipmentItemPrefab not assigned in ResultScreen Inspector.");
        }
    }

    public void ShowResult(bool victory,
        int playerTotalDamage,
        int enemyTotalDamage,
        int turnCount,
        List<StoneData> stones = null,
        List<EquipmentData> equipments = null)
    {
        gameObject.SetActive(true);

        // 🔹 Ẩn nút Return trước
        if (returnButton != null)
            returnButton.gameObject.SetActive(false);

        // Result text
        if (resultText != null)
            resultText.text = victory ? "Victory!" : "Defeat!";

        if (historyText != null)
        {
            historyText.text = $"Turns: {turnCount}\n" +
                              $"Player Team Total Damage: {playerTotalDamage}\n" +
                              $"Enemy Team Total Damage: {enemyTotalDamage}";
        }

        // Clear old items
        foreach (Transform child in stonesContainer) Destroy(child.gameObject);
        foreach (Transform child in equipmentsContainer) Destroy(child.gameObject);

        // Spawn stones
        if (stones != null && stones.Count > 0)
        {
            var stoneGroups = stones.GroupBy(s => s)
                                   .Select(g => new { Stone = g.Key, Count = g.Count() })
                                   .ToList();

            foreach (var group in stoneGroups)
            {
                GameObject stoneItem = Instantiate(stoneItemPrefab, stonesContainer);
                Image stoneImage = stoneItem.GetComponent<Image>();
                TextMeshProUGUI stoneText = stoneItem.GetComponentInChildren<TextMeshProUGUI>();

                if (stoneImage != null && group.Stone.ItemSprite != null)
                    stoneImage.sprite = group.Stone.ItemSprite;

                if (stoneText != null)
                    stoneText.text = $"x{group.Count}"; // Chỉ hiển thị số lượng đá
            }
            stonesContainer.gameObject.SetActive(true);

            if (inventoryData != null)
                inventoryData.AddItems(stones.Cast<ItemData>().ToList());
        }
        else stonesContainer.gameObject.SetActive(false);

        // Spawn equipments
        if (equipments != null && equipments.Count > 0)
        {
            foreach (var eq in equipments)
            {
                GameObject eqItem = Instantiate(equipmentItemPrefab, equipmentsContainer);
                RewardItemUI ui = eqItem.GetComponent<RewardItemUI>();
                if (ui != null) ui.Setup(eq);
            }
            equipmentsContainer.gameObject.SetActive(true);

            if (inventoryData != null)
                inventoryData.AddItems(equipments.Cast<ItemData>().ToList());
        }
        else equipmentsContainer.gameObject.SetActive(false);

        // 🔹 Bắt đầu Coroutine đợi 5s mới hiện returnButton
        StartCoroutine(ShowReturnButtonAfterDelay(1.5f));
    }

    private IEnumerator ShowReturnButtonAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (returnButton != null)
            returnButton.gameObject.SetActive(true);
    }

    public void SetInventoryData(InventoryData inv)
    {
        if (inv == null)
        {
            DebugLogger.LogWarning("Attempted to set null InventoryData in ResultScreen.");
            return;
        }
        inventoryData = inv;
    }
}