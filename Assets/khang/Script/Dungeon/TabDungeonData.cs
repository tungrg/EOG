using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TabDungeonData", menuName = "Data/TabDungeonData")]
public class TabDungeonData : ScriptableObject
{
    public ElementType Element; // Wind, Ash, Ice, Tide
    public Sprite TabSprite;
    public Sprite PanelSprite;
    public List<LevelDungeonData> Levels; // 5 cấp độ
}