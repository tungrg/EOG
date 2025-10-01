using UnityEngine;
using System;
using static ItemData;

public enum AttackRange { Melee, Ranged }
public enum StatusEffect { None, Freeze, Burn, Slow, Panic, Heal, Shield }
public enum ElementType { Wind, Ash, Ice, Tide }

[CreateAssetMenu(fileName = "CombatantData", menuName = "Data/CombatantData")]
public class CombatantData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("ID duy nhất, tự sinh khi để trống")]
    public string CharacterId;
    public string Name;

    [Header("Stats")]
    public int MaxHP;
    public int HP;
    public int Attack;
    public int DEF;
    public int Agility;
    public int SPD;
    public float CritRate;
    public float CritDMG;
    public float BonusDMG;
    public float Vulnerability;
    public float RES;
    public ElementType Element;
    public string Path;
    public AttackType AttackType;
    public AttackRange AttackRange;
    public float SlowAmount;
    public int SlowDuration;
    public SkillData[] Skills = new SkillData[3];
    public int Skill3ManaCost = 100;
    public int Level = 1;

    [Header("Presentation")]
    public Sprite AvatarSprite;
    public GameObject Prefab;

    [Header("Unlocking")]
    [Tooltip("Level của main cần đạt để NHẬN nhân vật này (sẽ hiện panel nhận).")]
    public int UnlockAtLevel = 1;

    // Thêm các base stats để scale khi nâng cấp
    public int BaseMaxHP;
    public int BaseAttack;
    public int BaseDEF;
    public int BaseSPD;
    public int BaseAgility;

    public WeaponType AllowedWeapon; // gán đúng cho mỗi nhân vật trong Inspector

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(CharacterId))
            CharacterId = Guid.NewGuid().ToString("N"); // sinh ID cố định 1 lần

        // Khởi tạo base stats nếu cần migrate
        if (BaseMaxHP == 0) BaseMaxHP = MaxHP;
        if (BaseAttack == 0) BaseAttack = Attack;
        if (BaseDEF == 0) BaseDEF = DEF;
        if (BaseSPD == 0) BaseSPD = SPD;
        if (BaseAgility == 0) BaseAgility = Agility;
    }
#endif
}