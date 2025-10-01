using UnityEngine;

public class PlayerCombatant : Combatant
{
    private new void Awake()
    {
        base.Awake();
    }

    public override (int damage, string skillName, bool isAoE, float slowChance, int maxAoETargets, float aoEDamageReduction) CalculateDamage(int actionIndex, ICombatant target, bool isPrimaryTarget = true)
    {
        var (damage, skillName, isAoE, slowChance, maxAoETargets, aoEDamageReduction) = base.CalculateDamage(actionIndex, target, isPrimaryTarget);
        if (Energy > 50) damage += Mathf.RoundToInt(damage * 0.1f); // Tăng 10% sát thương nếu Energy > 50
        return (damage, skillName, isAoE, slowChance, maxAoETargets, aoEDamageReduction);
    }
}