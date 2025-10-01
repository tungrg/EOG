using UnityEngine;
using UnityEngine.SceneManagement;

public class Combatant : MonoBehaviour, ICombatant
{
    [SerializeField] private CombatantData data;
    [SerializeField] private Animator animator;
    private int hp;
    private int mana;
    private int energy;
    private int skillCharge;
    private float actionValue;
    private float slowAmount;
    private int slowTurnsRemaining;
    private UIManager uiManager;
    private const float DEF_COEFFICIENT = 1.0f;
    private static Combatant selectedPlayer;
    private static bool isSelectingBuffTarget = false; // Biến tĩnh để theo dõi trạng thái chọn mục tiêu buff

    protected void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                DebugLogger.LogWarning("Animator not found on " + gameObject.name);
            }
        }

        if (SceneManager.GetActiveScene().name == "BattleScene")
        {
            uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager == null)
            {
                DebugLogger.LogError("UIManager not found for " + gameObject.name);
            }
        }
    }

    void Start()
    {
        SetIdle();
    }

    void OnMouseDown()
    {
        if (HP > 0 && SceneManager.GetActiveScene().name == "BattleScene")
        {
            if (isSelectingBuffTarget)
            {
                // Chỉ chọn đồng minh khi đang ở trạng thái chọn mục tiêu buff
                Enemy.ClearSelectedEnemy();
                if (selectedPlayer != null && selectedPlayer != this)
                {
                    selectedPlayer.SetHighlight(false);
                }
                selectedPlayer = this;
                SetHighlight(true);
                if (uiManager != null)
                {
                    uiManager.ShowCombatantInfo(this);
                }
                else
                {
                    DebugLogger.LogError("UIManager not found in scene.");
                }
            }
            // Không hiển thị vòng sáng hoặc InfoPanel nếu không ở trạng thái chọn mục tiêu buff
        }
    }

    public static Combatant GetSelectedPlayer() => selectedPlayer;

    public static void ClearSelectedPlayer()
    {
        if (selectedPlayer != null)
        {
            selectedPlayer.SetHighlight(false);
            if (selectedPlayer.uiManager != null)
            {
                selectedPlayer.uiManager.HideCombatantInfo();
            }
            selectedPlayer = null;
        }
    }

    public static void SetSelectingBuffTarget(bool selecting)
    {
        isSelectingBuffTarget = selecting;
    }

    public static bool IsSelectingBuffTarget() => isSelectingBuffTarget;

    // Phương thức mới để hiển thị vòng sáng cho tất cả đồng minh
    public static void HighlightAllAllies(Combatant[] allies)
    {
        foreach (var ally in allies)
        {
            if (ally != null && ally.HP > 0)
            {
                ally.SetHighlight(true);
            }
        }
    }

    // Phương thức mới để tắt vòng sáng cho tất cả đồng minh
    public static void ClearAllAlliesHighlight(Combatant[] allies)
    {
        foreach (var ally in allies)
        {
            if (ally != null)
            {
                ally.SetHighlight(false);
            }
        }
    }

    public void SetHighlight(bool isHighlighted)
    {
        if (isHighlighted)
        {
            HighlightManager.Instance.ShowHighlight(transform, true);
        }
        else
        {
            HighlightManager.Instance.ClearHighlight();
        }
    }

    public string Name => data?.Name ?? "Unknown";

    public int HP
    {
        get => hp;
        set
        {
            int maxHP = EquipmentManager.Instance != null
                ? EquipmentManager.Instance.GetEffectiveMaxHP(data)
                : data?.MaxHP ?? 1000;

            hp = Mathf.Clamp(value, 0, maxHP);

            if (uiManager != null)
            {
                uiManager.UpdateCharacterPanels();
            }
            if (hp <= 0 && animator != null)
            {
                animator.SetBool("IsDead", true);
                StartCoroutine(StopDeathAnimation());
            }
        }
    }

    public int Mana
    {
        get => mana;
        set
        {
            mana = Mathf.Clamp(value, 0, data?.Skill3ManaCost ?? 100);
            if (uiManager != null)
            {
                uiManager.UpdateCharacterPanels();
            }
        }
    }

    public int Energy
    {
        get => energy;
        set => energy = Mathf.Clamp(value, 0, 100);
    }

    public int SkillCharge
    {
        get => skillCharge;
        set => skillCharge = Mathf.Clamp(value, 0, 3);
    }

    public int SPD
    {
        get
        {
            if (data == null) return 100;
            if (EquipmentManager.Instance == null) return data.SPD;
            return EquipmentManager.Instance.GetEffectiveSPD(data);
        }
    }

    public float ActionValue
    {
        get => actionValue;
        set => actionValue = value;
    }

    public AttackType AttackType => data?.AttackType ?? AttackType.Melee;

    public void SetData(CombatantData d)
    {
        data = d;

        if (EquipmentManager.Instance != null)
            HP = EquipmentManager.Instance.GetEffectiveMaxHP(d);
        else
            HP = data.MaxHP;

        Mana = 0;
        Energy = 0;
        SkillCharge = 0;
        actionValue = 0f;
        SetIdle();
    }

    public CombatantData GetData() => data;

    public void TakeDamage(int damage)
    {
        HP -= damage;
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

    public virtual (int damage, string skillName, bool isAoE, float slowChance, int maxAoETargets, float aoEDamageReduction)
        CalculateDamage(int actionIndex, ICombatant target, bool isPrimaryTarget = true)
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

        int atk = EquipmentManager.Instance != null ? EquipmentManager.Instance.GetEffectiveAttack(data) : data.Attack;
        float cr = EquipmentManager.Instance != null ? EquipmentManager.Instance.GetEffectiveCritRate(data) : data.CritRate;
        float cd = EquipmentManager.Instance != null ? EquipmentManager.Instance.GetEffectiveCritDMG(data) : data.CritDMG;
        float bonus = EquipmentManager.Instance != null ? EquipmentManager.Instance.GetEffectiveBonusDMG(data) : data.BonusDMG;

        int targetDef = target is Combatant c
            ? EquipmentManager.Instance.GetEffectiveDEF(c.GetData())
            : (target as Enemy).GetData().DEF;

        float targetRes = target is Combatant c2
            ? EquipmentManager.Instance.GetEffectiveRES(c2.GetData())
            : (target as Enemy).GetData().RES;

        float targetVulnerability = target is Combatant c3 ? c3.GetData().Vulnerability : (target as Enemy).GetData().Vulnerability;
        ElementType targetElement = target is Combatant c4 ? c4.GetData().Element : (target as Enemy).GetData().Element;

        float elementalModifier = GetElementalModifier(skill.Element, targetElement);
        float adjustedBonusDMG = bonus + (elementalModifier > 1f ? 0.5f : 0f);
        float adjustedVulnerability = targetVulnerability + (elementalModifier > 1f ? 0.5f : 0f);
        float adjustedRES = targetRes * (elementalModifier > 1f ? 0.5f : 1f);

        if (skill.DoTMultiplier == 0 && skill.BreakBase == 0)
        {
            float critMultiplier = Random.value < cr ? 1f + cd : 1f;
            float defenseReduction = atk / (atk + targetDef * DEF_COEFFICIENT);

            damage = Mathf.RoundToInt(
                (atk * skill.DamageMultiplier) *
                critMultiplier *
                (1f + adjustedBonusDMG) *
                (1f + adjustedVulnerability) *
                (1f - adjustedRES) *
                defenseReduction *
                (isAoE && !isPrimaryTarget ? 1f - aoEDamageReduction : 1f)
            );

            DebugLogger.Log($"<color=#00B7EB>[ACTION] {Name} uses {skill.SkillName} on <color=#FF0000>{target.Name}</color></color>");
            DebugLogger.Log($"<color=#00B7EB>[DAMAGE] Direct Damage: {Name} → <color=#FF0000>{target.Name}</color>: {damage} {(critMultiplier > 1f ? "(CRIT!)" : "")} (Elemental Modifier: {elementalModifier}, Defense Reduction: {defenseReduction:F3}, AOE Reduction: {(isAoE && !isPrimaryTarget ? aoEDamageReduction : 0f)})</color>");
        }
        else if (skill.DoTMultiplier > 0)
        {
            damage = Mathf.RoundToInt(
                atk *
                skill.DoTMultiplier *
                (1f + adjustedBonusDMG) *
                (1f + adjustedVulnerability) *
                (1f - adjustedRES) *
                (isAoE && !isPrimaryTarget ? 1f - aoEDamageReduction : 1f)
            );
            DebugLogger.Log($"<color=#00B7EB>[ACTION] {Name} uses {skill.SkillName} (DoT) on <color=#FF0000>{target.Name}</color></color>");
            DebugLogger.Log($"<color=#00B7EB>[DAMAGE] DoT Damage: {Name} → <color=#FF0000>{target.Name}</color>: {damage} (Elemental Modifier: {elementalModifier}, AOE Reduction: {(isAoE && !isPrimaryTarget ? aoEDamageReduction : 0f)})</color>");
        }
        else if (skill.BreakBase > 0)
        {
            float levelMultiplier = 1f + (data.Level * 0.05f);
            damage = Mathf.RoundToInt(
                (skill.BreakBase * levelMultiplier + atk * skill.BreakScaling) *
                skill.WeaknessTypeBonus *
                elementalModifier *
                (isAoE && !isPrimaryTarget ? 1f - aoEDamageReduction : 1f)
            );
            DebugLogger.Log($"<color=#00B7EB>[ACTION] {Name} uses {skill.SkillName} (Break) on <color=#FF0000>{target.Name}</color></color>");
            DebugLogger.Log($"<color=#00B7EB>[DAMAGE] Break Damage: {Name} → <color=#FF0000>{target.Name}</color>: {damage} (Elemental Modifier: {elementalModifier}, AOE Reduction: {(isAoE && !isPrimaryTarget ? aoEDamageReduction : 0f)})</color>");
        }

        return (damage, skillName, isAoE, 0f, maxAoETargets, aoEDamageReduction);
    }

    public void GainSkillCharge(int amount) { SkillCharge = Mathf.Min(SkillCharge + amount, 3); }
    public void ResetSkillCharge() { SkillCharge = 0; }
    public void GainMana(int amount) { Mana = mana + amount; }
    public void ResetMana() { Mana = 0; }
    public void GainEnergy(int amount) { Energy = energy + amount; }
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

    private System.Collections.IEnumerator StopDeathAnimation()
    {
        float deathDuration = GetAnimationDuration("Death");
        yield return new WaitForSeconds(deathDuration);
        if (animator != null)
        {
            animator.enabled = false;
            DebugLogger.Log($"[Combatant] {Name} death animation stopped.");
        }
    }

    private float GetAnimationDuration(string animationTrigger)
    {
        if (animator == null) return 1f;
        var controller = animator.runtimeAnimatorController;
        if (controller == null) return 1f;
        foreach (var clip in controller.animationClips)
        {
            if (clip != null && clip.name.Contains(animationTrigger))
            {
                return clip.length;
            }
        }
        return 1f;
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
        if (animator != null && HP > 0)
        {
            SetIdle();
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
}