using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class TeamSaveData
{
    public string[] characterIds; // Lưu danh sách CharacterId của các nhân vật
}

public class TeamSetupManager : MonoBehaviour
{
    [SerializeField] private TeamData teamData;
    [SerializeField] private List<CombatantData> availableCharacters;
    public List<CombatantData> AvailableCharacters => availableCharacters;
    [SerializeField] private UnityEngine.UI.Button[] teamOptionButtons; // 3 buttons for 3 teams
    [SerializeField] private UnityEngine.UI.Button[] characterSlots; // 4 slots
    [SerializeField] private TextMeshProUGUI[] slotTexts;
    [SerializeField] private UnityEngine.UI.Image[] slotImages;
    [SerializeField] private UnityEngine.UI.Button saveButton;
    [SerializeField] private UnityEngine.UI.Button quickSelectButton;
    [SerializeField] private UnityEngine.UI.Button clearButton;
    [SerializeField] private UnityEngine.UI.Button backButton; // Button to return to Map
    [SerializeField] private UnityEngine.UI.Button confirmButton; // Button to confirm character selection
    [SerializeField] private UnityEngine.UI.Button escapeButton; // Button to close character selection panel
    [SerializeField] private GameObject teamSetupPanel; // Reference to the parent panel
    [SerializeField] private GameObject characterSelectionPanel;
    [SerializeField] private GameObject warningPanel; // Panel for warning message
    [SerializeField] private TextMeshProUGUI warningText; // Text for warning message
    [SerializeField] private UnityEngine.UI.Button warningCloseButton; // Button to close warning panel
    [SerializeField] private TextMeshProUGUI characterInfoText;
    [SerializeField] private Transform scrollViewContent; // Use Transform for ScrollView content
    [SerializeField] private GameObject buttonAvatarPrefab;
    // Fields for skill info panel
    [SerializeField] private GameObject skillInfoPanel;
    [SerializeField] private Transform skillScrollViewContent; // ScrollView content for skills
    [SerializeField] private GameObject skillButtonPrefab; // Prefab for skill display
    [SerializeField] private TutorialOverlay tutorialOverlay;


    private List<CombatantData>[] savedTeams = new List<CombatantData>[3];
    private int currentTeamIndex = 0;
    private CombatantData selectedCharacter;
    private List<CombatantData> selectedCharactersInTeam = new List<CombatantData>(); // Track selected characters
    private int currentSlotIndex = -1; // Track the slot being edited

    void Start()
    {
        if (teamData == null || availableCharacters == null || teamOptionButtons == null || characterSlots == null ||
            slotTexts == null || slotImages == null || saveButton == null || quickSelectButton == null ||
            clearButton == null || backButton == null || confirmButton == null || teamSetupPanel == null ||
            characterSelectionPanel == null || characterInfoText == null ||
            scrollViewContent == null || buttonAvatarPrefab == null || escapeButton == null ||
            warningPanel == null || warningText == null || warningCloseButton == null ||
            skillInfoPanel == null || skillScrollViewContent == null || skillButtonPrefab == null)
        {
            DebugLogger.LogError("TeamSetupManager components not assigned. Check Inspector for missing references.");
            return;
        }

        // Khởi tạo savedTeams
        for (int i = 0; i < savedTeams.Length; i++)
        {
            savedTeams[i] = new List<CombatantData>();
        }

        // Tải các đội hình từ PlayerPrefs
        LoadTeamsFromPlayerPrefs();

        SetupUI();
        characterSelectionPanel.SetActive(false);
        warningPanel.SetActive(false);
        skillInfoPanel.SetActive(false); // Initialize skill panel as hidden
        saveButton.interactable = true;

        // Hiển thị teamSetupPanel nếu đội hình chưa đầy
        if (!teamData.IsTeamFull())
        {
            teamSetupPanel.SetActive(true);
            DebugLogger.Log($"Team is not full, showing teamSetupPanel. BattleType: {teamData.CurrentBattleType}");
        }


        bool isFirstTime = PlayerPrefs.GetInt("FirstTimeTeamSetup", 1) == 1;
        if (isFirstTime && tutorialOverlay != null)
        {
            StartCoroutine(RunFirstTimeTutorial());
        }

        else
        {
            teamSetupPanel.SetActive(false);
            DebugLogger.Log($"Team is full, hiding teamSetupPanel. BattleType: {teamData.CurrentBattleType}");
        }
    }

