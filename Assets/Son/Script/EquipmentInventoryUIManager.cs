using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using static ItemData;

public class EquipmentInventoryUIManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private InventoryData inventoryData;
    [SerializeField] private TeamSetupManager teamSetupManager;
    private List<CombatantData> availableCharacters;

    [Header("Inventory Grid")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button backButton;
    [SerializeField] private InventoryUIManager inventoryUI;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject slotItemPrefab;
    [SerializeField] private int rows = 4;
    [SerializeField] private int columns = 3;

    [Header("Equip Popup (New UI)")]
    [SerializeField] private GameObject equipPanel;
    [SerializeField] private TMP_Dropdown characterDropdown;
    [SerializeField] private Image selectedCharacterAvatar;
    [SerializeField] private Transform equippedSlotsContainer;
    [SerializeField] private GameObject equipSlotPrefab;
    [SerializeField] private Button confirmEquipButton;
    [SerializeField] private Button closeEquipPanelButton;
    [SerializeField] private Image equipStatsBackground;
    [SerializeField] private TextMeshProUGUI equipStatsText;

    [Header("Delete Button")]
    [SerializeField] private Button deleteButton;

    private readonly EquipmentSlot[] slotOrder = new[] { EquipmentSlot.Gloves, EquipmentSlot.Armor, EquipmentSlot.Pants, EquipmentSlot.Weapon };

    private List<Image> slotImages = new();
    private List<TextMeshProUGUI> slotTexts = new();
    private List<InventoryData.ItemEntry> equipmentEntries = new();

    private EquipmentData pendingEquip;
    private CombatantData selectedChar;
    private List<CombatantData> filteredCharactersForPending = new();

    private class EquipSlotUI
    {
        public GameObject root;
        public Image icon;
        public TextMeshProUGUI label;
        public Button rootButton;
        public Button removeButton;
        public GameObject highlight;
        public EquipmentSlot slot;
    }
    private readonly List<EquipSlotUI> equipSlotUIs = new();

    private void Awake()
    {
        UpdatePendingEquipStatsDisplay();
    }

    private void OnEnable()
    {
        if (inventoryData != null) inventoryData.OnInventoryChanged += RefreshUI;
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged += OnEquipmentChanged;
    }

    private void OnDisable()
    {
        if (inventoryData != null) inventoryData.OnInventoryChanged -= RefreshUI;
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged -= OnEquipmentChanged;
    }

    private void Start()
    {
        Debug.Log($"Khởi tạo EquipmentInventoryUIManager. teamSetupManager: {(teamSetupManager != null ? teamSetupManager.name : "null")}");
        if (teamSetupManager == null)
        {
            Debug.LogError("teamSetupManager không được gán trong EquipmentInventoryUIManager! Vui lòng gán trong Inspector.");
            availableCharacters = new List<CombatantData>();
        }
        else if (teamSetupManager.AvailableCharacters == null)
        {
            Debug.LogWarning("teamSetupManager.AvailableCharacters là null! Khởi tạo availableCharacters thành danh sách rỗng.");
            availableCharacters = new List<CombatantData>();
        }
        else
        {
            availableCharacters = teamSetupManager.AvailableCharacters;
            Debug.Log($"availableCharacters được khởi tạo với {availableCharacters.Count} nhân vật.");
        }

        BuildGrid();
        BuildEquipSlots();
        RefreshUI();
        BuildCharacterDropdown();

        if (closeEquipPanelButton != null)
            closeEquipPanelButton.onClick.AddListener(() =>
            {
                pendingEquip = null;
                selectedChar = null;
                UpdateEquipSlotsDisplay();
            });

        if (backButton != null)
            backButton.onClick.AddListener(SwitchBack);

        if (confirmEquipButton != null)
        {
            confirmEquipButton.onClick.RemoveAllListeners();
            confirmEquipButton.onClick.AddListener(DoEquip);
        }

        if (deleteButton != null)
            deleteButton.gameObject.SetActive(false);

        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged += OnEquipmentChanged;
    }

    public void ShowPanel()
    {
        Debug.Log($"ShowPanel được gọi trong EquipmentInventoryUIManager. panel active: {(panel != null ? panel.activeSelf.ToString() : "null")}, equipPanel active: {(equipPanel != null ? equipPanel.activeSelf.ToString() : "null")}");
        if (panel == null)
        {
            Debug.LogError("Main panel không được gán! Vui lòng gán trong Inspector.");
            return;
        }

        panel.SetActive(true);
        if (equipPanel == null)
        {
            Debug.LogError("equipPanel không được gán! Vui lòng gán trong Inspector.");
            return;
        }

        equipPanel.SetActive(true);
        Debug.Log($"Kích hoạt equipPanel: {equipPanel.name}, trạng thái trước đó: {equipPanel.activeSelf}");

        try
        {
            RefreshAvailableCharacters();
            BuildCharacterDropdown();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Lỗi trong BuildCharacterDropdown: {ex.Message}");
        }

        RefreshUI();
        UpdateEquipSlotsDisplay();
        UpdatePendingEquipStatsDisplay();
    }

    private void RefreshAvailableCharacters()
    {
        if (teamSetupManager == null)
        {
            Debug.LogError("teamSetupManager không được gán trong RefreshAvailableCharacters!");
            availableCharacters = new List<CombatantData>();
        }
        else if (teamSetupManager.AvailableCharacters == null)
        {
            Debug.LogWarning("teamSetupManager.AvailableCharacters là null trong RefreshAvailableCharacters!");
            availableCharacters = new List<CombatantData>();
        }
        else
        {
            availableCharacters = teamSetupManager.AvailableCharacters;
            Debug.Log($"Làm mới availableCharacters với {availableCharacters.Count} nhân vật.");
        }
    }

    private void BuildCharacterDropdown()
    {
        if (characterDropdown == null)
        {
            Debug.LogError("characterDropdown không được gán trong BuildCharacterDropdown!");
            return;
        }

        characterDropdown.onValueChanged.RemoveAllListeners();
        characterDropdown.ClearOptions();

        if (availableCharacters == null)
        {
            Debug.LogError("availableCharacters là null trong BuildCharacterDropdown! Khởi tạo danh sách rỗng.");
            availableCharacters = new List<CombatantData>();
        }

        var options = availableCharacters
            .Select(c => new TMP_Dropdown.OptionData($"{c.Name} (Lv {c.Level})"))
            .ToList();

        if (options.Count == 0)
        {
            Debug.LogWarning("Không có nhân vật nào trong danh sách dropdown!");
            characterDropdown.AddOptions(new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("Không có nhân vật") });
            characterDropdown.interactable = false;
            if (selectedCharacterAvatar != null) selectedCharacterAvatar.sprite = null;
            if (confirmEquipButton != null) confirmEquipButton.interactable = false;
            return;
        }

        characterDropdown.AddOptions(options);
        characterDropdown.interactable = true;

        characterDropdown.onValueChanged.AddListener((i) =>
        {
            if (i >= 0 && i < availableCharacters.Count)
                OnCharacterSelected(availableCharacters[i]);
            else
                OnCharacterSelected(null);
        });

        characterDropdown.value = 0;
        characterDropdown.RefreshShownValue();
        if (availableCharacters.Count > 0) OnCharacterSelected(availableCharacters[0]);
    }

    public void HidePanel()
    {
        if (panel != null) panel.SetActive(false);
        if (equipPanel != null)
        {
            Debug.Log("Tắt equipPanel trong HidePanel");
            equipPanel.SetActive(false);
        }

        if (deleteButton != null) deleteButton.gameObject.SetActive(false);

        pendingEquip = null;
        selectedChar = null;
        UpdatePendingEquipStatsDisplay();
    }

    private void OnEquipmentChanged(string characterId)
    {
        if (selectedChar != null && selectedChar.CharacterId == characterId)
            UpdateEquipSlotsDisplay();

        RefreshUI();
    }

    private void SwitchBack()
    {
        Debug.Log($"SwitchBack được gọi. panel active: {(panel != null ? panel.activeSelf.ToString() : "null")}, equipPanel active: {(equipPanel != null ? equipPanel.activeSelf.ToString() : "null")}");
        if (panel != null) panel.SetActive(false);
        if (equipPanel != null) equipPanel.SetActive(false);
        if (deleteButton != null) deleteButton.gameObject.SetActive(false);

        pendingEquip = null;
        selectedChar = null;

        if (inventoryUI != null)
        {
            inventoryUI.ShowInventoryPanel();
        }
        else
        {
            Debug.LogError("inventoryUI không được gán! Không thể chuyển về Inventory UI.");
        }
    }

    private void BuildGrid()
    {
        if (slotsContainer == null || slotItemPrefab == null) return;

        foreach (Transform child in slotsContainer) Destroy(child.gameObject);
        slotImages.Clear();
        slotTexts.Clear();

        int total = rows * columns;

        var grid = slotsContainer.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (grid != null)
        {
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
        }

        for (int i = 0; i < total; i++)
        {
            var go = Instantiate(slotItemPrefab, slotsContainer);
            var img = go.GetComponent<Image>();
            var txt = go.GetComponentInChildren<TextMeshProUGUI>();
            var btn = go.GetComponent<Button>();

            slotImages.Add(img);
            slotTexts.Add(txt);

            int index = i;
            if (btn != null) btn.onClick.AddListener(() => OnSlotClicked(index));
        }
    }

    private void BuildEquipSlots()
    {
        if (equippedSlotsContainer == null || equipSlotPrefab == null) return;

        for (int i = equippedSlotsContainer.childCount - 1; i >= 0; i--)
            Destroy(equippedSlotsContainer.GetChild(i).gameObject);
        equipSlotUIs.Clear();

        for (int i = 0; i < slotOrder.Length; i++)
        {
            var go = Instantiate(equipSlotPrefab, equippedSlotsContainer);

            var iconTf = go.transform.Find("Icon");
            Image img = iconTf != null ? iconTf.GetComponent<Image>() : go.GetComponentInChildren<Image>();

            var labelTf = go.transform.Find("Label");
            TextMeshProUGUI txt = labelTf != null ? labelTf.GetComponent<TextMeshProUGUI>() : go.GetComponentInChildren<TextMeshProUGUI>();

            var removeBtnTf = go.transform.Find("RemoveButton");
            Button removeBtn = removeBtnTf != null ? removeBtnTf.GetComponent<Button>() : null;

            var rootBtn = go.GetComponent<Button>();
            var highlightTf = go.transform.Find("Highlight");
            GameObject highlightGO = highlightTf != null ? highlightTf.gameObject : null;

            var ui = new EquipSlotUI
            {
                root = go,
                icon = img,
                label = txt,
                rootButton = rootBtn,
                removeButton = removeBtn,
                highlight = highlightGO,
                slot = slotOrder[i]
            };

            if (ui.label != null) ui.label.text = slotOrder[i].ToString();

            if (ui.icon != null) ui.icon.sprite = null;
            if (ui.removeButton != null) { ui.removeButton.onClick.RemoveAllListeners(); ui.removeButton.gameObject.SetActive(false); }
            if (ui.highlight != null) ui.highlight.SetActive(false);

            if (ui.removeButton != null)
            {
                var capturedSlot = ui.slot;
                ui.removeButton.onClick.AddListener(() =>
                {
                    if (selectedChar == null) return;
                    EquipmentManager.Instance?.Unequip(selectedChar, capturedSlot);
                    UpdateEquipSlotsDisplay();
                });
            }

            if (ui.rootButton != null)
            {
                ui.rootButton.onClick.RemoveAllListeners();
                ui.rootButton.onClick.AddListener(() =>
                {
                    bool has = ui.icon != null && ui.icon.sprite != null;
                    if (ui.removeButton != null) ui.removeButton.gameObject.SetActive(has);
                });
            }

            equipSlotUIs.Add(ui);
        }
    }

    private void RefreshUI()
    {
        for (int i = 0; i < slotImages.Count; i++)
        {
            if (slotImages[i] != null) slotImages[i].sprite = null;
            if (slotTexts[i] != null) slotTexts[i].text = "";
        }

        if (inventoryData == null) return;

        equipmentEntries = inventoryData.Items
            .Where(e => e != null && e.Item is EquipmentData && e.Quantity > 0)
            .OrderBy(e => e.Item.ItemName)
            .ToList();

        for (int i = 0; i < equipmentEntries.Count && i < slotImages.Count; i++)
        {
            var entry = equipmentEntries[i];
            var eq = (EquipmentData)entry.Item;

            if (slotImages[i] != null) slotImages[i].sprite = eq.ItemSprite;
            if (slotTexts[i] != null) slotTexts[i].text = $" x{entry.Quantity}";
        }
    }

    private void OnSlotClicked(int index)
    {
        if (index < 0 || index >= equipmentEntries.Count)
        {
            Debug.LogWarning($"Chỉ số slot không hợp lệ: {index}");
            return;
        }

        pendingEquip = (EquipmentData)equipmentEntries[index].Item;
        filteredCharactersForPending.Clear();

        if (equipPanel != null)
        {
            Debug.Log($"Kích hoạt equipPanel trong OnSlotClicked cho chỉ số: {index}");
            equipPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("equipPanel là null trong OnSlotClicked!");
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            int slotIndex = equipmentEntries[index].SlotIndex;
            deleteButton.onClick.AddListener(() =>
            {
                inventoryData.RemoveItem(slotIndex);
                deleteButton.gameObject.SetActive(false);
                RefreshUI();
            });

            deleteButton.gameObject.SetActive(true);
        }

        if (pendingEquip != null)
        {
            if (EquipmentManager.Instance != null)
            {
                filteredCharactersForPending = availableCharacters
                    .Where(c => EquipmentManager.Instance.IsCompatible(c, pendingEquip))
                    .ToList();
            }
            else
            {
                filteredCharactersForPending = new List<CombatantData>(availableCharacters);
            }

            if (filteredCharactersForPending.Count == 0)
            {
                if (selectedCharacterAvatar != null) selectedCharacterAvatar.sprite = null;
                if (confirmEquipButton != null) confirmEquipButton.interactable = false;
                foreach (var eui in equipSlotUIs)
                {
                    if (eui.icon != null) eui.icon.sprite = null;
                    if (eui.removeButton != null) eui.removeButton.gameObject.SetActive(false);
                    if (eui.highlight != null) eui.highlight.SetActive(false);
                }
            }
            else
            {
                CombatantData targetChar = selectedChar;
                if (targetChar == null || !filteredCharactersForPending.Contains(targetChar))
                {
                    targetChar = filteredCharactersForPending[0];
                }

                int idx = availableCharacters.IndexOf(targetChar);
                if (idx >= 0 && characterDropdown != null)
                {
                    characterDropdown.onValueChanged.RemoveAllListeners();
                    characterDropdown.value = idx;
                    characterDropdown.RefreshShownValue();

                    characterDropdown.onValueChanged.AddListener((i) =>
                    {
                        if (i >= 0 && i < availableCharacters.Count)
                            OnCharacterSelected(availableCharacters[i]);
                        else
                            OnCharacterSelected(null);
                    });

                    OnCharacterSelected(targetChar);
                }
                else
                {
                    OnCharacterSelected(targetChar);
                }
            }
        }
        else
        {
            if (selectedCharacterAvatar != null) selectedCharacterAvatar.sprite = null;
            if (confirmEquipButton != null) confirmEquipButton.interactable = false;
            foreach (var eui in equipSlotUIs)
            {
                if (eui.icon != null) eui.icon.sprite = null;
                if (eui.removeButton != null) eui.removeButton.gameObject.SetActive(false);
                if (eui.highlight != null) eui.highlight.SetActive(false);
            }
        }

        UpdateEquipSlotsHighlight();
        UpdatePendingEquipStatsDisplay();
    }

    private void OnCharacterSelected(CombatantData c)
    {
        selectedChar = c;
        if (selectedChar == null)
        {
            if (selectedCharacterAvatar != null) selectedCharacterAvatar.sprite = null;
            if (confirmEquipButton != null) confirmEquipButton.interactable = false;
            UpdateEquipSlotsDisplay();
            return;
        }

        if (selectedCharacterAvatar != null) selectedCharacterAvatar.sprite = selectedChar.AvatarSprite;

        bool canEquip = pendingEquip != null && EquipmentManager.Instance != null && EquipmentManager.Instance.IsCompatible(selectedChar, pendingEquip);
        if (confirmEquipButton != null) confirmEquipButton.interactable = canEquip;

        UpdateEquipSlotsDisplay();
        UpdateEquipSlotsHighlight();
        UpdatePendingEquipStatsDisplay();
    }

    private void UpdateEquipSlotsDisplay()
    {
        foreach (var ui in equipSlotUIs)
        {
            if (ui.icon != null) ui.icon.sprite = null;
            if (ui.removeButton != null) ui.removeButton.gameObject.SetActive(false);
            if (ui.label != null) ui.label.text = ui.slot.ToString();
            if (ui.highlight != null) ui.highlight.SetActive(false);
        }

        if (selectedChar == null || EquipmentManager.Instance == null) return;

        var set = EquipmentManager.Instance.GetEquipped(selectedChar);
        if (set == null) return;

        var allEquips = EquipmentManager.Instance.GetAllEquips();

        foreach (var ui in equipSlotUIs)
        {
            string id = null;
            switch (ui.slot)
            {
                case EquipmentSlot.Gloves: id = set.GlovesId; break;
                case EquipmentSlot.Armor: id = set.ArmorId; break;
                case EquipmentSlot.Pants: id = set.PantsId; break;
                case EquipmentSlot.Weapon: id = set.WeaponId; break;
            }
            if (!string.IsNullOrEmpty(id))
            {
                var eqData = allEquips.Find(e => e != null && e.Id == id);
                if (eqData != null)
                {
                    if (ui.icon != null) ui.icon.sprite = eqData.ItemSprite;
                    if (ui.removeButton != null) ui.removeButton.gameObject.SetActive(true);
                }
            }
        }

        UpdateEquipSlotsHighlight();
    }

    private void UpdateEquipSlotsHighlight()
    {
        foreach (var ui in equipSlotUIs)
        {
            if (ui.highlight == null) continue;
            bool should = (pendingEquip != null && pendingEquip.Slot == ui.slot);
            ui.highlight.SetActive(should);
        }
    }

    private void DoEquip()
    {
        if (pendingEquip == null || selectedChar == null) return;
        if (EquipmentManager.Instance == null)
        {
            Debug.LogError("EquipmentManager.Instance null khi Equip!");
            return;
        }

        bool ok = EquipmentManager.Instance.Equip(selectedChar, pendingEquip);
        if (ok)
        {
            RefreshUI();
            UpdateEquipSlotsDisplay();
        }

        pendingEquip = null;

        int currentIndex = availableCharacters.IndexOf(selectedChar);
        BuildCharacterDropdown();
        UpdatePendingEquipStatsDisplay();

        if (currentIndex >= 0 && characterDropdown != null && currentIndex < availableCharacters.Count)
        {
            characterDropdown.value = currentIndex;
            characterDropdown.RefreshShownValue();
            OnCharacterSelected(selectedChar);
        }

        if (deleteButton != null) deleteButton.gameObject.SetActive(false);
    }

    private void UpdatePendingEquipStatsDisplay()
    {
        if (equipStatsText == null)
            return;

        bool hasEquip = pendingEquip != null;

        equipStatsText.gameObject.SetActive(hasEquip);
        if (equipStatsBackground != null)
            equipStatsBackground.gameObject.SetActive(hasEquip);

        if (!hasEquip) return;

        string rarityText = pendingEquip.ItemRarity.ToString();
        string slotText = pendingEquip.Slot.ToString();

        List<string> lines = new List<string>();

        lines.Add($"<b>{pendingEquip.ItemName}</b>");
        lines.Add($"Type: {slotText}");
        lines.Add($"Rarity: {rarityText}");

        if (pendingEquip.AttackBonus != 0) lines.Add($"ATK +{pendingEquip.AttackBonus}");
        if (pendingEquip.DefBonus != 0) lines.Add($"DEF +{pendingEquip.DefBonus}");
        if (pendingEquip.HpBonus != 0) lines.Add($"HP +{pendingEquip.HpBonus}");
        if (pendingEquip.SpdBonus != 0) lines.Add($"SPD +{pendingEquip.SpdBonus}");
        if (pendingEquip.AgilityBonus != 0) lines.Add($"AGI +{pendingEquip.AgilityBonus}");
        if (pendingEquip.CritRateBonus != 0f) lines.Add($"CR +{Mathf.RoundToInt(pendingEquip.CritRateBonus * 100f)}%");
        if (pendingEquip.CritDMGBonus != 0f) lines.Add($"CD +{Mathf.RoundToInt(pendingEquip.CritDMGBonus * 100f)}%");
        if (pendingEquip.BonusDMG != 0f) lines.Add($"BDMG +{Mathf.RoundToInt(pendingEquip.BonusDMG * 100f)}%");
        if (pendingEquip.RES != 0) lines.Add($"RES +{pendingEquip.RES}");

        equipStatsText.text = string.Join("\n", lines);
    }
}