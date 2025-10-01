using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private MapEnemyProgress progress;
    [SerializeField] private GameObject teamInfoPanel; // Thay prefab bằng GameObject gán trực tiếp
    [SerializeField] private List<GameObject> waveGameObjects = new List<GameObject>();

    private List<List<EnemyTrigger>> waves = new List<List<EnemyTrigger>>();
    private const int EnemiesPerWave = 10;

    void Awake()
    {
        if (progress == null)
        {
            DebugLogger.LogError("MapEnemyProgress not assigned in EnemyManager.");
            return;
        }

        // Load progress từ PlayerPrefs
        progress.Load();
        DebugLogger.Log($"[EnemyManager] Loaded progress: Wave {progress.CurrentWave}, Index {progress.CurrentEnemyIndex}");

        foreach (var waveGO in waveGameObjects)
        {
            var enemies = waveGO.GetComponentsInChildren<EnemyTrigger>(true).ToList();
            if (enemies.Count != EnemiesPerWave)
            {
                DebugLogger.LogWarning($"Wave {waveGO.name} has {enemies.Count} enemies, expected {EnemiesPerWave}.");
            }
            waves.Add(enemies);
        }

        if (waves.Count == 0)
        {
            DebugLogger.LogError("No waves found in EnemyManager.");
            return;
        }

        if (teamInfoPanel == null)
        {
            DebugLogger.LogError("teamInfoPanel not assigned in EnemyManager.");
            return;
        }

        // Không cần instantiate prefab, chỉ cần đảm bảo panel không active
        teamInfoPanel.SetActive(false);
    }

    void Start()
    {
        foreach (var wave in waves)
        {
            foreach (var enemy in wave)
            {
                if (enemy != null)
                {
                    // Gán teamInfoPanel trực tiếp cho EnemyTrigger
                    typeof(EnemyTrigger).GetField("teamInfoPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .SetValue(enemy, teamInfoPanel);
                }
            }
        }

        ApplyProgressState();
    }

    private void ApplyProgressState()
    {
        for (int w = 0; w < waves.Count; w++)
        {
            var waveGO = waveGameObjects[w];
            if (w < progress.CurrentWave)
            {
                waveGO.SetActive(false);
            }
            else if (w > progress.CurrentWave)
            {
                waveGO.SetActive(false);
            }
            else
            {
                waveGO.SetActive(true);
                var waveEnemies = waves[w];
                for (int e = 0; e < waveEnemies.Count; e++)
                {
                    var enemy = waveEnemies[e];
                    if (enemy == null) continue;

                    bool isDead = PlayerPrefs.GetInt($"EnemyDead_W{w}_E{e}", 0) == 1;
                    bool isBeforeCurrent = e < progress.CurrentEnemyIndex;

                    if (isDead || isBeforeCurrent)
                    {
                        DebugLogger.Log($"[EnemyManager] Destroying enemy W{w}_E{e} (Dead: {isDead}, BeforeCurrent: {isBeforeCurrent})");
                        Destroy(enemy.gameObject);
                        continue;
                    }
                    else
                    {
                        bool locked = (e != progress.CurrentEnemyIndex);
                        enemy.SetLocked(locked);
                        if (!locked)
                        {
                            enemy.SetHighlight(true);
                            DebugLogger.Log($"[EnemyManager] Unlocking and highlighting enemy W{w}_E{e}");
                        }
                    }
                }
            }
        }
    }
}