    void Update()
    {
        // Handle Esc key to close panels
        if (skillInfoPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            DebugLogger.Log("Closing skillInfoPanel via Escape key.");
            CloseSkillInfoPanel();
        }
        else if (characterSelectionPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            DebugLogger.Log("Closing characterSelectionPanel via Escape key.");
            CloseCharacterSelection();
        }
    }

    private void SetupUI()
    {
        for (int i = 0; i < teamOptionButtons.Length; i++)
        {
            int index = i;
            teamOptionButtons[i].onClick.AddListener(() => SelectTeam(index));
        }

        for (int i = 0; i < characterSlots.Length; i++)
        {
            int index = i;
            characterSlots[i].onClick.AddListener(() => OpenCharacterSelection(index));
        }

        saveButton.onClick.AddListener(SaveTeam);
        quickSelectButton.onClick.AddListener(QuickSelectTeam);
        clearButton.onClick.AddListener(ClearTeam);
        backButton.onClick.AddListener(ReturnToMap);
        confirmButton.onClick.AddListener(ConfirmCharacterSelection);
        escapeButton.onClick.AddListener(CloseCharacterSelection);
        warningCloseButton.onClick.AddListener(() => warningPanel.SetActive(false));

        UpdateSlotUI();
        PopulateScrollView();
    }

    public void ShowTeamSetupPanel()
    {
        if (teamSetupPanel != null)
        {
            teamSetupPanel.SetActive(true);
            DebugLogger.Log($"ShowTeamSetupPanel called. BattleType: {teamData.CurrentBattleType}, Team Size: {teamData.SelectedCombatants.Count}");
            UpdateSlotUI();
            PopulateScrollView();
        }
        else
        {
            DebugLogger.LogError("teamSetupPanel is null in ShowTeamSetupPanel.");
        }
    }

    private void SelectTeam(int teamIndex)
    {
        if (currentTeamIndex >= 0 && currentTeamIndex < savedTeams.Length)
        {
            savedTeams[currentTeamIndex] = new List<CombatantData>(teamData.SelectedCombatants);
        }

        currentTeamIndex = teamIndex;
        teamData.SelectedCombatants = new List<CombatantData>(savedTeams[teamIndex]);
        selectedCharactersInTeam.Clear();
        foreach (var character in teamData.SelectedCombatants)
        {
            if (character != null) selectedCharactersInTeam.Add(character);
        }
        UpdateSlotUI();
        saveButton.interactable = true;
    }

    private void UpdateSlotUI()
    {
        for (int i = 0; i < characterSlots.Length; i++)
        {
            if (i < teamData.SelectedCombatants.Count && teamData.SelectedCombatants[i] != null)
            {
                CombatantData character = teamData.SelectedCombatants[i];
                slotTexts[i].text = character.Name;
                slotImages[i].sprite = character.AvatarSprite;
                foreach (Transform child in characterSlots[i].transform)
                {
                    if (child != slotImages[i].transform && child != slotTexts[i].transform)
                        Destroy(child.gameObject);
                }
                if (character.Prefab != null)
                {
                    GameObject model = Instantiate(character.Prefab, characterSlots[i].transform);
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localScale = Vector3.one * 0.5f;
                    model.transform.SetParent(characterSlots[i].transform, false);
                    var combatant = model.GetComponent<Combatant>();
                    if (combatant != null) combatant.enabled = false;
                }
            }
            else
            {
                slotTexts[i].text = "+";
                slotImages[i].sprite = null;
                foreach (Transform child in characterSlots[i].transform)
                {
                    if (child != slotImages[i].transform && child != slotTexts[i].transform)
                        Destroy(child.gameObject);
                }
            }
        }
        saveButton.interactable = true;
        PopulateScrollView();
    }

