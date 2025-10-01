using UnityEngine;

// ==============================
// EquipmentData kế thừa ItemData
// ==============================
[CreateAssetMenu(fileName = "EquipmentData", menuName = "Data/Equipment")]
public class EquipmentData : ItemData
{
    [Header("Loại trang bị")]
    public EquipmentSlot Slot;    // Loại slot
    public Rarity ItemRarity;     // Độ hiếm
    public WeaponType WeaponType; // Chỉ dùng nếu Slot == Weapon

    [Header("Bonus chỉ số")]
    public int AttackBonus;
    public int DefBonus;
    public int HpBonus;
    public int SpdBonus;
    public int AgilityBonus;
    public float CritRateBonus;   // 0.10 = +10%
    public float CritDMGBonus;    // 0.30 = +30%
    public float BonusDMG;        // % sát thương đặc biệt
    public int RES;               // Kháng
}
