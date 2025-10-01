using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelDungeonData", menuName = "Data/LevelDungeonData")]
public class LevelDungeonData : ScriptableObject
{
    public int Level; // Cấp độ (1-5)
    public List<EnemyData> Enemies; // Danh sách kẻ thù
    public List<StoneDropData> StoneDrops; // Tỷ lệ rơi đá
}

[System.Serializable]
public class StoneDropData
{
    public StoneData Stone;
    public float DropRate; // Tỷ lệ phần trăm (0-100)
}