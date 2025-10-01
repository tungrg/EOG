using UnityEngine;

[System.Serializable]
public enum EffectType
{
    Static,
    Projectile,
    FromAbove
}

[System.Serializable]
public class SkillData
{
    public string SkillName;
    public float DamageMultiplier = 1f;
    public float DoTMultiplier = 0f;
    public float BreakBase = 0f;
    public float BreakScaling = 0f;
    public float WeaknessTypeBonus = 1f;
    public ElementType Element;
    public bool IsAoE;
    public int MaxAoETargets = 3;
    public float AoEDamageReduction = 0.5f;
    public float SlowAmount = 0.7f;
    public float HealMultiplier = 0f;
    public float ShieldAmount = 0f;
    public GameObject EffectPrefab;
    public AudioClip SoundClip;
    public string AnimationTrigger;
    public StatusEffect StatusEffect;
    public int StatusEffectDuration;
    public bool HasMovementTag;
    public float MovementDistance = 0f;
    public float EffectHeightOffset = 1f;
    public float EffectDistanceOffset = 1f;
    public EffectType EffectType = EffectType.Static;
    public float EffectAboveHeight = 5f;
    public float EffectScaleMultiplier = 1f;
    public bool IsBuff;
    public Sprite SkillIcon; // Added field for skill icon
}