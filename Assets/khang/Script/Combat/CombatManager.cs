using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CombatManager : MonoBehaviour
{
    [SerializeField] private TeamData teamData;
    [SerializeField] private List<Transform> playerSpawnPoints = new List<Transform>();
    [SerializeField] private List<Transform> enemySpawnPoints = new List<Transform>();
    [SerializeField] private MapEnemyProgress mapProgress;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private GameObject resultScreenObject;
    [SerializeField] private DungeonData dungeonData;
    [SerializeField] private InventoryData inventoryData;
    [SerializeField] private DungeonProgressData progressData;
    [SerializeField] private RewardConfig rewardConfig;
    [SerializeField] private ResultScreen resultScreen;
    [SerializeField] private GameObject countdownText;

    [SerializeField] private Button exitButton;
    [SerializeField] private GameObject exitConfirmationPanel;
    [SerializeField] private TextMeshProUGUI confirmationText;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;


    private CombatantManager combatantManager;
    private TurnManager turnManager;
    private MovementManager movementManager;
    private ObjectPool objectPool;
    private List<StoneData> collectedStones = new List<StoneData>();
    private List<(string characterName, string skillName, int damage)> battleHistory = new List<(string, string, int)>();
    private int turnCount;
    private bool isProcessingTurn = false;
    private bool victory;
    private bool waitingForPlayerAction = false;
    private ICombatant persistentTarget;



    void Awake()
    {
        combatantManager = gameObject.AddComponent<CombatantManager>();
        turnManager = gameObject.AddComponent<TurnManager>();
        movementManager = gameObject.AddComponent<MovementManager>();
        objectPool = gameObject.AddComponent<ObjectPool>();
        if (uiManager == null)
        {
            DebugLogger.LogError("UIManager not assigned in CombatManager.");
        }
        else
        {
            uiManager.SetCombatManager(this);
        }
        // Kiểm tra các thành phần UI 
        if (exitButton == null || exitConfirmationPanel == null || confirmationText == null ||
            confirmYesButton == null || confirmNoButton == null)
        {
            DebugLogger.LogError("Exit button, confirmation panel, confirmation text, or confirm buttons not assigned in CombatManager.");
        }

        combatantManager.SetTeamData(teamData);
        combatantManager.SetSpawnPoints(playerSpawnPoints, enemySpawnPoints);
        movementManager.SetCombatManager(this);
    }

    void Start()
    {
        if (!ValidateSetup())
        {
            DebugLogger.LogError("Cannot start battle: Invalid TeamData or no combatants/enemies selected. Returning to Map scene.");
            SceneManager.LoadScene("Map");
            return;
        }

        foreach (var combatant in teamData.SelectedCombatants)
        {
            foreach (var skill in combatant.Skills)
            {
                if (skill?.EffectPrefab != null)
                {
                    objectPool.InitializePool(skill.EffectPrefab, 5);
                }
            }
        }
        foreach (var enemy in teamData.SelectedEnemies)
        {
            foreach (var skill in enemy.Skills)
            {
                if (skill?.EffectPrefab != null)
                {
                    objectPool.InitializePool(skill.EffectPrefab, 5);
                }
            }
        }

        combatantManager.SetupCombatants();
        if (uiManager != null)
        {
            uiManager.InitializeCharacterPanels(combatantManager.PlayerCombatants);
        }

        OrientCombatants();
        turnManager.Initialize(combatantManager.PlayerCombatants, combatantManager.EnemyCombatants);
        // DebugLogger.Log("[START] Initializing combat system");

        // Thêm listener cho nút thoát
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ShowExitConfirmationPanel);
            exitConfirmationPanel.SetActive(false); // Ẩn panel xác nhận khi bắt đầu
        }
        StartCoroutine(StartBattleWithCountdown());
    }

    // Hàm mới: Hiển thị panel xác nhận thoát trận
    private void ShowExitConfirmationPanel()
    {
        if (exitConfirmationPanel != null && confirmationText != null && confirmYesButton != null && confirmNoButton != null)
        {
            exitConfirmationPanel.SetActive(true);
            confirmationText.text = "Are you sure you want to leave the match? This will be recorded as a defeat.";

            // Xóa listener cũ để tránh lặp
            confirmYesButton.onClick.RemoveAllListeners();
            confirmNoButton.onClick.RemoveAllListeners();

            // Thêm listener cho nút Yes/No
            confirmYesButton.onClick.AddListener(ExitBattle);
            confirmNoButton.onClick.AddListener(() => exitConfirmationPanel.SetActive(false));

            DebugLogger.Log("[UI] Exit confirmation panel shown");
        }
        else
        {
            DebugLogger.LogError("Cannot show exit confirmation panel: UI components not assigned.");
        }
    }
    // Hàm mới: Xử lý thoát trận
    private void ExitBattle()
    {
        DebugLogger.Log("[CombatManager] Player chose to exit battle. Marking as loss and returning to Map scene.");

        // Đánh dấu trận đấu là thua
        victory = false;
        collectedStones.Clear();

        if (teamData.CurrentBattleType == BattleType.Map)
        {
            PlayerPrefs.SetInt("BattleLost", 1);
            PlayerPrefs.SetInt("WaveCompleted", 0);
            PlayerPrefs.Save();
            DebugLogger.Log("[CombatManager] Player lost by exiting. Set BattleLost=1, WaveCompleted=0, PlayerPrefs saved.");
        }

        // Tải lại scene Map
        SceneManager.LoadScene("Loading");
    }

    private IEnumerator StartBattleWithCountdown()
    {
        if (countdownText != null)
        {
            countdownText.SetActive(true);
            var textComponent = countdownText.GetComponent<TextMeshProUGUI>();

            for (int i = 3; i > 0; i--)
            {
                if (textComponent != null) textComponent.text = i.ToString();

                yield return new WaitForSeconds(1f);
            }

            if (textComponent != null) textComponent.text = "Fight!";

            yield return new WaitForSeconds(1f);

            countdownText.SetActive(false);
        }

        StartCoroutine(ProcessTicks());
    }

    private IEnumerator ProcessTicks()
    {
        while (!combatantManager.IsBattleOver())
        {
            if (!isProcessingTurn)
            {
                var turnInfo = turnManager.ProcessNextTick();
                if (turnInfo != null)
                {
                    yield return StartCoroutine(ProcessTurn(turnInfo.Value));
                }
            }
            yield return null;
        }
        DebugLogger.Log("[BATTLE END] Battle is over, calculating rewards and showing result screen");
        if (teamData.CurrentBattleType == BattleType.Dungeon)
        {
            bool isVictory = combatantManager.EnemyCombatants.All(e => e == null || e.HP <= 0);
            if (isVictory)
            {
                CalculateStoneDrops();
                MarkLevelCompleted();
            }
            else
            {
                collectedStones.Clear();
            }
        }
        else
        {
            collectedStones.Clear();
        }
        ShowBattleResult();
    }



    private void CalculateStoneDrops()
    {
        collectedStones.Clear();
        if (dungeonData == null)
        {
            DebugLogger.LogError("DungeonData is not assigned in CombatManager Inspector.");
            return;
        }
        if (inventoryData == null)
        {
            DebugLogger.LogError("InventoryData is not assigned in CombatManager Inspector.");
            return;
        }

        ElementType element = teamData.CurrentElement;
        int levelNum = teamData.CurrentLevel;

        TabDungeonData currentTab = dungeonData.Tabs.FirstOrDefault(t => t.Element == element);
        if (currentTab == null)
        {
            DebugLogger.LogError($"Could not find TabDungeonData for element {element}.");
            return;
        }

        LevelDungeonData currentLevel = currentTab.Levels.FirstOrDefault(l => l.Level == levelNum);
        if (currentLevel == null)
        {
            DebugLogger.LogError($"Could not find LevelDungeonData for {element} Level {levelNum}.");
            return;
        }

        DebugLogger.Log($"[CalculateStoneDrops] Processing drops for {element} Level {levelNum} with {currentLevel.StoneDrops.Count} StoneDrops");

        if (currentLevel.StoneDrops == null || currentLevel.StoneDrops.Count == 0)
        {
            DebugLogger.LogError($"No StoneDrops configured for {element} Level {levelNum}");
            return;
        }

        int stoneCount = levelNum switch
        {
            1 => 3,
            2 => 4,
            3 => 5,
            4 => 5,
            5 => 6,
            _ => 3
        };

        float totalDropRate = currentLevel.StoneDrops.Sum(drop => drop?.DropRate ?? 0);
        DebugLogger.Log($"Total Drop Rate: {totalDropRate}%");
        if (Mathf.Abs(totalDropRate - 100f) > 0.01f)
        {
            DebugLogger.LogWarning($"Total drop rate for {element} Level {levelNum} is {totalDropRate}%, should be 100%. Normalizing...");
            float scale = 100f / totalDropRate;
            foreach (var drop in currentLevel.StoneDrops)
            {
                if (drop != null) drop.DropRate *= scale;
            }
        }

        for (int i = 0; i < stoneCount; i++)
        {
            float random = Random.Range(0f, 100f);
            float cumulative = 0f;
            foreach (var stoneDrop in currentLevel.StoneDrops)
            {
                if (stoneDrop == null || stoneDrop.Stone == null) continue;
                cumulative += stoneDrop.DropRate;
                if (random <= cumulative)
                {
                    // chỉ thêm vào collectedStones — không add vào inventory ở đây
                    collectedStones.Add(stoneDrop.Stone);
                    DebugLogger.Log($"[STONE DROP] Dropped {stoneDrop.Stone.ItemName} ({element} Level {levelNum}, Rate: {stoneDrop.DropRate}%, Random: {random}, Cumulative: {cumulative})");
                    break;
                }
            }
        }

        if (collectedStones.Count == 0)
        {
            DebugLogger.LogWarning($"No stones dropped for {element} Level {levelNum}. Check StoneDrops configuration.");
        }
        else
        {
            DebugLogger.Log($"Collected {collectedStones.Count} stones: {string.Join(", ", collectedStones.Select(s => s.ItemName))}");
        }
    }

    private void MarkLevelCompleted()
    {
        if (teamData.CurrentBattleType != BattleType.Dungeon)
        {
            DebugLogger.Log("[CombatManager] Not a dungeon battle, skipping mark level completed.");
            return;
        }

        if (progressData == null)
        {
            DebugLogger.LogError("[CombatManager] DungeonProgressData is not assigned in CombatManager Inspector.");
            return;
        }

        ElementType element = teamData.CurrentElement;
        int levelNum = teamData.CurrentLevel;

        progressData.MarkLevelCompleted(element, levelNum);
        DebugLogger.Log($"[CombatManager] Marked {element} Level {levelNum} as completed");
    }

    private bool ValidateSetup()
    {
        bool isValid = true;
        if (teamData == null)
        {
            DebugLogger.LogError("TeamData is null in CombatManager.");
            isValid = false;
        }
        if (teamData != null && (teamData.SelectedCombatants == null || teamData.SelectedCombatants.Count == 0))
        {
            DebugLogger.LogError("No combatants selected in TeamData.");
            isValid = false;
        }
        if (teamData != null && (teamData.SelectedEnemies == null || teamData.SelectedEnemies.Count == 0))
        {
            DebugLogger.LogError("No enemies selected in TeamData.");
            isValid = false;
        }
        if (playerSpawnPoints.Count < 4)
        {
            DebugLogger.LogError("Need at least 4 player spawn points.");
            isValid = false;
        }
        if (enemySpawnPoints.Count < 1)
        {
            DebugLogger.LogError("Need at least 1 enemy spawn point.");
            isValid = false;
        }
        return isValid;
    }

    private void UpdateCombatantRotations()
    {
        foreach (var player in combatantManager.PlayerCombatants.Where(p => p != null && p.HP > 0))
        {
            var nearestEnemy = combatantManager.EnemyCombatants.Where(e => e != null && e.HP > 0)
                .OrderBy(e => Vector3.Distance(player.transform.position, e.transform.position))
                .FirstOrDefault();
            if (nearestEnemy != null)
            {
                UpdateRotation(player.transform, nearestEnemy.transform);
            }
        }

        foreach (var enemy in combatantManager.EnemyCombatants.Where(e => e != null && e.HP > 0))
        {
            var nearestPlayer = combatantManager.PlayerCombatants.Where(p => p != null && p.HP > 0)
                .OrderBy(p => Vector3.Distance(enemy.transform.position, p.transform.position))
                .FirstOrDefault();
            if (nearestPlayer != null)
            {
                UpdateRotation(enemy.transform, nearestPlayer.transform);
            }
        }
    }

    private void UpdateRotation(Transform mover, Transform target)
    {
        if (mover == null || target == null) return;
        Vector3 directionToTarget = (target.position - mover.position).normalized;
        directionToTarget.y = 0;
        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            mover.rotation = Quaternion.Slerp(mover.rotation, targetRotation, Time.deltaTime * 12f);
        }
    }

    private void OrientCombatants()
    {
        foreach (var player in combatantManager.PlayerCombatants.Where(p => p != null && p.HP > 0))
        {
            var nearestEnemy = combatantManager.EnemyCombatants.Where(e => e != null && e.HP > 0)
                .OrderBy(e => Vector3.Distance(player.transform.position, e.transform.position))
                .FirstOrDefault();
            if (nearestEnemy != null)
            {
                Vector3 direction = (nearestEnemy.transform.position - player.transform.position).normalized;
                direction.y = 0;
                player.transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        foreach (var enemy in combatantManager.EnemyCombatants.Where(e => e != null && e.HP > 0))
        {
            var nearestPlayer = combatantManager.PlayerCombatants.Where(p => p != null && p.HP > 0)
                .OrderBy(p => Vector3.Distance(enemy.transform.position, p.transform.position))
                .FirstOrDefault();
            if (nearestPlayer != null)
            {
                Vector3 direction = (nearestPlayer.transform.position - enemy.transform.position).normalized;
                direction.y = 0;
                enemy.transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    private IEnumerator ProcessTurn((ICombatant combatant, bool isPlayer) turnInfo)
    {
        isProcessingTurn = true;

        if (combatantManager.IsBattleOver())
        {
            ShowBattleResult();
            isProcessingTurn = false;
            yield break;
        }

        var (combatant, isPlayer) = turnInfo;
        turnCount++;
        uiManager.UpdateTurnCount(turnCount);

        if (uiManager != null)
        {
            uiManager.UpdateCharacterPanels();
            if (isPlayer && combatant is Combatant player)
            {
                uiManager.HighlightCharacterPanel(player);
            }
            else
            {
                uiManager.HighlightCharacterPanel(null);
            }
        }

        // 🔹 Chỉ highlight ally
        HighlightManager.Instance.ClearAllHighlights();
        if (isPlayer && combatant is Combatant playerCombatant)
        {
            HighlightManager.Instance.ShowHighlight(playerCombatant.transform, true);
        }

        if (isPlayer)
        {
            if (combatant is Combatant player && player.HP > 0)
            {
                waitingForPlayerAction = true;
                uiManager.ShowActionPanel(player, (actionIndex) => StartCoroutine(PerformPlayerAction(player, actionIndex)));

                yield return new WaitUntil(() => !waitingForPlayerAction);
            }
            else
            {
                isProcessingTurn = false;
            }
        }
        else
        {
            if (combatant is Enemy enemy && enemy.HP > 0)
            {
                uiManager.HideActionPanel();
                yield return StartCoroutine(PerformEnemyAction(enemy, false));
            }
            else
            {
                isProcessingTurn = false;
            }
        }

       

        yield return new WaitForSeconds(1f);
        isProcessingTurn = false;
    }


    private IEnumerator PerformPlayerAction(Combatant combatant, int actionIndex)
    {
        DebugLogger.Log($"[START] Performing player action for {combatant.Name}, actionIndex: {actionIndex}");
        uiManager.HideActionPanel();
        CombatantData data = combatant.GetData();
        SkillData skill = data.Skills[actionIndex];
        ICombatant target = null;

        // Kiểm tra điều kiện sử dụng skill
        if (actionIndex == 1 && combatant.SkillCharge < 3)
        {
            DebugLogger.LogWarning($"{combatant.Name} cannot use Skill 2 yet. SkillCharge: {combatant.SkillCharge}/3");
            uiManager.ShowWarning("Không đủ Skill Charge!");
            uiManager.ShowActionPanel(combatant, index => StartCoroutine(PerformPlayerAction(combatant, index)));
            isProcessingTurn = false;
            yield break;
        }
        if (actionIndex == 2 && combatant.Mana < data.Skill3ManaCost)
        {
            DebugLogger.LogWarning($"{combatant.Name} cannot use Skill 3 yet. Mana: {combatant.Mana}/{data.Skill3ManaCost}");
            uiManager.ShowWarning("Không đủ Mana!");
            uiManager.ShowActionPanel(combatant, index => StartCoroutine(PerformPlayerAction(combatant, index)));
            isProcessingTurn = false;
            yield break;
        }

        // ========================
        // Chọn mục tiêu
        // ========================
        if (skill.IsBuff)
        {
            // --- code chọn ally giữ nguyên ---
            bool hasValidAllies = combatantManager.PlayerCombatants.Any(p => p != null && p.gameObject != null && p.HP > 0);
            if (!hasValidAllies)
            {
                DebugLogger.LogWarning("No valid allies available for buff skill.");
                uiManager.ShowWarning("Không còn đồng minh nào để sử dụng kỹ năng!");
                yield return new WaitForSeconds(1f);
                uiManager.HideTargetSelectionPrompt();
                uiManager.ShowActionPanel(combatant, index => StartCoroutine(PerformPlayerAction(combatant, index)));
                isProcessingTurn = false;
                yield break;
            }

            uiManager.ShowTargetSelectionPrompt(true);
            Combatant.SetSelectingBuffTarget(true);
            Combatant.ClearAllAlliesHighlight(combatantManager.PlayerCombatants.ToArray());
            Combatant.HighlightAllAllies(combatantManager.PlayerCombatants.ToArray());
            Combatant.ClearSelectedPlayer();
            bool isCancelled = false;

            Button cancelButton = uiManager.GetCancelButton();
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(() => isCancelled = true);
                cancelButton.gameObject.SetActive(true);
            }

            while (!isCancelled && (target == null || ((Component)target) == null || ((Component)target).gameObject == null || target.HP <= 0))
            {
                yield return new WaitUntil(() => isCancelled || Combatant.GetSelectedPlayer() != null);
                if (isCancelled)
                {
                    uiManager.ShowWarning("Hành động đã bị hủy!");
                    Combatant.ClearAllAlliesHighlight(combatantManager.PlayerCombatants.ToArray());
                    Combatant.SetSelectingBuffTarget(false);
                    uiManager.HideTargetSelectionPrompt();
                    uiManager.HideWarning();
                    if (cancelButton != null) cancelButton.gameObject.SetActive(false);

                    uiManager.ShowActionPanel(combatant, index => StartCoroutine(PerformPlayerAction(combatant, index)));

                    HighlightManager.Instance.ClearAllHighlights();
                    HighlightManager.Instance.ShowHighlight(combatant.transform, true);
                    yield break;
                }
                target = Combatant.GetSelectedPlayer();
                if (target == null || ((Component)target) == null || ((Component)target).gameObject == null || target.HP <= 0)
                {
                    uiManager.ShowWarning("Vui lòng chọn một đồng minh hợp lệ!");
                    Combatant.ClearSelectedPlayer();
                }
                else
                {
                    uiManager.HideWarning();
                    uiManager.SetTargetText($"Mục tiêu: {target.Name}");
                    foreach (var ally in combatantManager.PlayerCombatants)
                    {
                        if (ally != null && ally != target)
                            ally.SetHighlight(false);
                    }
                    (target as Combatant).SetHighlight(true);
                }
            }
            Combatant.SetSelectingBuffTarget(false);
            uiManager.HideTargetSelectionPrompt();
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
        }
        else
        {
            // --- Chọn enemy (đã fix null) ---
            bool hasValidEnemies = combatantManager.EnemyCombatants.Any(e => e != null && e.HP > 0);
            if (!hasValidEnemies)
            {
                DebugLogger.LogWarning("No valid enemies available for attack.");
                uiManager.ShowWarning("Không còn kẻ thù nào để tấn công!");
                yield return new WaitForSeconds(1f);
                uiManager.HideTargetSelectionPrompt();
                uiManager.ShowActionPanel(combatant, index => StartCoroutine(PerformPlayerAction(combatant, index)));
                isProcessingTurn = false;
                yield break;
            }

            ICombatant newlySelected = Enemy.GetSelectedEnemy();

            if (newlySelected != null && ((Component)newlySelected) != null && ((Component)newlySelected).gameObject != null && newlySelected.HP > 0)
            {
                persistentTarget = newlySelected;
                target = persistentTarget;
            }
            else if (persistentTarget != null && ((Component)persistentTarget) != null && ((Component)persistentTarget).gameObject != null && persistentTarget.HP > 0)
            {
                target = persistentTarget;
            }
            else
            {
                persistentTarget = null;
                uiManager.ShowTargetSelectionPrompt(false);
                uiManager.ShowWarning("Please select a valid enemy!");
                Enemy.HighlightAllEnemies(combatantManager.EnemyCombatants.ToArray());
                Enemy.ClearSelectedEnemy();

                bool isCancelled = false;
                Button cancelButton = uiManager.GetCancelButton();
                if (cancelButton != null)
                {
                    cancelButton.onClick.RemoveAllListeners();
                    cancelButton.onClick.AddListener(() => isCancelled = true);
                    cancelButton.gameObject.SetActive(true);
                }

                while (!isCancelled && (target == null || ((Component)target) == null || ((Component)target).gameObject == null || target.HP <= 0))
                {
                    yield return new WaitUntil(() => isCancelled || Enemy.GetSelectedEnemy() != null);
                    if (isCancelled)
                    {
                        uiManager.ShowWarning("Hành động đã bị hủy!");
                        Enemy.ClearAllEnemiesHighlight(combatantManager.EnemyCombatants.ToArray());
                        uiManager.HideTargetSelectionPrompt();
                        uiManager.HideWarning();
                        if (cancelButton != null) cancelButton.gameObject.SetActive(false);
                        uiManager.ShowActionPanel(combatant, index => StartCoroutine(PerformPlayerAction(combatant, index)));
                        yield return new WaitForSeconds(0.5f);
                        isProcessingTurn = false;
                        yield break;
                    }
                    target = Enemy.GetSelectedEnemy();
                    if (target != null && ((Component)target) != null && ((Component)target).gameObject != null && target.HP > 0)
                    {
                        persistentTarget = target;
                    }
                    else
                    {
                        uiManager.ShowWarning("Please select a valid enemy!");
                        Enemy.ClearSelectedEnemy();
                    }
                }

                uiManager.HideTargetSelectionPrompt();
                if (cancelButton != null) cancelButton.gameObject.SetActive(false);
            }

            if (target != null && ((Component)target) != null && ((Component)target).gameObject != null && target.HP > 0)
            {
                uiManager.SetTargetText($"Mục tiêu: {target.Name}");
                Enemy.ClearAllEnemiesHighlight(combatantManager.EnemyCombatants.ToArray());
                (target as Enemy).SetHighlight(true);
            }
            else
            {
                persistentTarget = null; // clear target invalid để tránh crash
            }
        }

        // ========================
        // Thực hiện tấn công (phần dưới giữ nguyên)
        // ========================
        Vector3 originalPosition = combatant.transform.position;
        bool hasMovementTag = skill.HasMovementTag;
        float moveDistance = hasMovementTag ? skill.MovementDistance : (data.AttackRange == AttackRange.Melee ? 0f : 0f);

        if (hasMovementTag || data.AttackRange == AttackRange.Melee)
        {
            yield return StartCoroutine(movementManager.MoveForAction(combatant.transform, ((Component)target).transform, combatant.AttackType, moveDistance, false, null, hasMovementTag, useRunAnimation: false));
        }

        var (damage, skillName, isAoE, _, maxAoETargets, aoEDamageReduction) = combatant.CalculateDamage(actionIndex, target);
        float comboMultiplier = CheckCombo(combatant.Name, skillName);
        damage = Mathf.RoundToInt(damage * comboMultiplier);
        bool isCritical = data != null && Random.value < data.CritRate;

        // Effect + Sound giữ nguyên
        if (skill.EffectPrefab != null)
        {
            Vector3 effectPosition;
            Vector3 targetEffectPosition = ((Component)target).transform.position + Vector3.up * skill.EffectHeightOffset;
            if (data.AttackRange == AttackRange.Melee)
                effectPosition = Vector3.Lerp(combatant.transform.position, ((Component)target).transform.position, skill.EffectDistanceOffset) + Vector3.up * skill.EffectHeightOffset;
            else if (skill.EffectType == EffectType.Projectile)
                effectPosition = combatant.transform.position + Vector3.up * skill.EffectHeightOffset;
            else if (skill.EffectType == EffectType.FromAbove)
                effectPosition = ((Component)target).transform.position + Vector3.up * (skill.EffectHeightOffset + skill.EffectAboveHeight);
            else
                effectPosition = targetEffectPosition;

            GameObject effectObj = objectPool.GetObject(skill.EffectPrefab.name, effectPosition, Quaternion.identity);
            if (effectObj != null)
            {
                if (skill.EffectType == EffectType.Projectile || skill.EffectType == EffectType.FromAbove)
                {
                    EffectMover mover = effectObj.GetComponent<EffectMover>();
                    if (mover == null) mover = effectObj.AddComponent<EffectMover>();
                    mover.Initialize(targetEffectPosition, 10f);
                }
                ParticleSystem particleSystem = effectObj.GetComponent<ParticleSystem>();
                float duration = particleSystem != null ? particleSystem.main.duration : 0f;
                StartCoroutine(ReturnEffectAfterDuration(effectObj, duration));
            }
        }

        if (skill.SoundClip != null)
            AudioSource.PlayClipAtPoint(skill.SoundClip, combatant.transform.position);

        combatant.SetAttacking(actionIndex);

        float animationDuration = GetAnimationDuration(combatant, actionIndex switch
        {
            0 => "Attack1",
            1 => "Attack2",
            2 => "Attack3",
            _ => "Attack1"
        });
        float hitDelay = animationDuration * 0.3f;
        yield return new WaitForSeconds(hitDelay);

        if (skill.IsBuff)
        {
            ApplyStatusEffect(target, skill, combatant);
            if (skill.IsAoE)
            {
                var secondaryTargets = combatantManager.PlayerCombatants
                    .Where(p => p != null && p != target && p.HP > 0)
                    .OrderBy(p => Vector3.Distance(((Component)target).transform.position, ((Component)p).transform.position))
                    .Take(skill.MaxAoETargets - 1)
                    .ToList();

                foreach (var secondaryTarget in secondaryTargets)
                {
                    ApplyStatusEffect(secondaryTarget, skill, combatant);
                    if (skill.EffectPrefab != null)
                    {
                        Vector3 secondaryEffectPosition = ((Component)secondaryTarget).transform.position + Vector3.up * skill.EffectHeightOffset;
                        GameObject effectObj = objectPool.GetObject(skill.EffectPrefab.name, secondaryEffectPosition, Quaternion.identity);
                        if (effectObj != null)
                        {
                            ParticleSystem particleSystem = effectObj.GetComponent<ParticleSystem>();
                            float duration = particleSystem != null ? particleSystem.main.duration : 0f;
                            StartCoroutine(ReturnEffectAfterDuration(effectObj, duration));
                        }
                    }
                }
            }
        }
        else
        {
            ApplyDamageAndEffects(combatant, target, damage, skillName, isCritical, 0f);
            if (skill.StatusEffect != StatusEffect.None)
                ApplyStatusEffect(target, skill, combatant);

            if (isAoE)
            {
                var secondaryTargets = combatantManager.EnemyCombatants
                    .Where(e => e != null && e != target && e.HP > 0)
                    .OrderBy(e => Vector3.Distance(((Component)target).transform.position, ((Component)e).transform.position))
                    .Take(maxAoETargets - 1)
                    .ToList();

                foreach (var secondaryTarget in secondaryTargets)
                {
                    var (secondaryDamage, _, _, _, _, _) = combatant.CalculateDamage(actionIndex, secondaryTarget, false);
                    ApplyDamageAndEffects(combatant, secondaryTarget, secondaryDamage, skillName, isCritical, 0f);
                    if (skill.StatusEffect != StatusEffect.None)
                        ApplyStatusEffect(secondaryTarget, skill, combatant);

                    if (skill.EffectPrefab != null)
                    {
                        Vector3 secondaryEffectPosition = ((Component)secondaryTarget).transform.position + Vector3.up * skill.EffectHeightOffset;
                        GameObject effectObj = objectPool.GetObject(skill.EffectPrefab.name, secondaryEffectPosition, Quaternion.identity);
                        if (effectObj != null)
                        {
                            ParticleSystem particleSystem = effectObj.GetComponent<ParticleSystem>();
                            float duration = particleSystem != null ? particleSystem.main.duration : 0f;
                            StartCoroutine(ReturnEffectAfterDuration(effectObj, duration));
                        }
                    }
                }
            }
        }

        yield return new WaitForSeconds(animationDuration - hitDelay);
        combatant.ResetAttacking();

        if (data.AttackRange == AttackRange.Melee)
        {
            var nearestEnemy = combatantManager.EnemyCombatants
                .Where(e => e != null && e.HP > 0)
                .OrderBy(e => Vector3.Distance(combatant.transform.position, ((Component)e).transform.position))
                .FirstOrDefault();
            if (nearestEnemy != null)
            {
                yield return StartCoroutine(movementManager.MoveForAction(combatant.transform, ((Component)nearestEnemy).transform, combatant.AttackType, 0f, true, originalPosition, false, useRunAnimation: false));
            }
        }

        if (actionIndex == 0)
        {
            combatant.GainMana(20);
            combatant.GainSkillCharge(1);
        }
        else if (actionIndex == 1)
        {
            combatant.GainMana(20);
            combatant.ResetSkillCharge();
        }
        else if (actionIndex == 2)
        {
            combatant.ResetMana();
        }

        DebugLogger.Log($"{combatant.Name} gains 20 mana from attacking. Current Mana: {combatant.Mana}");

        // Clear ally thôi, enemy giữ persistentTarget
        if (skill.IsBuff)
        {
            Combatant.ClearSelectedPlayer();
            Combatant.ClearAllAlliesHighlight(combatantManager.PlayerCombatants.ToArray());
        }

        if (uiManager != null)
            uiManager.UpdateCharacterPanels();

        yield return new WaitForSeconds(0.5f);
        DebugLogger.Log($"[END] Player action completed for {combatant.Name}");
        waitingForPlayerAction = false;
        isProcessingTurn = false;
        HighlightManager.Instance.ClearAllHighlights();
    }

    private IEnumerator PerformEnemyAction(Enemy enemy, bool isPrimaryTarget = true)
    {
        DebugLogger.Log($"[START] Performing enemy action for {enemy.Name}");
        int actionIndex = DetermineEnemyAction(enemy);
        EnemyData data = enemy.GetData();
        SkillData skill = data.Skills[actionIndex];
        ICombatant target = null;

        if (skill.IsBuff)
        {
            target = combatantManager.EnemyCombatants.Where(e => e != null && e.HP > 0).OrderBy(e => e.HP).FirstOrDefault();
        }
        else
        {
            target = combatantManager.PlayerCombatants.Where(p => p != null && p.HP > 0).OrderBy(p => p.HP).FirstOrDefault();
        }

        if (target == null || ((Component)target).gameObject == null || target.HP <= 0)
        {
            DebugLogger.LogWarning("No valid target found or target is dead.");
            isProcessingTurn = false;
            yield break;
        }

        Vector3 originalPosition = enemy.transform.position;
        bool hasMovementTag = skill.HasMovementTag;
        float moveDistance = hasMovementTag ? skill.MovementDistance : (data.AttackRange == AttackRange.Melee ? 0f : 0f);

        if (hasMovementTag || data.AttackRange == AttackRange.Melee)
        {
            yield return StartCoroutine(movementManager.MoveForAction(enemy.transform, ((Component)target).transform, enemy.AttackType, moveDistance, false, null, hasMovementTag, useRunAnimation: false));
        }

        var (damage, skillName, isAoE, _, maxAoETargets, aoEDamageReduction) = enemy.CalculateDamage(actionIndex, target);
        bool isCritical = Random.value < 0.05f;

        if (skill.EffectPrefab != null)
        {
            Vector3 effectPosition;
            Vector3 targetEffectPosition = ((Component)target).transform.position + Vector3.up * skill.EffectHeightOffset;
            if (data.AttackRange == AttackRange.Melee)
            {
                effectPosition = Vector3.Lerp(enemy.transform.position, ((Component)target).transform.position, skill.EffectDistanceOffset) + Vector3.up * skill.EffectHeightOffset;
            }
            else
            {
                if (skill.EffectType == EffectType.Projectile)
                {
                    effectPosition = enemy.transform.position + Vector3.up * skill.EffectHeightOffset;
                }
                else if (skill.EffectType == EffectType.FromAbove)
                {
                    effectPosition = ((Component)target).transform.position + Vector3.up * (skill.EffectHeightOffset + skill.EffectAboveHeight);
                }
                else
                {
                    effectPosition = targetEffectPosition;
                }
            }

            GameObject effectObj = objectPool.GetObject(skill.EffectPrefab.name, effectPosition, Quaternion.identity);
            if (effectObj == null)
            {
                DebugLogger.LogWarning($"[Effect] Failed to get EffectPrefab {skill.EffectPrefab.name} from object pool for main target.");
            }
            else
            {
                if (skill.EffectType == EffectType.Projectile || skill.EffectType == EffectType.FromAbove)
                {
                    EffectMover mover = effectObj.GetComponent<EffectMover>();
                    if (mover == null) mover = effectObj.AddComponent<EffectMover>();
                    mover.Initialize(targetEffectPosition, 10f);
                }
                ParticleSystem particleSystem = effectObj.GetComponent<ParticleSystem>();
                float duration = particleSystem != null ? particleSystem.main.duration : 0f;
                StartCoroutine(ReturnEffectAfterDuration(effectObj, duration));
            }
        }
        else
        {
            DebugLogger.LogWarning($"No EffectPrefab assigned for skill {skill.SkillName}");
        }

        if (skill.SoundClip != null)
        {
            AudioSource.PlayClipAtPoint(skill.SoundClip, enemy.transform.position);
        }
        else
        {
            DebugLogger.LogWarning($"No SoundClip assigned for skill {skill.SkillName}");
        }

        enemy.SetAttacking(actionIndex);

        // Đợi một phần animation attack (50% thời gian) trước khi apply damage và trigger hit
        float animationDuration = GetAnimationDuration(enemy, actionIndex switch
        {
            0 => "Attack1",
            1 => "Attack2",
            2 => "Attack3",
            _ => "Attack1"
        });
        float hitDelay = animationDuration * 0.5f; // Trigger hit ở giữa animation attack
        yield return new WaitForSeconds(hitDelay);

        // Apply damage và trigger animation "Hit" cho mục tiêu
        if (skill.IsBuff)
        {
            ApplyDamageAndEffects(enemy, target, damage, skillName, isCritical, 0f);
            if (skill.StatusEffect != StatusEffect.None)
            {
                ApplyStatusEffect(target, skill, enemy);
                if (skill.IsAoE)
                {
                    var secondaryTargets = combatantManager.EnemyCombatants
                        .Where(e => e != null && e != target && e.HP > 0)
                        .OrderBy(e => Vector3.Distance(((Component)target).transform.position, ((Component)e).transform.position))
                        .Take(skill.MaxAoETargets - 1)
                        .ToList();

                    foreach (var secondaryTarget in secondaryTargets)
                    {
                        var (secondaryDamage, _, _, _, _, _) = enemy.CalculateDamage(actionIndex, secondaryTarget, false);
                        ApplyDamageAndEffects(enemy, secondaryTarget, secondaryDamage, skillName, isCritical, 0f);
                        ApplyStatusEffect(secondaryTarget, skill, enemy);
                        if (skill.EffectPrefab != null)
                        {
                            Vector3 secondaryEffectPosition = ((Component)secondaryTarget).transform.position + Vector3.up * skill.EffectHeightOffset;
                            GameObject effectObj = objectPool.GetObject(skill.EffectPrefab.name, secondaryEffectPosition, Quaternion.identity);
                            if (effectObj == null)
                            {
                                DebugLogger.LogWarning($"[Effect] Failed to get EffectPrefab {skill.EffectPrefab.name} from object pool for secondary target {secondaryTarget.Name}.");
                            }
                            else
                            {
                                ParticleSystem particleSystem = effectObj.GetComponent<ParticleSystem>();
                                float duration = particleSystem != null ? particleSystem.main.duration : 0f;
                                StartCoroutine(ReturnEffectAfterDuration(effectObj, duration));
                            }
                        }
                    }
                }
            }
        }
        else
        {
            ApplyDamageAndEffects(enemy, target, damage, skillName, isCritical, 0f);
            if (skill.StatusEffect != StatusEffect.None)
            {
                ApplyStatusEffect(target, skill, enemy);
            }
            if (skill.IsAoE)
            {
                var secondaryTargets = combatantManager.PlayerCombatants
                    .Where(p => p != null && p != target && p.HP > 0)
                    .OrderBy(p => Vector3.Distance(((Component)target).transform.position, ((Component)p).transform.position))
                    .Take(skill.MaxAoETargets - 1)
                    .ToList();

                foreach (var secondaryTarget in secondaryTargets)
                {
                    var (secondaryDamage, _, _, _, _, _) = enemy.CalculateDamage(actionIndex, secondaryTarget, false);
                    ApplyDamageAndEffects(enemy, secondaryTarget, secondaryDamage, skillName, isCritical, 0f);
                    if (skill.StatusEffect != StatusEffect.None)
                    {
                        ApplyStatusEffect(secondaryTarget, skill, enemy);
                    }
                    if (skill.EffectPrefab != null)
                    {
                        Vector3 secondaryEffectPosition = ((Component)secondaryTarget).transform.position + Vector3.up * skill.EffectHeightOffset;
                        GameObject effectObj = objectPool.GetObject(skill.EffectPrefab.name, secondaryEffectPosition, Quaternion.identity);
                        if (effectObj == null)
                        {
                            DebugLogger.LogWarning($"[Effect] Failed to get EffectPrefab {skill.EffectPrefab.name} from object pool for secondary target {secondaryTarget.Name}.");
                        }
                        else
                        {
                            ParticleSystem particleSystem = effectObj.GetComponent<ParticleSystem>();
                            float duration = particleSystem != null ? particleSystem.main.duration : 0f;
                            StartCoroutine(ReturnEffectAfterDuration(effectObj, duration));
                        }
                    }
                }
            }
        }

        // Đợi phần còn lại của animation attack
        yield return new WaitForSeconds(animationDuration - hitDelay);

        enemy.ResetAttacking();

        // Cập nhật mana sau khi hành động
        UpdateEnemyResources(enemy, actionIndex);
        DebugLogger.Log($"{enemy.Name} gains 20 mana from attacking. Current Mana: {enemy.Mana}");

        if (data.AttackRange == AttackRange.Melee)
        {
            var nearestPlayer = combatantManager.PlayerCombatants
                .Where(p => p != null && p.HP > 0)
                .OrderBy(p => Vector3.Distance(enemy.transform.position, ((Component)p).transform.position))
                .FirstOrDefault();
            if (nearestPlayer != null)
            {
                yield return StartCoroutine(movementManager.MoveForAction(enemy.transform, ((Component)nearestPlayer).transform, enemy.AttackType, 0f, true, originalPosition, false, useRunAnimation: false));
            }
            else
            {
                DebugLogger.LogWarning($"No valid target found for rotation after return for {enemy.Name}. Skipping return movement.");
            }
        }

        if (uiManager != null)
        {
            uiManager.UpdateCharacterPanels();
        }
        yield return new WaitForSeconds(0.5f);
        DebugLogger.Log($"[END] Enemy action completed for {enemy.Name}");
        isProcessingTurn = false;
    }

    private IEnumerator ReturnEffectAfterDuration(GameObject effectObj, float duration)
    {
        if (effectObj == null)
        {
            DebugLogger.LogWarning("[Effect] EffectObj is null before waiting, skipping return.");
            yield break;
        }

        float minimumDuration = 2f; // Thời gian chờ tối thiểu là 2 giây
        string effectName = effectObj.name; // Lưu tên trước để tránh truy cập sau khi bị hủy
        DebugLogger.Log($"[Effect] Waiting to return {effectName} after {Mathf.Max(duration, minimumDuration)} seconds");
        yield return new WaitForSeconds(Mathf.Max(duration, minimumDuration));

        if (effectObj == null)
        {
            //DebugLogger.LogWarning($"[Effect] EffectObj {effectName} was destroyed before returning to pool.");
            yield break;
        }

        DebugLogger.Log($"[Effect] Returning {effectName} to pool");
        objectPool.ReturnObject(effectObj);
    }

    private void ApplyDamageAndEffects(ICombatant attacker, ICombatant target, int damage, string skillName, bool isCritical, float slowChance)
    {
        if (target == null || ((Component)target).gameObject == null) return;

        // Lấy thuộc tính của attacker và target
        ElementType attackerElement = attacker is Combatant combatant1 ? combatant1.GetData().Element : (attacker as Enemy).GetData().Element;
        ElementType targetElement = target is Combatant combatant2 ? combatant2.GetData().Element : (target as Enemy).GetData().Element;

        // Lấy elemental modifier từ hàm GetElementalModifier của attacker
        float elementalModifier = attacker is Combatant combatant3 ? combatant3.GetElementalModifier(attackerElement, targetElement) : (attacker as Enemy).GetElementalModifier(attackerElement, targetElement);

        // Áp dụng sát thương
        target.TakeDamage(damage);

        // Hiển thị "Weakness" nếu đánh trúng điểm yếu (elementalModifier > 1f)
        if (damage > 0 && elementalModifier > 1f)
        {
            uiManager.ShowWeaknessText();
            DebugLogger.Log($"<color=#FFFF00>[WEAKNESS] {attacker.Name} hits {target.Name}'s weakness!</color>");
        }

        // Kích hoạt animation "Hit" cho mục tiêu nếu còn sống
        if (damage > 0 && target.HP > 0)
        {
            Animator targetAnimator = (target as Component)?.GetComponent<Animator>();
            if (targetAnimator != null)
            {
                targetAnimator.SetTrigger("Hit");
            }
        }

        // Hồi 10 mana cho target khi bị đánh
        try
        {
            if (target is Combatant combatant4)
            {
                combatant4.GainMana(10);
                DebugLogger.Log($"{combatant4.Name} gains 10 mana from being hit. Current Mana: {combatant4.Mana}");
            }
            else if (target is Enemy enemy)
            {
                enemy.GainMana(10);
                DebugLogger.Log($"{enemy.Name} gains 10 mana from being hit. Current Mana: {enemy.Mana}");
            }
        }
        catch (System.Exception e)
        {
            DebugLogger.LogError($"Failed to apply mana gain for {target?.Name ?? "Unknown"}: {e.Message}");
        }

        battleHistory.Add((attacker.Name, skillName, damage));

        if (slowChance > 0 && Random.value < slowChance)
        {
            float slowAmount = attacker is Combatant combatant5 ? combatant5.GetData().SlowAmount : (attacker as Enemy).GetData().SlowAmount;
            int slowDuration = attacker is Combatant combatant6 ? combatant6.GetData().SlowDuration : (attacker as Enemy).GetData().SlowDuration;
            target.ApplySlow(slowAmount, slowDuration);
            string color = attacker is Combatant ? "#00B7EB" : "#FF0000";
            DebugLogger.Log($"<color={color}>[BUFF] {attacker.Name} → <color={(attacker is Combatant ? "#FF0000" : "#00B7EB")}>{target.Name}</color>: Slow (Amount: {slowAmount}, Duration: {slowDuration} turns)</color>");
        }
    }

    private void ApplyStatusEffect(ICombatant target, SkillData skill, ICombatant attacker)
    {
        if (target == null || ((Component)target).gameObject == null) return;
        string color = attacker is Combatant ? "#00B7EB" : "#FF0000";
        string targetColor = attacker is Combatant ? "#FF0000" : "#00B7EB";

        switch (skill.StatusEffect)
        {
            case StatusEffect.Freeze:
                target.ApplySlow(1f, skill.StatusEffectDuration);
                DebugLogger.Log($"<color={color}>[BUFF] {attacker.Name} → <color={targetColor}>{target.Name}</color>: Freeze (Duration: {skill.StatusEffectDuration} turns)</color>");
                break;
            case StatusEffect.Burn:
                DebugLogger.Log($"<color={color}>[BUFF] {attacker.Name} → <color={targetColor}>{target.Name}</color>: Burn (Duration: {skill.StatusEffectDuration} turns)</color>");
                break;
            case StatusEffect.Heal:
                int targetMaxHP = target is Combatant c ? c.GetData().MaxHP : (target as Enemy).GetData().MaxHP;
                int healAmount = Mathf.RoundToInt(targetMaxHP * skill.HealMultiplier); // Change to % of target's MaxHP
                target.TakeDamage(-healAmount);
                DebugLogger.Log($"<color={color}>[BUFF] {attacker.Name} → <color={targetColor}>{target.Name}</color>: Heal ({healAmount} HP)</color>");
                break;
            case StatusEffect.Shield:
                // Giả sử có logic xử lý khiên (có thể thêm biến Shield vào ICombatant sau)
                DebugLogger.Log($"<color={color}>[BUFF] {attacker.Name} → <color={targetColor}>{target.Name}</color>: Shield ({skill.ShieldAmount} HP, Duration: {skill.StatusEffectDuration} turns)</color>");
                break;
            case StatusEffect.Panic:
                DebugLogger.Log($"<color={color}>[BUFF] {attacker.Name} → <color={targetColor}>{target.Name}</color>: Panic (Duration: {skill.StatusEffectDuration} turns)</color>");
                break;
            case StatusEffect.Slow:
                target.ApplySlow(skill.SlowAmount, skill.StatusEffectDuration);
                DebugLogger.Log($"<color={color}>[BUFF] {attacker.Name} → <color={targetColor}>{target.Name}</color>: Slow (Amount: {skill.SlowAmount}, Duration: {skill.StatusEffectDuration} turns)</color>");
                break;
            case StatusEffect.None:
                float advanceAmount = 10000f / target.SPD * 0.3f;
                target.AdvanceActionValue(-advanceAmount);
                DebugLogger.Log($"<color={color}>[BUFF] {attacker.Name} → <color={targetColor}>{target.Name}</color>: Advance Forward (+2 Turns)</color>");
                break;
        }
    }

    private int DetermineEnemyAction(Enemy enemy)
    {
        EnemyData data = enemy.GetData();
        // Ưu tiên skill 3 tuyệt đối nếu mana đầy
        if (enemy.Mana >= data.Skill3ManaCost)
        {
            DebugLogger.Log($"{enemy.Name} chooses Skill 3 (Mana: {enemy.Mana}/{data.Skill3ManaCost})");
            return 2;
        }
        // Skill 2 không yêu cầu mana, chỉ dựa trên xác suất
        if (Random.value <= data.Skill2Chance)
        {
            DebugLogger.Log($"{enemy.Name} chooses Skill 2");
            return 1;
        }
        // Mặc định dùng skill 0
        DebugLogger.Log($"{enemy.Name} chooses Skill 0");
        return 0;
    }

    private void UpdateEnemyResources(Enemy enemy, int actionIndex)
    {
        EnemyData data = enemy.GetData();
        if (actionIndex == 0 || actionIndex == 1) // Skill 0 và Skill 2 đều là đánh thường
        {
            enemy.GainMana(30);
            enemy.GainEnergy(10);
        }
        else if (actionIndex == 2)
        {
            enemy.ResetMana();
        }
    }

    private float CheckCombo(string characterName, string skillName)
    {
        if (battleHistory.Count > 0 && battleHistory[battleHistory.Count - 1].characterName == "Astra" &&
            battleHistory[battleHistory.Count - 1].skillName == "EntropicWave" &&
            characterName == "Hugo" && skillName == "Cataclysmic Blow")
        {
            return 1.2f;
        }
        return 1f;
    }

    private void ShowBattleResult()
    {
        bool isVictory = combatantManager.EnemyCombatants.All(e => e == null || e.HP <= 0);
        victory = isVictory;

        if (teamData.CurrentBattleType == BattleType.Map)
        {
            if (isVictory)
            {
                if (mapProgress != null)
                {
                    // đánh dấu quái chết
                    PlayerPrefs.SetInt($"EnemyDead_W{mapProgress.CurrentWave}_E{mapProgress.CurrentEnemyIndex}", 1);

                    mapProgress.CurrentEnemyIndex++;

                    // xử lý wave completed
                    bool waveCompleted = mapProgress.CurrentEnemyIndex >= 10; // EnemiesPerWave
                    if (waveCompleted)
                    {
                        mapProgress.CurrentWave++;
                        mapProgress.CurrentEnemyIndex = 0;
                        PlayerPrefs.SetInt("WaveCompleted", 1);

                        PlayerPrefs.SetInt("LastCompletedWaveDisplay", mapProgress.CurrentWave);
                        PlayerPrefs.Save();
                    }
                    else
                    {
                        PlayerPrefs.SetInt("WaveCompleted", 0);
                    }

                    // Save progress để persist
                    mapProgress.Save();
                    PlayerPrefs.SetInt("BattleLost", 0);
                    PlayerPrefs.Save();

                    DebugLogger.Log($"[CombatManager] Saved progress after victory: Wave {mapProgress.CurrentWave}, Index {mapProgress.CurrentEnemyIndex}");
                }
            }
            else // thua
            {
                PlayerPrefs.SetInt("BattleLost", 1);
                PlayerPrefs.SetInt("WaveCompleted", 0);
                PlayerPrefs.Save();
                DebugLogger.Log("[CombatManager] Player lost. Set BattleLost=1, WaveCompleted=0, PlayerPrefs saved.");
            }
        }

        List<EquipmentData> equipmentDrops = null;
        if (isVictory && teamData.CurrentBattleType != BattleType.Dungeon)
        {
            if (rewardConfig != null)
            {
                var itemDrops = rewardConfig.RollRewards().OfType<EquipmentData>().ToList();
                equipmentDrops = itemDrops;
                DebugLogger.Log($"[CombatManager] Rewards rolled: {itemDrops.Count}");
            }
            else
            {
                DebugLogger.LogWarning("[CombatManager] RewardConfig missing!");
            }
        }

        if (resultScreenObject != null)
        {
            var resultScreenComp = resultScreenObject.GetComponent<ResultScreen>();
            if (resultScreenComp != null)
            {
                int playerTotalDamage = 0;
                int enemyTotalDamage = 0;
                var playerNames = combatantManager.PlayerCombatants.Select(p => p?.Name).ToList();

                foreach (var entry in battleHistory)
                {
                    if (playerNames.Contains(entry.characterName))
                        playerTotalDamage += entry.damage;
                    else
                        enemyTotalDamage += entry.damage;
                }

                string stonesLog = teamData.CurrentBattleType == BattleType.Dungeon && collectedStones.Count > 0
                    ? string.Join(", ", collectedStones.Select(s => s.ItemName))
                    : "None";

                DebugLogger.Log($"[RESULT] Displaying ResultScreen. Victory: {isVictory}, TurnCount: {turnCount}, PlayerTotalDamage: {playerTotalDamage}, EnemyTotalDamage: {enemyTotalDamage}, Stones: {stonesLog}");

                if (inventoryData != null)
                {
                    resultScreenComp.SetInventoryData(inventoryData);
                }

                resultScreenObject.SetActive(true);

                resultScreenComp.ShowResult(
                    isVictory,
                    playerTotalDamage,
                    enemyTotalDamage,
                    turnCount,
                    teamData.CurrentBattleType == BattleType.Dungeon ? collectedStones : null,
                    equipmentDrops
                );

                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                DebugLogger.Log("[CombatManager] Cursor reset: visible = true, lockState = None");
            }
            else
            {
                DebugLogger.LogError("ResultScreen component not found on assigned GameObject.");
                SceneManager.LoadScene("Map");
            }
        }
        else
        {
            DebugLogger.LogError("ResultScreen GameObject not assigned in CombatManager Inspector.");
            SceneManager.LoadScene("Map");
        }

        var continueBtn = resultScreenObject != null ? resultScreenObject.GetComponentInChildren<Button>() : null;
        if (continueBtn != null)
        {
            continueBtn.onClick.RemoveAllListeners();
            continueBtn.onClick.AddListener(() => { SceneManager.LoadScene("Loading"); }); // Change to Map
        }
    }


    private float GetAnimationDuration(ICombatant combatant, string animationTrigger)
    {
        Animator animator = (combatant as Component)?.GetComponent<Animator>();
        if (animator == null)
        {
            DebugLogger.LogWarning($"No Animator found for {combatant.Name}, using default duration 1f");
            return 1f;
        }
        var controller = animator.runtimeAnimatorController;
        if (controller == null)
        {
            DebugLogger.LogWarning($"No AnimatorController found for {combatant.Name}, using default duration 1f");
            return 1f;
        }
        foreach (var clip in controller.animationClips)
        {
            if (clip != null && clip.name.Contains(animationTrigger))
            {
                return clip.length;
            }
        }
        DebugLogger.LogWarning($"No animation clip found for trigger {animationTrigger} in {combatant.Name}, using default duration 1f");
        return 1f;
    }


}