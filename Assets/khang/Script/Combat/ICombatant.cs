using UnityEngine;

public interface ICombatant
{
    string Name { get; }
    int HP { get; set; }
    int Mana { get; set; }
    int Energy { get; set; }
    int SPD { get; }
    float ActionValue { get; set; }
    void TakeDamage(int damage);
    void ApplySlow(float amount, int duration);
    void UpdateStatus();
    void AdvanceActionValue(float amount);
    void ResetActionValue();
    (int damage, string skillName, bool isAoE, float slowChance, int maxAoETargets, float aoEDamageReduction) CalculateDamage(int actionIndex, ICombatant target, bool isPrimaryTarget = true);
}