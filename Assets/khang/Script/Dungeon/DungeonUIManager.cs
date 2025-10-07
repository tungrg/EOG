using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DungeonUIManager : MonoBehaviour
{
    [SerializeField] private DungeonData dungeonData;
    [SerializeField] private DungeonProgressData progressData;
    [SerializeField] private GameObject dungeonPanel;
    [SerializeField] private Button openDungeonButton;
    [SerializeField] private Button closeDungeonButton;
    [SerializeField] private Transform tabContainer;
    [SerializeField] private GameObject tabButtonPrefab;
    [SerializeField] private Transform levelContainer;
    [SerializeField] private GameObject levelButtonPrefab;
    [SerializeField] private GameObject prefabPanel;
    [SerializeField] private Image panelImage;
    [SerializeField] private TeamData teamData;
    [SerializeField] private TeamSetupManager teamSetupManager;

    private TabDungeonData currentTab;

    void Start()
    {
        if (!ValidateComponents())
        {
            DebugLogger.LogError("DungeonUIManager components not assigned properly.");
            return;
        }

        if (teamSetupManager == null)
        {
            teamSetupManager = FindObjectOfType<TeamSetupManager>();
            if (teamSetupManager == null)
            {
                DebugLogger.LogError("TeamSetupManager not found in the scene.");
            }
        }

        prefabPanel.SetActive(false);
        openDungeonButton.onClick.AddListener(ShowDungeonPanel);
        closeDungeonButton.onClick.AddListener(HideDungeonPanel);
        InitializeTabs();
    }

    private bool ValidateComponents()
    {
        return dungeonData != null &&
               progressData != null &&
               dungeonPanel != null &&
               openDungeonButton != null &&
               closeDungeonButton != null &&
               tabContainer != null &&
               tabButtonPrefab != null &&
               levelContainer != null &&
               levelButtonPrefab != null &&
               prefabPanel != null &&
               panelImage != null &&
               teamData != null;
    }

    private void InitializeTabs()
    {
        foreach (Transform child in tabContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var tab in dungeonData.Tabs)
        {
            if (tab == null) continue;

            GameObject tabButtonObj = Instantiate(tabButtonPrefab, tabContainer);
            Button tabButton = tabButtonObj.GetComponent<Button>();
            TextMeshProUGUI tabText = tabButtonObj.GetComponentInChildren<TextMeshProUGUI>();
            Image tabImage = tabButtonObj.GetComponent<Image>();

            if (tabText != null)
            {
                tabText.text = tab.Element.ToString();
            }
            else
            {
                DebugLogger.LogWarning($"TextMeshProUGUI not found on tab button for {tab.Element}");
            }

            if (tabImage != null && tab.TabSprite != null)
            {
                tabImage.sprite = tab.TabSprite;
            }
            else
            {
                DebugLogger.LogWarning($"Image or TabSprite not found for tab {tab.Element}");
            }

            tabButton.onClick.AddListener(() => ShowTabContent(tab));
        }
    }

    private void ShowDungeonPanel()
    {
        if (dungeonData.Tabs == null || dungeonData.Tabs.Count == 0)
        {
            DebugLogger.LogError("No tabs found in DungeonData.");
            return;
        }
        GameObject layoutDungeon = GameObject.Find("DungeonUIManager/Canvas/LayoutDungeon");
        if (layoutDungeon == null)
        {
            DebugLogger.LogError("LayoutDungeon GameObject not found in the scene.");
        }
        else
        {
            layoutDungeon.SetActive(true);
        }
        

        UILayer.Instance.ShowPanel(dungeonPanel);
        ShowTabContent(dungeonData.Tabs[0]);
    }

    private void ShowTabContent(TabDungeonData tab)
    {
        if (tab == null || tab.Levels == null)
        {
            DebugLogger.LogError($"TabDungeonData or Levels is null for {tab?.Element}");
            return;
        }

        currentTab = tab;

        if (prefabPanel != null && panelImage != null)
        {
            prefabPanel.SetActive(true);
            if (tab.PanelSprite != null)
            {
                panelImage.sprite = tab.PanelSprite;
                DebugLogger.Log($"[DungeonUIManager] Displaying PanelSprite for {tab.Element}");
            }
            else
            {
                DebugLogger.LogWarning($"PanelSprite not set for {tab.Element}");
                panelImage.sprite = null;
            }
        }
        else
        {
            DebugLogger.LogError("PrefabPanel or PanelImage not assigned in DungeonUIManager.");
        }

        ShowLevels(tab);
    }

    private void ShowLevels(TabDungeonData tab)
    {
        foreach (Transform child in levelContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var level in tab.Levels)
        {
            if (level == null) continue;

            GameObject levelButtonObj = Instantiate(levelButtonPrefab, levelContainer);
            Button levelButton = levelButtonObj.GetComponent<Button>();
            TextMeshProUGUI levelText = levelButtonObj.GetComponentInChildren<TextMeshProUGUI>();

            string status = progressData.GetLevelStatus(tab.Element, level.Level);
            bool isAvailable = status == "Available" || status == "Completed";

            if (levelText != null)
            {
                levelText.text = $"Level {level.Level} ({status})";
            }
            else
            {
                DebugLogger.LogWarning($"TextMeshProUGUI not found on level button for Level {level.Level}");
            }

            levelButton.interactable = isAvailable;
            if (!isAvailable)
            {
                var colors = levelButton.colors;
                colors.normalColor = Color.gray;
                colors.highlightedColor = Color.gray;
                levelButton.colors = colors;
            }

            if (isAvailable)
            {
                levelButton.onClick.AddListener(() => StartDungeonBattle(level));
            }
        }
    }

    private void StartDungeonBattle(LevelDungeonData level)
    {
        if (level == null || level.Enemies == null)
        {
            DebugLogger.LogError($"[DungeonUIManager] LevelDungeonData or Enemies is null for Level {level?.Level}");
            return;
        }

        if (teamData.SelectedCombatants.Count == 0)
        {
            DebugLogger.Log("[DungeonUIManager] Team is empty. Showing team setup before dungeon battle.");
            teamData.SelectedEnemies = new List<EnemyData>(level.Enemies);
            teamData.CurrentBattleType = BattleType.Dungeon;
            teamData.CurrentElement = currentTab.Element;
            teamData.CurrentLevel = level.Level;
            if (teamSetupManager != null)
            {
                teamSetupManager.ShowTeamSetupPanel();
            }
            else
            {
                DebugLogger.LogError("Cannot show team setup panel: TeamSetupManager is null.");
            }
        }
        else
        {
            teamData.SelectedEnemies = new List<EnemyData>(level.Enemies);
            teamData.CurrentBattleType = BattleType.Dungeon;
            teamData.CurrentElement = currentTab.Element;
            teamData.CurrentLevel = level.Level;
            DebugLogger.Log($"[DungeonUIManager] Starting Dungeon battle for {currentTab.Element} Level {level.Level} with enemies: {string.Join(", ", level.Enemies.Select(e => e?.Name ?? "null"))}");
            SceneManager.LoadScene("Loading 1");
        }
    }

    public void ConfirmTeamAndStartDungeonBattle()
    {
        if (teamData.IsTeamFull())
        {
            prefabPanel.SetActive(false);
            DebugLogger.Log("[DungeonUIManager] Team selected and full. Starting dungeon battle.");
            SceneManager.LoadScene("Loading 1");
        }
        else
        {
            DebugLogger.LogWarning("[DungeonUIManager] Team is not full. Please select more characters.");
            if (teamSetupManager != null)
            {
                teamSetupManager.ShowTeamSetupPanel();
            }
            else
            {
                DebugLogger.LogError("Cannot show team setup panel: TeamSetupManager is null.");
            }
        }
    }

    public void HideDungeonPanel()
    {
        UILayer.Instance.HideAllPanels();
        prefabPanel.SetActive(false);
        GameObject layoutDungeon = GameObject.Find("DungeonUIManager/Canvas/LayoutDungeon");
        if (layoutDungeon == null)
        {
            DebugLogger.LogError("LayoutDungeon GameObject not found in the scene.");
        }
        else
        {
            layoutDungeon.SetActive(false);
        }
    }
}