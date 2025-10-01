using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatantManager : MonoBehaviour
{
    public List<Combatant> PlayerCombatants { get; private set; } = new List<Combatant>();
    public List<Enemy> EnemyCombatants { get; private set; } = new List<Enemy>();

    private TeamData teamData;
    private List<Transform> playerSpawnPoints;
    private List<Transform> enemySpawnPoints;
    private ObjectPool objectPool; // Thêm ObjectPool

    void Awake()
    {
        objectPool = gameObject.AddComponent<ObjectPool>(); // Khởi tạo ObjectPool
    }

    public void SetTeamData(TeamData data)
    {
        teamData = data;
    }

    public void SetSpawnPoints(List<Transform> playerPoints, List<Transform> enemyPoints)
    {
        playerSpawnPoints = playerPoints;
        enemySpawnPoints = enemyPoints;
    }

    public void SetupCombatants()
    {
        if (teamData == null || playerSpawnPoints == null || enemySpawnPoints == null)
        {
            DebugLogger.LogError("CombatantManager: TeamData or SpawnPoints not set.");
            return;
        }

        // Khởi tạo pool cho CharacterUI
        GameObject uiPrefab = Resources.Load<GameObject>("Prefabs/CharacterUI");
        if (uiPrefab != null)
        {
            objectPool.InitializePool(uiPrefab, 4); // Pool cho tối đa 4 CharacterUI
        }

        // Khởi tạo pool cho HealthBarPrefab của mỗi enemy
        foreach (var enemyData in teamData.SelectedEnemies)
        {
            if (enemyData != null && enemyData.HealthBarPrefab != null)
            {
                objectPool.InitializePool(enemyData.HealthBarPrefab, 1); // Pool cho mỗi HealthBar
            }
        }

        PlayerCombatants.Clear();
        EnemyCombatants.Clear();

        List<Transform> availablePlayerSpawns = new List<Transform>(playerSpawnPoints);
        List<Transform> availableEnemySpawns = new List<Transform>(enemySpawnPoints);

        // Spawn Player Combatants
        int playerCount = Mathf.Min(teamData.SelectedCombatants.Count, 4);

        for (int i = 0; i < playerCount; i++)
        {
            if (i >= availablePlayerSpawns.Count) break;
            var combatantData = teamData.SelectedCombatants[i];
            if (combatantData != null && combatantData.Prefab != null)
            {
                GameObject playerObj = Instantiate(combatantData.Prefab, availablePlayerSpawns[i].position, Quaternion.identity);
                Combatant combatant = playerObj.GetComponent<Combatant>();
                if (combatant != null)
                {
                    combatant.SetData(combatantData);
                    PlayerCombatants.Add(combatant);

                    if (uiPrefab != null)
                    {
                        GameObject uiObj = objectPool.GetObject(uiPrefab.name, combatant.transform.position + Vector3.up * 2f, Quaternion.identity, combatant.transform);
                        CharacterUIManager uiManager = uiObj.GetComponent<CharacterUIManager>();
                        if (uiManager != null) uiManager.SetCombatant(combatant);
                    }
                }
                else
                {
                    DebugLogger.LogError($"Combatant component missing on instantiated prefab {combatantData.Name}");
                }
            }
            else
            {
                DebugLogger.LogError($"Invalid CombatantData or Prefab at index {i}");
            }
        }

        // Spawn Enemy Combatants
        int enemyCount = Mathf.Min(teamData.SelectedEnemies.Count, availableEnemySpawns.Count);

        for (int i = 0; i < enemyCount; i++)
        {
            if (i >= availableEnemySpawns.Count) break;
            var enemyData = teamData.SelectedEnemies[i];
            if (enemyData != null && enemyData.Prefab != null)
            {
                GameObject enemyObj = Instantiate(enemyData.Prefab, availableEnemySpawns[i].position, Quaternion.identity);
                Enemy enemy = enemyObj.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.SetData(enemyData);
                    EnemyCombatants.Add(enemy);

                    EnemyTrigger trigger = enemyObj.GetComponent<EnemyTrigger>();
                    if (trigger != null)
                    {
                        Destroy(trigger);
                    }

                    if (enemyData.HealthBarPrefab == null)
                    {
                        DebugLogger.LogWarning($"HealthBarPrefab not assigned for enemy {enemyData.Name}.");
                    }
                }
                else
                {
                    DebugLogger.LogError($"Enemy component missing on instantiated prefab {enemyData.Name}");
                }
            }
            else
            {
                DebugLogger.LogError($"Invalid EnemyData or Prefab at index {i}");
            }
        }
    }

    public bool IsBattleOver()
    {
        return PlayerCombatants.All(p => p == null || p.HP <= 0) || EnemyCombatants.All(e => e == null || e.HP <= 0);
    }

    // Trả lại UI về pool khi combatant bị phá hủy
    public void ReturnUIObject(GameObject uiObject)
    {
        if (uiObject != null)
        {
            objectPool.ReturnObject(uiObject);
        }
    }
}