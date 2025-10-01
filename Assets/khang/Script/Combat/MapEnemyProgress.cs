using UnityEngine;

[CreateAssetMenu(fileName = "MapEnemyProgress", menuName = "Data/MapEnemyProgress")]
public class MapEnemyProgress : ScriptableObject
{
    public int CurrentWave = 0;
    public int CurrentEnemyIndex = 0;

    // Reset nếu cần (gọi manual)
    public void ResetProgress()
    {
        CurrentWave = 0;
        CurrentEnemyIndex = 0;
        Save();
    }

    public void Save()
    {
        PlayerPrefs.SetInt("Map_CurrentWave", CurrentWave);
        PlayerPrefs.SetInt("Map_CurrentEnemyIndex", CurrentEnemyIndex);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        CurrentWave = PlayerPrefs.GetInt("Map_CurrentWave", 0);
        CurrentEnemyIndex = PlayerPrefs.GetInt("Map_CurrentEnemyIndex", 0);
    }
}