using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    [Header("Thông tin cơ bản")]
    public string Id;             // ID duy nhất
    public string ItemName;       // Tên vật phẩm
    public Sprite ItemSprite;     // Icon hiển thị

    // -----------------------------
    // Enums dùng chung
    // -----------------------------
    public enum Rarity { Common, Rare, Epic }               // 3 độ hiếm: Common < Rare < Epic
    public enum EquipmentSlot { Gloves, Armor, Pants, Weapon } // Các loại slot trang bị
    public enum WeaponType { Sword, Saber, Book, Bow }     // Kiếm, Đao, Sách, Cung
}
