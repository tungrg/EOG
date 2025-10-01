using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject actionPanel;
    [SerializeField] private UnityEngine.UI.Button[] actionButtons;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text turnCountText;
    [SerializeField] private TMP_Text targetEnemyText;

    // Panel chung cho ally & enemy
    [SerializeField] private GameObject selectedInfoPanel;
    [SerializeField] private Image selectedAvatarImage;
    [SerializeField] private TMP_Text selectedNameText;

    [SerializeField] private GameObject enemyDetailPanel;
    [SerializeField] private TMP_Text enemyDetailText;

    [SerializeField] private GameObject characterInfoPanelPrefab;
    [SerializeField] private Transform characterPanelsContainer;

    [SerializeField] private TMP_Text warningText;
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI weaknessText;

    private List<CharacterInfoPanel> characterInfoPanels = new List<CharacterInfoPanel>();
    private CombatManager combatManager;
    private bool isActionSelected = false;


    public GameObject GetWarningPanel()
    {
        return warningPanel;
    }
    public void SetCombatManager(CombatManager manager)
    {
        combatManager = manager;
    }


    public Button GetCancelButton()
    {
        return cancelButton;
    }


    public void InitializeCharacterPanels(List<Combatant> combatants)
    {
        if (characterInfoPanelPrefab == null || characterPanelsContainer == null)
        {
            DebugLogger.LogError("CharacterInfoPanelPrefab or CharacterPanelsContainer not assigned.");
            return;
        }

        foreach (Transform child in characterPanelsContainer)
        {
            Destroy(child.gameObject);
        }
        characterInfoPanels.Clear();

        for (int i = 0; i < Mathf.Min(combatants.Count, 4); i++)
        {
            if (combatants[i] != null)
            {
                GameObject panelObj = Instantiate(characterInfoPanelPrefab, characterPanelsContainer);
                CharacterInfoPanel panel = panelObj.GetComponent<CharacterInfoPanel>();
                if (panel != null)
                {
                    panel.SetCombatant(combatants[i]);
                    characterInfoPanels.Add(panel);
                }
                else
                {
                    DebugLogger.LogError($"CharacterInfoPanel component missing on instantiated prefab for combatant {combatants[i].Name}");
                }
            }
        }
    }

    public void HighlightCharacterPanel(Combatant activeCombatant)
    {
        foreach (var panel in characterInfoPanels)
        {
            panel.SetHighlight(panel.Combatant == activeCombatant);
        }
    }

    public void UpdateCharacterPanels()
    {
        foreach (var panel in characterInfoPanels)
        {
            panel.UpdatePanel();
        }
    }

    public void ShowActionPanel(Combatant combatant, System.Action<int> onActionSelected)
    {
        if (actionPanel == null || actionButtons == null || characterNameText == null || turnCountText == null || targetEnemyText == null)
        {
            DebugLogger.LogError("UI components not assigned.");
            return;
        }

        actionPanel.SetActive(true);
        characterNameText.text = combatant.Name;
        targetEnemyText.text = "Target: None"; // Khởi tạo văn bản mục tiêu

        isActionSelected = false;

        for (int i = 0; i < actionButtons.Length; i++)
        {
            if (i < combatant.GetData().Skills.Length)
            {
                SkillData skill = combatant.GetData().Skills[i];
                actionButtons[i].gameObject.SetActive(true);
                actionButtons[i].GetComponentInChildren<TMP_Text>().text = skill.SkillName;

                Image buttonImage = actionButtons[i].GetComponent<Image>();
                if (buttonImage != null && skill.SkillIcon != null)
                {
                    buttonImage.sprite = skill.SkillIcon;
                }
                else
                {
                    DebugLogger.LogWarning($"No Image component or SkillIcon assigned for skill {skill.SkillName} on button {i}");
                }

                int index = i;
                actionButtons[i].onClick.RemoveAllListeners();
                actionButtons[i].onClick.AddListener(() =>
                {
                    if (isActionSelected)
                    {
                        DebugLogger.LogWarning($"[UI] Action already selected for {combatant.Name}. Ignoring additional click.");
                        return;
                    }

                    isActionSelected = true;
                    foreach (var button in actionButtons)
                    {
                        button.interactable = false;
                    }
                    onActionSelected(index);
                });

                bool isEnabled = false;
                switch (index)
                {
                    case 0: isEnabled = true; break;
                    case 1: isEnabled = combatant.SkillCharge >= 3; break;
                    case 2: isEnabled = combatant.Mana >= combatant.GetData().Skill3ManaCost; break;
                }
                actionButtons[i].interactable = isEnabled;
            }
            else
            {
                actionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void HideActionPanel()
    {
        if (actionPanel != null)
        {
            actionPanel.SetActive(false);
        }
        isActionSelected = false;
    }

    public void UpdateTurnCount(int count)
    {
        if (turnCountText != null)
        {
            turnCountText.text = "" + count;
        }
    }

    // ====== Panel chung cho cả Ally và Enemy ======
    public void ShowCombatantInfo(ICombatant combatant)
    {
        if (selectedInfoPanel == null || selectedAvatarImage == null || selectedNameText == null)
        {
            DebugLogger.LogError("SelectedInfoPanel components not assigned.");
            return;
        }

        if (combatant == null)
        {
            HideCombatantInfo();
            return;
        }

        selectedInfoPanel.SetActive(true);
        selectedNameText.text = combatant.Name;

        Sprite avatar = null;
        if (combatant is Combatant ally)
            avatar = ally.GetData().AvatarSprite;
        else if (combatant is Enemy enemy)
            avatar = enemy.GetData().AvatarSprite;

        if (avatar != null)
            selectedAvatarImage.sprite = avatar;
        else
            DebugLogger.LogWarning($"No AvatarSprite assigned for {combatant.Name}");

        // Nếu là enemy thì ẩn detail panel cho tới khi người chơi mở
        if (combatant is Enemy)
            HideEnemyDetail();
    }

    public void HideCombatantInfo()
    {
        if (selectedInfoPanel != null)
            selectedInfoPanel.SetActive(false);

        HideEnemyDetail();
    }


    public void ShowDetail(ICombatant combatant)
    {
        if (enemyDetailPanel == null || enemyDetailText == null)
        {
            DebugLogger.LogError("DetailPanel components not assigned.");
            return;
        }

        if (combatant == null) return;

        enemyDetailPanel.SetActive(true);

        string status;
        string skills;
        string detailText;

        if (combatant is Enemy enemy)
        {
            EnemyData data = enemy.GetData();
            status = enemy.HP > 0 ? "Active" : "Defeated";
            skills = string.Join("\n", data.Skills.Select(s => s != null ? s.SkillName : "None"));
            detailText =
                $"[ENEMY INFO]\n" +
                $"Name: {data.Name}\n" +
                $"HP: {enemy.HP}/{data.MaxHP}\n" +
                $"Element: {data.Element}\n" +
                $"Attack Type: {data.AttackType}\n" +
                $"Attack Range: {data.AttackRange}\n" +
                // $"Skills:\n{skills}\n" +
                $"Status: {status}";
        }
        else if (combatant is Combatant ally)
        {
            CombatantData data = ally.GetData();
            status = ally.HP > 0 ? "Active" : "Defeated";
            skills = string.Join("\n", data.Skills.Select(s => s != null ? s.SkillName : "None"));
            detailText =
                $"[ALLY INFO]\n" +
                $"Name: {data.Name}\n" +
                $"HP: {ally.HP}/{data.MaxHP}\n" +
                $"Element: {data.Element}\n" +
                $"Attack Type: {data.AttackType}\n" +
                $"Attack Range: {data.AttackRange}\n" +
                $"Skills:\n{skills}\n" +
                $"Status: {status}";
        }
        else
        {
            detailText = "[UNKNOWN TYPE]";
        }

        enemyDetailText.text = detailText;
    }

    // Phương thức để cập nhật targetEnemyText
    public void SetTargetText(string message)
    {
        if (targetEnemyText != null)
        {
            targetEnemyText.text = message;
        }
        else
        {
            DebugLogger.LogWarning("targetEnemyText not assigned in UIManager.");
        }
    }

    public void ShowWarning(string message)
    {
        if (warningPanel != null && warningText != null)
        {
            warningText.text = message;
            warningPanel.SetActive(true);
        }
        else
        {
            DebugLogger.LogWarning("warningPanel or warningText not assigned in UIManager.");
        }
    }

    // Phương thức để ẩn warningPanel
    public void HideWarning()
    {
        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
        }
    }

    public void ShowTargetSelectionPrompt(bool isAlly)
    {
        if (warningPanel != null && warningText != null)
        {
            warningText.text = isAlly ? "Please select a valid ally!" : "Please select a valid enemy!";
            warningPanel.SetActive(true);
            DebugLogger.Log($"[UI] Hiển thị giao diện chọn {(isAlly ? "đồng minh" : "kẻ thù")}");
        }
        else
        {
            DebugLogger.LogWarning("warningPanel hoặc warningText chưa được gán trong UIManager.");
        }
    }

    public void HideTargetSelectionPrompt()
    {
        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
            DebugLogger.Log("[UI] Ẩn giao diện chọn mục tiêu");
        }
    }

    public void HideEnemyDetail()
    {
        if (enemyDetailPanel != null)
        {
            enemyDetailPanel.SetActive(false);
        }
    }


    public void ShowWeaknessText()
    {
        if (weaknessText == null)
        {
            DebugLogger.LogError("WeaknessText not assigned in UIManager.");
            return;
        }

        // Bật text và đặt nội dung
        weaknessText.gameObject.SetActive(true);
        weaknessText.text = "Weakness";
        weaknessText.color = Color.yellow;

        // Lấy hoặc thêm CanvasGroup để làm mờ dần
        CanvasGroup canvasGroup = weaknessText.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = weaknessText.gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 1f;

        StartCoroutine(FadeAndHideText(canvasGroup, 1.5f));
    }

    private IEnumerator FadeAndHideText(CanvasGroup canvasGroup, float duration)
    {
        float elapsed = 0f;
        Vector3 initialPosition = canvasGroup.transform.position;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - (elapsed / duration);
            canvasGroup.transform.position = initialPosition + Vector3.up * (elapsed * 50f); // Di chuyển lên trên
            yield return null;
        }
        canvasGroup.gameObject.SetActive(false);
        canvasGroup.transform.position = initialPosition; // Reset vị trí
    }
}