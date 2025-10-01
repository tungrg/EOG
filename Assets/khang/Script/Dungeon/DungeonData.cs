using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DungeonData", menuName = "Data/DungeonData")]
public class DungeonData : ScriptableObject
{
    public List<TabDungeonData> Tabs; // 4 tab: Wind, Ash, Ice, Tide
}