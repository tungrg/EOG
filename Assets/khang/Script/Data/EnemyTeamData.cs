using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyTeamData", menuName = "Data/EnemyTeamData")]
public class EnemyTeamData : ScriptableObject
{
    public List<EnemyData> Enemies;
}