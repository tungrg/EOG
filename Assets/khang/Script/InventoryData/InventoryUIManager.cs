using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using NUnit.Framework.Constraints;

public class InventoryUIManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private InventoryData inventoryData;
    [SerializeField] private TeamSetupManager teamSetupManager;
    private List<CombatantData> availableCharacters;

    [Header("Inventory UI (Stone)")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Button openInventoryButton;
    [SerializeField] private Button closeInventoryButton;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject slotItemPrefab;
    [SerializeField] private int rows = 4;
    [SerializeField] private int columns = 3;
    [SerializeField] private Button sortButton;

    [Header("Upgrade UI")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private TextMeshProUGUI upgradeInfoText;
    [SerializeField] private TMP_Dropdown characterDropdown;
    [SerializeField] private Button confirmUpgradeButton;
    [SerializeField] private Image characterAvatarImage;
    [SerializeField] private Image stoneImage;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button mergeButton;

    [Header("Switch to Equipment UI")]
    [SerializeField] private EquipmentInventoryUIManager equipmentUI;
    [SerializeField] private Button switchButton;

    private List<Image> slotImages = new();
    private List<TextMeshProUGUI> slotTexts = new();
    private List<Sprite> defaultSlotSprites = new(); // ✅ thêm để lưu sprite mặc định

    private StoneData selectedStone;
    private int selectedSlotIndex = -1;
    private bool showingStoneInventory = true;

    public bool IsInitialized { get; private set; }

    void Start()
    {
        if (!ValidateComponents())
        {
            Debug.LogError("[InventoryUIManager] Missing components");
            return;
        }

        upgradePanel.SetActive(false);
        deleteButton.gameObject.SetActive(false);
        mergeButton.gameObject.SetActive(false);

        openInventoryButton.onClick.AddListener(ShowInventoryPanel);
        if (closeInventoryButton != null)
            closeInventoryButton.onClick.AddListener(HideInventoryPanel);
        if (sortButton != null)
            sortButton.onClick.AddListener(() =>
            {
                inventoryData.SortItems();
                UpdateInventoryUI();
            });
        if (switchButton != null)
        {
            Debug.Log("Assigning switchButton onClick event");
            switchButton.onClick.RemoveAllListeners();
            switchButton.onClick.AddListener(SwitchInventory);
        }
        else
        {
            Debug.LogError("switchButton is null in InventoryUIManager!");
        }

        availableCharacters = teamSetupManager != null && teamSetupManager.AvailableCharacters != null
            ? teamSetupManager.AvailableCharacters
            : new List<CombatantData>();

        inventoryData.OnInventoryChanged -= UpdateInventoryUI;
        inventoryData.OnInventoryChanged += UpdateInventoryUI;
        InitializeInventoryUI();
    }

    private bool ValidateComponents()
    {
        bool isValid = inventoryData != null &&
                       inventoryPanel != null &&
                       openInventoryButton != null &&
                       slotsContainer != null &&
                       slotItemPrefab != null &&
                       upgradePanel != null &&
                       upgradeInfoText != null &&
                       characterDropdown != null &&
                       confirmUpgradeButton != null &&
                       teamSetupManager != null &&
                       characterAvatarImage != null &&
                       stoneImage != null &&
                       deleteButton != null &&
                       mergeButton != null &&
                       sortButton != null &&
                       switchButton != null &&
                       equipmentUI != null;

        if (!isValid)
        {
            Debug.LogError("Missing components in InventoryUIManager. Check Inspector assignments.");
        }
        return isValid;
    }

    private void InitializeInventoryUI()
    {
        if (slotImages.Count > 0) return;

        slotImages.Clear();
        slotTexts.Clear();
        defaultSlotSprites.Clear(); // ✅ đảm bảo danh sách rỗng trước khi thêm

        for (int i = 0; i < rows * columns; i++)
        {
            GameObject slotItem = Instantiate(slotItemPrefab, slotsContainer);
            Image slotImage = slotItem.GetComponent<Image>();
            TextMeshProUGUI slotText = slotItem.GetComponentInChildren<TextMeshProUGUI>();
            Button slotButton = slotItem.GetComponent<Button>();

            if (slotImage != null) slotImage.sprite = slotImage.sprite; // giữ sprite hiện tại
            if (slotText != null) slotText.text = "";

            slotImages.Add(slotImage);
            slotTexts.Add(slotText);
            defaultSlotSprites.Add(slotImage != null ? slotImage.sprite : null); // ✅ lưu sprite mặc định

            if (slotButton != null)
            {
                int index = i;
                slotButton.onClick.AddListener(() => OnSlotClicked(index));
            }
        }

        UpdateInventoryUI();
        IsInitialized = true;
    }

    private void OnSlotClicked(int slotIndex)
    {
        selectedSlotIndex = slotIndex;
        var itemEntry = inventoryData.Items.Find(i => i.SlotIndex == slotIndex);

        deleteButton.gameObject.SetActive(false);
        mergeButton.gameObject.SetActive(false);

        if (itemEntry == null || itemEntry.Item == null) return;

        if (itemEntry.Item is StoneData stone)
        {
            selectedStone = stone;

            deleteButton.transform.SetParent(slotsContainer.GetChild(slotIndex));
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() =>
            {
                inventoryData.RemoveItem(slotIndex);
                deleteButton.gameObject.SetActive(false);
                mergeButton.gameObject.SetActive(false);
            });
            deleteButton.gameObject.SetActive(true);

            mergeButton.transform.SetParent(slotsContainer.GetChild(slotIndex));
            int availableLowStones = inventoryData.Items
                .Where(i => i.Item is StoneData s && s.Element == stone.Element && s.Grade == stone.Grade)
                .Sum(i => i.Quantity);

            mergeButton.GetComponentInChildren<TextMeshProUGUI>().text = $"Merge (x{availableLowStones}/10)";
            mergeButton.interactable = (stone.Grade < 5 && availableLowStones >= 10);
            mergeButton.onClick.RemoveAllListeners();
            mergeButton.onClick.AddListener(() =>
            {
                if (inventoryData.MergeStones(stone.Element, stone.Grade))
                {
                    UpdateInventoryUI();
                    deleteButton.gameObject.SetActive(false);
                    mergeButton.gameObject.SetActive(false);
                }
            });
            mergeButton.gameObject.SetActive(true);

            ShowUpgradePanel();
        }
    }

    private void ShowUpgradePanel()
    {
        upgradePanel.SetActive(true);
        characterDropdown.ClearOptions();

        var options = availableCharacters
            .Where(c => c.Element == selectedStone.Element)
            .Select(c => new TMP_Dropdown.OptionData($"{c.Name} (Lv {c.Level})"))
            .ToList();

        characterDropdown.AddOptions(options);

        if (options.Count > 0) UpdateUpgradeInfo(options[0].text);
        else
        {
            upgradeInfoText.text = "No compatible characters available.";
            characterAvatarImage.sprite = null;
            stoneImage.sprite = null;
            confirmUpgradeButton.interactable = false;
        }

        confirmUpgradeButton.onClick.RemoveAllListeners();
        confirmUpgradeButton.onClick.AddListener(ConfirmUpgrade);

        characterDropdown.onValueChanged.RemoveAllListeners();
        characterDropdown.onValueChanged.AddListener(delegate
        {
            UpdateUpgradeInfo(characterDropdown.options[characterDropdown.value].text);
        });
    }

    private void UpdateUpgradeInfo(string charName)
    {
        var character = availableCharacters.Find(c => c.Name == charName.Split(" (Lv")[0]);
        if (character == null)
        {
            upgradeInfoText.text = "Character not found.";
            characterAvatarImage.sprite = null;
            stoneImage.sprite = null;
            confirmUpgradeButton.interactable = false;
            return;
        }

        characterAvatarImage.sprite = character.AvatarSprite;

        var progress = CharacterProgressManager.Instance.GetProgress(character);
        int currentLevel = progress?.Level ?? 1;
        var (grade, quantity) = CharacterProgressManager.Instance.GetUpgradeRequirement(currentLevel);

        if (grade == 0)
        {
            upgradeInfoText.text = $"{charName} is at max level.";
            stoneImage.sprite = null;
            confirmUpgradeButton.interactable = false;
            return;
        }

        int availableQuantity = inventoryData.Items
            .Where(i => i.Item is StoneData s && s.Element == character.Element && s.Grade == grade)
            .Sum(i => i.Quantity);

        StoneData requiredStone = inventoryData.Items
            .Select(i => i.Item as StoneData)
            .FirstOrDefault(s => s != null && s.Element == character.Element && s.Grade == grade);

        upgradeInfoText.text =
            $"Upgrade {character.Name} to level {currentLevel + 1}: " +
            $"Requires {quantity} grade {grade} stones ({character.Element}) | Available: {availableQuantity}";

        stoneImage.sprite = requiredStone != null ? requiredStone.ItemSprite : null;
        confirmUpgradeButton.interactable = availableQuantity >= quantity;
    }

    private void ConfirmUpgrade()
    {
        if (characterDropdown.options.Count == 0) return;

        var charName = characterDropdown.options[characterDropdown.value].text.Split(" (Lv")[0];
        var character = availableCharacters.Find(c => c.Name == charName);
        if (character != null)
        {
            CharacterProgressManager.Instance.UpgradeCharacter(character);
            UpdateInventoryUI();
            upgradePanel.SetActive(false);
            characterAvatarImage.sprite = null;
            stoneImage.sprite = null;
            deleteButton.gameObject.SetActive(false);
            mergeButton.gameObject.SetActive(false);
        }
    }

    public void ShowInventoryPanel()
    {
        Debug.Log("Showing Inventory Panel (Stone)");
        UILayer.Instance.ShowPanel(inventoryPanel);
        UpdateInventoryUI();
        deleteButton.gameObject.SetActive(false);
        mergeButton.gameObject.SetActive(false);
        GameObject InventoryLayout = GameObject.Find("InventoryUIManager/Canvas/InventoryLayout");
        if (InventoryLayout == null)
        {
            DebugLogger.LogError("LayoutDungeon GameObject not found in the scene.");
        }
        else
        {
            InventoryLayout.SetActive(true);
        }
    }

    public void UpdateInventoryUI()
    {
        for (int i = 0; i < slotImages.Count; i++)
        {
            if (slotImages[i] == null || slotTexts[i] == null) continue;

            // ✅ Giữ lại hình mặc định thay vì None
            slotImages[i].sprite = defaultSlotSprites[i];
            slotTexts[i].text = "";
        }

        foreach (var itemEntry in inventoryData.Items)
        {
            if (itemEntry == null || itemEntry.Item == null) continue;
            if (itemEntry.Item is not StoneData) continue;

            int slotIndex = itemEntry.SlotIndex;
            if (slotIndex >= 0 && slotIndex < slotImages.Count && slotImages[slotIndex] != null)
            {
                slotImages[slotIndex].sprite = itemEntry.Item.ItemSprite;
                slotTexts[slotIndex].text = $"x{itemEntry.Quantity}";
            }
        }
    }

    public void HideInventoryPanel()
    {
        Debug.Log("Hiding Inventory Panel (Stone)");
        UILayer.Instance.HideAllPanels();
        upgradePanel.SetActive(false);
        characterAvatarImage.sprite = null;
        stoneImage.sprite = null;
        deleteButton.gameObject.SetActive(false);
        mergeButton.gameObject.SetActive(false);
        GameObject InventoryLayout = GameObject.Find("InventoryUIManager/Canvas/InventoryLayout");
        if (InventoryLayout == null)
        {
            DebugLogger.LogError("LayoutDungeon GameObject not found in the scene.");
        }
        else
        {
            InventoryLayout.SetActive(false);
        }
    }

    private void SwitchInventory()
    {
        bool isStoneActive = inventoryPanel.activeSelf;

        if (isStoneActive)
        {
            if (equipmentUI != null)
            {
                Debug.Log("Switching to Equipment UI");
                UILayer.Instance.ShowPanel(equipmentUI.gameObject);
                GameObject InventoryLayout = GameObject.Find("InventoryUIManager/Canvas/InventoryLayout");
                if (InventoryLayout == null)
                {
                    DebugLogger.LogError("LayoutDungeon GameObject not found in the scene.");
                }
                else
                {
                    InventoryLayout.SetActive(false);
                }
                equipmentUI.ShowPanel();
            }
            else
            {
                Debug.LogError("equipmentUI is null! Cannot switch to Equipment UI.");
            }
        }
        else
        {
            Debug.Log("Switching to Stone Inventory UI");
            UILayer.Instance.ShowPanel(inventoryPanel);
        }
    }

    void Update()
    {
        if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer)
        {
            if (inventoryPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                HideInventoryPanel();
            }
        }
    }
}
