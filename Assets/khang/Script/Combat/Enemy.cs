using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Enemy : MonoBehaviour, ICombatant
{
    [SerializeField] private EnemyData data;
    [SerializeField] private Animator animator;
    private int hp;
    private int mana;
    private int energy;
    private float actionValue;
    private float slowAmount;
    private int slowTurnsRemaining;
    private static Enemy selectedEnemy;
    private GameObject healthBarInstance;
    private Image healthBarFill;
    private TextMeshProUGUI healthBarText;
    private HealthBarSmooth healthBarSmooth;
    private ObjectPool objectPool;
    private const float DEF_COEFFICIENT = 1.0f;

    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                DebugLogger.LogError("Animator not found on " + gameObject.name);
            }
        }
    }

    void Start()
    {
        SetIdle();
        if (SceneManager.GetActiveScene().name == "BattleScene" && healthBarInstance == null)
        {
            InitializeHealthBar();
        }
    }

    void OnMouseDown()
    {
        if (HP > 0 && SceneManager.GetActiveScene().name == "BattleScene")
        {
            Combatant.ClearSelectedPlayer();
            if (selectedEnemy != null && selectedEnemy != this)
            {
                selectedEnemy.SetHighlight(false);
            }
            selectedEnemy = this;
            SetHighlight(true);
            UIManager uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowCombatantInfo(this); // Gọi panel chung

            }
            else
            {
                DebugLogger.LogError("UIManager not found in scene.");
            }
        }
    }

    public string Name => data?.Name ?? "Unknown";
    public int HP
    {
        get => hp;
        set
        {
            hp = Mathf.Clamp(value, 0, data?.MaxHP ?? 1000);
            UpdateHealthBar();
            if (hp <= 0 && animator != null)
            {
                animator.SetBool("IsDead", true);
                SetHighlight(false);
                if (healthBarInstance != null)
                {
                    objectPool.ReturnObject(healthBarInstance);
                    healthBarInstance = null;
                }
                if (SceneManager.GetActiveScene().name == "BattleScene")
                {
                    UIManager uiManager = FindFirstObjectByType<UIManager>();
                    if (uiManager != null)
                    {
                        uiManager.HideCombatantInfo(); // Ẩn panel chung
                    }
                    StartCoroutine(DelayedHideOrDestroy());
                }
            }
        }
    }
    public int Mana
    {
        get => mana;
        set => mana = Mathf.Clamp(value, 0, 1000);
    }
    public int Energy
    {
        get => energy;
        set => energy = Mathf.Clamp(value, 0, 100);
    }
    public int SPD => data?.SPD ?? 100;
    public float ActionValue
    {
        get => actionValue;
        set => actionValue = value;
    }
    public AttackType AttackType => data?.AttackType ?? AttackType.Melee;

    public void SetData(EnemyData d)
    {
        data = d;
        HP = data.MaxHP;
        Mana = 0;
        Energy = 0;
        actionValue = 0f;
        SetIdle();
        if (SceneManager.GetActiveScene().name == "BattleScene" && healthBarInstance == null)
        {
            InitializeHealthBar();
        }
    }

    private void InitializeHealthBar()
    {
        if (data == null || data.HealthBarPrefab == null)
        {
            DebugLogger.LogWarning($"HealthBarPrefab not assigned for {data?.Name ?? "Unknown"}");
            return;
        }

        if (healthBarInstance != null)
        {
            DebugLogger.LogWarning($"HealthBar already initialized for {data.Name}. Skipping initialization.");
            return;
        }

        var combatManager = FindFirstObjectByType<CombatManager>();
        if (combatManager != null)
        {
            objectPool = combatManager.GetComponent<ObjectPool>();
            if (objectPool == null)
            {
                DebugLogger.LogError("ObjectPool component not found on CombatManager in scene " + SceneManager.GetActiveScene().name);
                return;
            }
        }
        else
        {
            DebugLogger.LogError("CombatManager not found in scene " + SceneManager.GetActiveScene().name);
            return;
        }

        // Calculate initial position
        Vector3 initialPosition = transform.position + data.HealthBarOffset;

        healthBarInstance = objectPool.GetObject(data.HealthBarPrefab.name, initialPosition, Quaternion.identity, null);  // Initially no parent

        if (healthBarInstance == null)
        {
            DebugLogger.LogError($"Failed to get HealthBar from ObjectPool for {data.Name}");
            return;
        }

        healthBarFill = healthBarInstance.GetComponentInChildren<Image>();
        healthBarText = healthBarInstance.GetComponentInChildren<TextMeshProUGUI>();
        healthBarSmooth = healthBarInstance.GetComponentInChildren<HealthBarSmooth>();

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = 1f;
        }
        if (healthBarText != null)
        {
            healthBarText.text = $"{data.Name} ({HP}/{data.MaxHP})";
        }
        if (healthBarSmooth != null)
        {
            healthBarSmooth.UpdateHealth((float)HP / data.MaxHP);
        }

        // Set up UIAnchor for health bar
        UIAnchor uiAnchor = healthBarInstance.GetComponent<UIAnchor>();
        if (uiAnchor != null)
        {
            uiAnchor.SetFollowTransform(data.IsFixedHealthBar ? null : transform); // Null for fixed, follow enemy if not fixed
        }
        else
        {
            DebugLogger.LogWarning($"UIAnchor component not found on HealthBar for {data.Name}");
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBarInstance == null || SceneManager.GetActiveScene().name != "BattleScene") return;
        if (healthBarFill != null)
        {
            if (healthBarSmooth != null)
            {
                healthBarSmooth.UpdateHealth((float)HP / data.MaxHP);
            }
            else
            {
                healthBarFill.fillAmount = (float)HP / data.MaxHP;
            }
        }
        if (healthBarText != null)
        {
            healthBarText.text = $"{data.Name} ({HP}/{data.MaxHP})";
        }
    }

    public EnemyData GetData() => data;

    public void TakeDamage(int damage)
    {
        HP -= damage;
        //if (damage > 0 && animator != null && HP > 0)
        //{
        // animator.SetTrigger("Hit");
        //}
    }

    public void ApplySlow(float amount, int duration)
    {
        slowAmount = amount;
        slowTurnsRemaining = duration;
    }

    public void UpdateStatus()
    {
        if (slowTurnsRemaining > 0)
        {
            slowTurnsRemaining--;
        }
        if (animator != null && !animator.GetBool("IsMoving") && !animator.GetBool("IsDead"))
        {
            SetIdle();
        }
    }

    public float GetElementalModifier(ElementType skillElement, ElementType targetElement)
    {
        if ((skillElement == ElementType.Wind && targetElement == ElementType.Ash) ||
            (skillElement == ElementType.Ash && targetElement == ElementType.Ice) ||
            (skillElement == ElementType.Ice && targetElement == ElementType.Tide) ||
            (skillElement == ElementType.Tide && targetElement == ElementType.Wind))
        {
            return 1.5f;
        }
        return 1f;
    }

    public (int damage, string skillName, bool isAoE, float slowChance, int maxAoETargets, float aoEDamageReduction) CalculateDamage(int actionIndex, ICombatant target, bool isPrimaryTarget = true)
    {
        if (data == null || actionIndex < 0 || actionIndex >= data.Skills.Length)
        {
            return (0, "N/A", false, 0f, 0, 0f);
        }

        SkillData skill = data.Skills[actionIndex];
        int damage = 0;
        string skillName = skill.SkillName;
        bool isAoE = skill.IsAoE;
        int maxAoETargets = skill.MaxAoETargets;
        float aoEDamageReduction = skill.AoEDamageReduction;

        if (skill.IsBuff)
        {
            if (skill.StatusEffect == StatusEffect.Heal)
            {
                damage = 0; // Remove negative damage calculation to avoid double heal
                DebugLogger.Log($"<color=#FF0000>[BUFF] {Name} uses {skill.SkillName} (Heal) on <color=#FF0000>{target.Name}</color></color>");
            }
            else if (skill.StatusEffect == StatusEffect.Shield)
            {
                DebugLogger.Log($"<color=#FF0000>[BUFF] {Name} uses {skill.SkillName} (Shield) on <color=#FF0000>{target.Name}</color></color>");
            }
            else
            {
                DebugLogger.Log($"<color=#FF0000>[BUFF] {Name} uses {skill.SkillName} on <color=#FF0000>{target.Name}</color></color>");
            }
            return (damage, skillName, isAoE, 0f, maxAoETargets, aoEDamageReduction);
        }

        float targetDEF = target is Combatant c ? c.GetData().DEF : (target as Enemy).GetData().DEF;
        float targetRES = target is Combatant c2 ? c2.GetData().RES : (target as Enemy).GetData().RES;
        float targetVulnerability = target is Combatant c3 ? c3.GetData().Vulnerability : (target as Enemy).GetData().Vulnerability;
        ElementType targetElement = target is Combatant c4 ? c4.GetData().Element : (target as Enemy).GetData().Element;

        float elementalModifier = GetElementalModifier(skill.Element, targetElement);
        float adjustedBonusDMG = data.BonusDMG + (elementalModifier > 1f ? 0.5f : 0f);
        float adjustedVulnerability = targetVulnerability + (elementalModifier > 1f ? 0.5f : 0f);
        float adjustedRES = targetRES * (elementalModifier > 1f ? 0.5f : 1f);

        if (skill.DoTMultiplier == 0 && skill.BreakBase == 0)
        {
            float critMultiplier = Random.value < 0.05f ? 1.5f : 1f;
            float defenseReduction = data.Attack / (data.Attack + targetDEF * DEF_COEFFICIENT);
            damage = Mathf.RoundToInt(
                (data.Attack * skill.DamageMultiplier) *
                critMultiplier *
                (1f + adjustedBonusDMG) *
                (1f + adjustedVulnerability) *
                (1f - adjustedRES) *
                defenseReduction *
                (isAoE && !isPrimaryTarget ? 1f - aoEDamageReduction : 1f)
            );
            DebugLogger.Log($"<color=#FF0000>[ACTION] {Name} uses {skill.SkillName} on <color=#00B7EB>{target.Name}</color></color>");
            DebugLogger.Log($"<color=#FF0000>[DAMAGE] Direct Damage: {Name} → <color=#00B7EB>{target.Name}</color>: {damage} {(critMultiplier > 1f ? "(CRIT!)" : "")} (Elemental Modifier: {elementalModifier}, Defense Reduction: {defenseReduction:F3}, AOE Reduction: {(isAoE && !isPrimaryTarget ? aoEDamageReduction : 0f)})</color>");
        }
        else if (skill.DoTMultiplier > 0)
        {
            damage = Mathf.RoundToInt(
                data.Attack *
                skill.DoTMultiplier *
                (1f + adjustedBonusDMG) *
                (1f + adjustedVulnerability) *
                (1f - adjustedRES) *
                (isAoE && !isPrimaryTarget ? 1f - aoEDamageReduction : 1f)
            );
            DebugLogger.Log($"<color=#FF0000>[ACTION] {Name} uses {skill.SkillName} (DoT) on <color=#00B7EB>{target.Name}</color></color>");
            DebugLogger.Log($"<color=#FF0000>[DAMAGE] DoT Damage: {Name} → <color=#00B7EB>{target.Name}</color>: {damage} (Elemental Modifier: {elementalModifier}, AOE Reduction: {(isAoE && !isPrimaryTarget ? aoEDamageReduction : 0f)})</color>");
        }
        else if (skill.BreakBase > 0)
        {
            float levelMultiplier = 1f + (data.Level * 0.05f);
            damage = Mathf.RoundToInt(
                (skill.BreakBase * levelMultiplier + data.Attack * skill.BreakScaling) *
                skill.WeaknessTypeBonus *
                elementalModifier *
                (isAoE && !isPrimaryTarget ? 1f - aoEDamageReduction : 1f)
            );
            DebugLogger.Log($"<color=#FF0000>[ACTION] {Name} uses {skill.SkillName} (Break) on <color=#00B7EB>{target.Name}</color></color>");
            DebugLogger.Log($"<color=#FF0000>[DAMAGE] Break Damage: {Name} → <color=#00B7EB>{target.Name}</color>: {damage} (Elemental Modifier: {elementalModifier}, AOE Reduction: {(isAoE && !isPrimaryTarget ? aoEDamageReduction : 0f)})</color>");
        }

        return (damage, skillName, isAoE, 0f, maxAoETargets, aoEDamageReduction);
    }

    public void GainMana(int amount) { Mana = mana + amount; }
    public void GainEnergy(int amount) { Energy = energy + amount; }
    public void ResetMana() { Mana = 0; }
    public void ResetActionValue() { actionValue = 0f; }

    public void AdvanceActionValue(float amount)
    {
        actionValue += (slowTurnsRemaining > 0 ? amount * slowAmount : amount);
    }

    public void SetMoving(bool isMoving)
    {
        if (animator != null)
        {
            animator.SetBool("IsMoving", isMoving);
            if (!isMoving && HP > 0)
            {
                SetIdle();
            }
        }
    }

    public void SetAttacking(int actionIndex)
    {
        if (animator != null)
        {
            string trigger = actionIndex switch
            {
                0 => "Attack1",
                1 => "Attack2",
                2 => "Attack3",
                _ => "Attack1"
            };
            animator.SetTrigger(trigger);
            animator.SetBool("IsMoving", false);
        }
    }

    public void ResetAttacking()
    {
        if (animator != null)
        {
            if (HP > 0)
            {
                SetIdle();
            }
        }
    }

    public void SetHighlight(bool isHighlighted)
    {
        if (isHighlighted)
        {
            HighlightManager.Instance.ShowHighlight(transform, false); // isAlly = false cho kẻ thù
        }
        else
        {
            HighlightManager.Instance.ClearHighlight();
        }
    }

    public static Enemy GetSelectedEnemy()
    {
        return selectedEnemy;
    }

    public static void ClearSelectedEnemy()
    {
        if (selectedEnemy != null)
        {
            selectedEnemy.SetHighlight(false);
            UIManager uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager != null)
            {
                uiManager.HideCombatantInfo(); // Ẩn panel chung
            }
            selectedEnemy = null;
        }
    }

    private void SetIdle()
    {
        if (animator != null && !animator.GetBool("IsDead"))
        {
            animator.SetBool("IsMoving", false);
            animator.ResetTrigger("Attack1");
            animator.ResetTrigger("Attack2");
            animator.ResetTrigger("Attack3");
        }
    }

    private System.Collections.IEnumerator DelayedHideOrDestroy()
    {
        float deathAnimationDuration = GetAnimationDuration("Death");
        yield return new WaitForSeconds(deathAnimationDuration);
        if (animator != null)
        {
            animator.enabled = false;
            DebugLogger.Log($"[Enemy] {Name} death animation stopped.");
        }
        if (SceneManager.GetActiveScene().name == "BattleScene")
        {
            gameObject.SetActive(false);
            DebugLogger.Log($"[Enemy] {Name} has been hidden (HP <= 0) in BattleScene.");
        }
    }


    public static void HighlightAllEnemies(Enemy[] enemies)
    {
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.HP > 0)
            {
                enemy.SetHighlight(true);
            }
        }
    }

    public static void ClearAllEnemiesHighlight(Enemy[] enemies)
    {
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.SetHighlight(false);
            }
        }
    }

    private float GetAnimationDuration(string animationTrigger)
    {
        if (animator == null)
        {
            DebugLogger.LogWarning($"No Animator found for {Name}, using default duration 1f");
            return 1f;
        }
        var controller = animator.runtimeAnimatorController;
        if (controller == null)
        {
            DebugLogger.LogWarning($"No AnimatorController found for {Name}, using default duration 1f");
            return 1f;
        }
        foreach (var clip in controller.animationClips)
        {
            if (clip != null && clip.name.Contains(animationTrigger))
            {
                return clip.length;
            }
        }
        DebugLogger.LogWarning($"No animation clip found for trigger {animationTrigger} in {Name}, using default duration 1f");
        return 1f;
    }
}