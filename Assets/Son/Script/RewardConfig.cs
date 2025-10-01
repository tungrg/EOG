using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "RewardConfig", menuName = "Data/RewardConfig")]
public class RewardConfig : ScriptableObject
{
    [Header("Danh sách trang bị")]
    [Tooltip("Kéo đủ 21 asset EquipmentData (7 loại × 3 độ hiếm)")]
    public List<EquipmentData> AllEquipment;

    [Header("Số lượng rơi mỗi lần")]
    [Tooltip("Khoảng số lượng item rơi (ví dụ 3–5 món)")]
    public Vector2Int DropCountRange = new Vector2Int(3, 5);

    [Header("Tỷ lệ độ hiếm (cộng lại ~1.0)")]
    [Range(0f, 1f)] public float CommonChance = 0.60f;
    [Range(0f, 1f)] public float RareChance = 0.30f;
    [Range(0f, 1f)] public float EpicChance = 0.10f;

    private System.Random rng = new System.Random();

    /// <summary>
    /// Roll ra danh sách trang bị rơi.
    /// </summary>
    public List<EquipmentData> RollRewards()
    {
        int count = rng.Next(DropCountRange.x, DropCountRange.y + 1);
        var results = new List<EquipmentData>();

        for (int i = 0; i < count; i++)
        {
            var rarity = RollRarity();
            var pool = AllEquipment.Where(e => e.ItemRarity == rarity).ToList();

            if (pool.Count == 0)
            {
                Debug.LogWarning($"[RewardConfig] Không có item nào thuộc rarity {rarity} trong AllEquipment!");
                continue;
            }

            var picked = pool[rng.Next(pool.Count)];
            results.Add(picked);
        }

        return results;
    }

    /// <summary>
    /// Roll độ hiếm dựa trên tỷ lệ.
    /// </summary>
    private ItemData.Rarity RollRarity()
    {
        float r = (float)rng.NextDouble();
        
        if (r < EpicChance) return ItemData.Rarity.Epic;
        if (r < EpicChance + RareChance) return ItemData.Rarity.Rare;
        return ItemData.Rarity.Common;
    }
}
