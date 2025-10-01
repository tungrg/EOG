using UnityEngine;
using System.Collections.Generic;
using System.IO;

[CreateAssetMenu(fileName = "DungeonProgressData", menuName = "Data/DungeonProgressData")]
public class DungeonProgressData : ScriptableObject
{
    [System.Serializable]
    public class LevelProgress
    {
        public ElementType Element; // Wind, Ash, Ice, Tide
        public int Level; // 1-5
        public bool IsCompleted;
    }

    [System.Serializable]
    private class JsonProgressData
    {
        public List<LevelProgress> Progress;
    }

    public List<LevelProgress> Progress = new List<LevelProgress>();
    private string SavePath => Path.Combine(Application.persistentDataPath, "dungeon_progress.json");

    void OnEnable()
    {
        LoadFromJson();
    }

    public void MarkLevelCompleted(ElementType element, int level)
    {
        var progress = Progress.Find(p => p.Element == element && p.Level == level);
        if (progress != null)
        {
            progress.IsCompleted = true;
        }
        else
        {
            Progress.Add(new LevelProgress { Element = element, Level = level, IsCompleted = true });
        }
        SaveToJson();
        DebugLogger.Log($"[DungeonProgressData] Marked {element} Level {level} as completed");
    }

    public bool IsLevelAvailable(ElementType element, int level)
    {
        if (level == 1) return true; // Level 1 luôn mở
        var previousLevel = Progress.Find(p => p.Element == element && p.Level == level - 1);
        return previousLevel != null && previousLevel.IsCompleted;
    }

    public string GetLevelStatus(ElementType element, int level)
    {
        var progress = Progress.Find(p => p.Element == element && p.Level == level);
        bool isAvailable = IsLevelAvailable(element, level);
        DebugLogger.Log($"[DungeonProgressData] Status for {element} Level {level}: {(progress != null ? $"Completed={progress.IsCompleted}" : "No progress entry")}, Available={isAvailable}");
        if (progress != null && progress.IsCompleted)
            return "Completed";
        if (isAvailable)
            return "Available";
        return "Wait";
    }

    public void SaveToJson()
    {
        var jsonData = new JsonProgressData { Progress = Progress };
        string json = JsonUtility.ToJson(jsonData, true);
        File.WriteAllText(SavePath, json);
        DebugLogger.Log($"[DungeonProgressData] Saved to {SavePath}");
    }

    public void LoadFromJson()
    {
        if (!File.Exists(SavePath))
        {
            Progress.Clear();
            DebugLogger.Log($"[DungeonProgressData] No save file found at {SavePath}, resetting progress.");
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            var jsonData = JsonUtility.FromJson<JsonProgressData>(json);
            Progress = jsonData?.Progress ?? new List<LevelProgress>();
            DebugLogger.Log($"[DungeonProgressData] Loaded {Progress.Count} progress entries");
        }
        catch (System.Exception e)
        {
            DebugLogger.LogError($"[DungeonProgressData] Error loading JSON from {SavePath}: {e.Message}");
            Progress.Clear();
        }
    }

    public void ResetProgress()
    {
        Progress.Clear();
        SaveToJson();
        DebugLogger.Log("[DungeonProgressData] Progress reset to empty.");
    }
}