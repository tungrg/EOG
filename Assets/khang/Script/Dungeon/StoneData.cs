using UnityEngine;

[CreateAssetMenu(fileName = "StoneData", menuName = "Data/StoneData")]
public class StoneData : ItemData
{
    public ElementType Element; // Wind, Ash, Ice, Tide
    public int Grade; // 1-5 (loại đá theo Excel)
    public int UpgradeValue; // Giữ nguyên, có thể dùng để scale stats nếu cần
}