    private void OpenCharacterSelection(int slotIndex)
    {
        currentSlotIndex = slotIndex;
        characterSelectionPanel.SetActive(true);
        selectedCharacter = null;
        characterInfoText.text = "";
        skillInfoPanel.SetActive(false); // Hide skill panel when opening character selection
        DebugLogger.Log($"Opening characterSelectionPanel for slot {slotIndex}.");
        PopulateScrollView();
    }

    private void PopulateScrollView()
    {
        foreach (Transform child in scrollViewContent)
        {
            Destroy(child.gameObject);
        }

        DebugLogger.Log($"Populating ScrollView with {availableCharacters.Count} available characters");

        int buttonCount = 0;
        foreach (var character in availableCharacters)
        {
            GameObject buttonObj = Instantiate(buttonAvatarPrefab, scrollViewContent, false);
            UnityEngine.UI.Button button = buttonObj.GetComponent<UnityEngine.UI.Button>();
            UnityEngine.UI.Image avatar = buttonObj.GetComponentInChildren<UnityEngine.UI.Image>();
            TextMeshProUGUI levelText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if (button == null || avatar == null || levelText == null)
            {
                DebugLogger.LogError("buttonAvatarPrefab is missing required components (Button, Image, or TextMeshProUGUI).");
                continue;
            }

            RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            avatar.sprite = character.AvatarSprite;
            levelText.text = $"Lv {character.Level}";

            bool isSelected = selectedCharactersInTeam.Contains(character);
            button.interactable = !isSelected;
            avatar.color = isSelected ? Color.grey : Color.white;

            button.onClick.AddListener(() => SelectCharacter(character));
            buttonCount++;
        }

        DebugLogger.Log($"Added {buttonCount} buttons to ScrollView");

        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewContent.GetComponent<RectTransform>());
        Canvas.ForceUpdateCanvases();
    }

    private void SelectCharacter(CombatantData character)
    {
        selectedCharacter = character;
        characterInfoText.text = $"Name: {character.Name}\nHP: {character.HP}\nAttack: {character.Attack}\nElement: {character.Element}\nPath: {character.Path}";
        DebugLogger.Log($"Selected character: {character.Name}. Showing skillInfoPanel.");

        // Show skill info panel and populate it
        if (skillInfoPanel != null)
        {
            PopulateSkillInfoPanel(character);
            skillInfoPanel.SetActive(true);
        }
        else
        {
            DebugLogger.LogError("skillInfoPanel is null. Cannot show skill info.");
        }
    }

    private void PopulateSkillInfoPanel(CombatantData character)
    {
        foreach (Transform child in skillScrollViewContent)
        {
            Destroy(child.gameObject);
        }

        if (character.Skills == null || character.Skills.Length == 0)
        {
            DebugLogger.LogWarning($"No skills found for character {character.Name}.");
            return;
        }

        DebugLogger.Log($"Populating SkillInfoPanel with {character.Skills.Length} skills for {character.Name}");

        int skillCount = 0;
        foreach (var skill in character.Skills)
        {
            if (skill != null && !string.IsNullOrEmpty(skill.SkillName))
            {
                GameObject skillObj = Instantiate(skillButtonPrefab, skillScrollViewContent, false);
                UnityEngine.UI.Image skillIcon = skillObj.GetComponentInChildren<UnityEngine.UI.Image>();
                TextMeshProUGUI skillText = skillObj.GetComponentInChildren<TextMeshProUGUI>();

                if (skillIcon == null || skillText == null)
                {
                    DebugLogger.LogError("skillButtonPrefab is missing required components (Image or TextMeshProUGUI).");
                    Destroy(skillObj);
                    continue;
                }

                RectTransform rectTransform = skillObj.GetComponent<RectTransform>();
                rectTransform.localPosition = Vector3.zero;
                rectTransform.localScale = Vector3.one;
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.zero;
                rectTransform.pivot = new Vector2(0.5f, 0.5f);

                skillIcon.sprite = skill.SkillIcon != null ? skill.SkillIcon : null;
                skillText.text = $"Skill: {skill.SkillName}\n" +
                                 $"Type: {(skill.IsBuff ? "Buff" : "Debuff")}\n" +
                                 $"Target: {(skill.IsAoE ? "AOE" : "1")}";

                skillCount++;
            }
            else
            {
                DebugLogger.LogWarning($"Invalid skill for {character.Name}: SkillName is {(skill == null ? "null" : "empty")}.");
            }
        }

        DebugLogger.Log($"Added {skillCount} skills to SkillInfoPanel");

        if (skillCount == 0)
        {
            DebugLogger.LogWarning($"No valid skills displayed for {character.Name}. Check SkillData configuration.");
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(skillScrollViewContent.GetComponent<RectTransform>());
        Canvas.ForceUpdateCanvases();
    }

    private void CloseSkillInfoPanel()
    {
        skillInfoPanel.SetActive(false);
        DebugLogger.Log("skillInfoPanel closed.");
    }

    private void ConfirmCharacterSelection()
    {
        if (selectedCharacter != null)
        {
            if (currentSlotIndex >= 0 && currentSlotIndex < characterSlots.Length)
            {
                if (currentSlotIndex < teamData.SelectedCombatants.Count && teamData.SelectedCombatants[currentSlotIndex] != null)
                {
                    selectedCharactersInTeam.Remove(teamData.SelectedCombatants[currentSlotIndex]);
                }

                teamData.SetCharacter(currentSlotIndex, selectedCharacter);
                if (!selectedCharactersInTeam.Contains(selectedCharacter))
                {
                    selectedCharactersInTeam.Add(selectedCharacter);
                }
                UpdateSlotUI();
                characterSelectionPanel.SetActive(false);
                skillInfoPanel.SetActive(false);
                currentSlotIndex = -1;
                DebugLogger.Log($"Character {selectedCharacter.Name} confirmed for slot {currentSlotIndex}.");
            }
        }
        else
        {
            DebugLogger.LogWarning("No character selected for confirmation.");
        }
    }

    private void CloseCharacterSelection()
    {
        characterSelectionPanel.SetActive(false);
        skillInfoPanel.SetActive(false);
        selectedCharacter = null;
        currentSlotIndex = -1;
        DebugLogger.Log("characterSelectionPanel closed.");
    }

    private void SaveTeam()
    {
        savedTeams[currentTeamIndex] = new List<CombatantData>(teamData.SelectedCombatants);
        SaveTeamToPlayerPrefs(currentTeamIndex);
        DebugLogger.Log($"Team {currentTeamIndex + 1} saved to PlayerPrefs.");
        teamSetupPanel.SetActive(false);
    }

    private void QuickSelectTeam()
    {
        teamData.SelectedCombatants = new List<CombatantData>();
        selectedCharactersInTeam.Clear();
        List<CombatantData> randomCharacters = new List<CombatantData>(availableCharacters);
        randomCharacters.Sort((a, b) => UnityEngine.Random.value > 0.5f ? 1 : -1);
        for (int i = 0; i < 4 && i < randomCharacters.Count; i++)
        {
            teamData.SetCharacter(i, randomCharacters[i]);
            selectedCharactersInTeam.Add(randomCharacters[i]);
        }
        UpdateSlotUI();
    }

    private void ClearTeam()
    {
        teamData.SelectedCombatants = new List<CombatantData>();
        selectedCharactersInTeam.Clear();
        UpdateSlotUI();
    }

    private void ReturnToMap()
    {
        teamSetupPanel.SetActive(false);
    }

    public void SetAvailableCharacters(List<CombatantData> list)
    {
        availableCharacters = list ?? new List<CombatantData>();
        RefreshScrollView();
    }

    public void AddAvailableCharacter(CombatantData character)
    {
        if (character == null) return;
        if (!availableCharacters.Contains(character))
        {
            availableCharacters.Add(character);
            RefreshScrollView();
        }
    }

    public void RefreshScrollView()
    {
        PopulateScrollView();
    }

    private void SaveTeamToPlayerPrefs(int teamIndex)
    {
        TeamSaveData saveData = new TeamSaveData
        {
            characterIds = savedTeams[teamIndex].Select(c => c?.CharacterId ?? "").ToArray()
        };

        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString($"Team{teamIndex + 1}", json);
        PlayerPrefs.Save();
    }

    private void LoadTeamsFromPlayerPrefs()
    {
        for (int i = 0; i < savedTeams.Length; i++)
        {
            string json = PlayerPrefs.GetString($"Team{i + 1}", "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    TeamSaveData saveData = JsonUtility.FromJson<TeamSaveData>(json);
                    savedTeams[i] = new List<CombatantData>();
                    foreach (string characterId in saveData.characterIds)
                    {
                        if (!string.IsNullOrEmpty(characterId))
                        {
                            CombatantData character = availableCharacters.Find(c => c.CharacterId == characterId);
                            if (character != null)
                            {
                                savedTeams[i].Add(character);
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    DebugLogger.LogError($"Failed to load Team {i + 1} from PlayerPrefs: {e.Message}");
                }
            }
        }

        if (currentTeamIndex >= 0 && currentTeamIndex < savedTeams.Length)
        {
            teamData.SelectedCombatants = new List<CombatantData>(savedTeams[currentTeamIndex]);
            selectedCharactersInTeam.Clear();
            foreach (var character in teamData.SelectedCombatants)
            {
                if (character != null) selectedCharactersInTeam.Add(character);
            }
            UpdateSlotUI();
        }
    }
    private IEnumerator RunFirstTimeTutorial()
    {
        // Tắt hết các button không cần thiết lúc step 1
        foreach (var btn in teamOptionButtons) btn.interactable = false;
        foreach (var btn in characterSlots) btn.interactable = false;

        quickSelectButton.interactable = true;
        saveButton.interactable = false;
        clearButton.interactable = false;
        backButton.interactable = false;

        // Highlight Quick
        tutorialOverlay.ShowOverlay(quickSelectButton.GetComponent<RectTransform>());
        bool quickSelected = false;
        quickSelectButton.onClick.AddListener(() => quickSelected = true);
        yield return new WaitUntil(() => quickSelected);

        // Step 2: Save
        quickSelectButton.interactable = false;
        saveButton.interactable = true;
        tutorialOverlay.ShowOverlay(saveButton.GetComponent<RectTransform>());

        bool saved = false;
        saveButton.onClick.AddListener(() =>
        {
            saved = true;

            // 🔥 Kết thúc tutorial
            tutorialOverlay.HideOverlay();
            PlayerPrefs.SetInt("FirstTimeTeamSetup", 0);
            PlayerPrefs.Save();

            // 👉 Reset lại toàn bộ UI về bình thường
            foreach (var btn in teamOptionButtons) btn.interactable = true;
            foreach (var btn in characterSlots) btn.interactable = true;
            quickSelectButton.interactable = true;
            saveButton.interactable = true;
            clearButton.interactable = true;
            backButton.interactable = true;

            DebugLogger.Log("[Tutorial] Completed. UI reset to normal.");
        });

        yield return new WaitUntil(() => saved);
    }





}