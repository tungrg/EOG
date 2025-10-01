using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class CharacterProgressManager : MonoBehaviour
{
    public static CharacterProgressManager Instance { get; private set; }

    [SerializeField] private InventoryData inventoryData;
    [SerializeField] private LevelUpgradeRequirements upgradeRequirements;
    [SerializeField] private List<CombatantData> allCharacters;

    [System.Serializable]
    public class CharacterProgress
    {
        public string CharacterName;
        public int Level;
        public int MaxHP;
        public int Attack;
        public int DEF;
        public int SPD;
        public int Agility;
    }

    private List<CharacterProgress> progresses = new List<CharacterProgress>();
    private string savePath => Path.Combine(Application.persistentDataPath, "character_progress.json");

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        LoadProgress();
    }

    public void UpgradeCharacter(CombatantData character)
    {
        if (character == null || inventoryData == null || upgradeRequirements == null)
        {
            DebugLogger.LogError("[CharacterProgressManager] Missing required components.");
            return;
        }

        var progress = progresses.Find(p => p.CharacterName == character.Name);
        if (progress == null)
        {
            progress = new CharacterProgress { CharacterName = character.Name, Level = 1 };
            progresses.Add(progress);
        }

        int currentLevel = progress.Level;
        if (currentLevel >= 80)
        {
            DebugLogger.LogWarning($"{character.Name} has reached max level (80).");
            return;
        }

        var (requiredGrade, requiredQuantity) = upgradeRequirements.GetRequirementForNextLevel(currentLevel);
        if (requiredGrade == 0)
        {
            DebugLogger.LogWarning($"No upgrade requirements found for level {currentLevel + 1}.");
            return;
        }

        var suitableStones = inventoryData.Items
            .Where(i => i.Item is StoneData stone && stone.Grade == requiredGrade && stone.Element == character.Element)
            .ToList();

        int totalAvailable = suitableStones.Sum(i => i.Quantity);
        if (totalAvailable < requiredQuantity)
        {
            DebugLogger.LogWarning($"Not enough stones (need {requiredQuantity} grade {requiredGrade} {character.Element}).");
            return;
        }

        int remaining = requiredQuantity;
        foreach (var entry in suitableStones.ToList())
        {
            if (remaining <= 0) break;
            int consume = Mathf.Min(remaining, entry.Quantity);
            entry.Quantity -= consume;
            if (entry.Quantity <= 0) inventoryData.Items.Remove(entry);
            remaining -= consume;
        }
        inventoryData.SaveToJson();

        progress.Level++;
        float multiplier = 1f + (progress.Level * 0.05f);
        progress.MaxHP = Mathf.RoundToInt(character.BaseMaxHP * multiplier);
        progress.Attack = Mathf.RoundToInt(character.BaseAttack * multiplier);
        progress.DEF = Mathf.RoundToInt(character.BaseDEF * multiplier);
        progress.SPD = Mathf.RoundToInt(character.BaseSPD * multiplier);
        progress.Agility = Mathf.RoundToInt(character.BaseAgility * multiplier);

        character.Level = progress.Level;
        character.MaxHP = progress.MaxHP;
        character.HP = progress.MaxHP;
        character.Attack = progress.Attack;
        character.DEF = progress.DEF;
        character.SPD = progress.SPD;
        character.Agility = progress.Agility;

        SaveProgress();
        DebugLogger.Log($"{character.Name} upgraded to level {progress.Level}. New stats: HP {progress.MaxHP}, Attack {progress.Attack}, DEF {progress.DEF}, SPD {progress.SPD}, Agility {progress.Agility}");
    }

    public CharacterProgress GetProgress(CombatantData character)
    {
        return progresses.Find(p => p.CharacterName == character.Name);
    }

    public (int grade, int quantity) GetUpgradeRequirement(int currentLevel)
    {
        return upgradeRequirements.GetRequirementForNextLevel(currentLevel);
    }

    private void SaveProgress()
    {
        var jsonData = new JsonWrapper { Progresses = progresses };
        string json = JsonUtility.ToJson(jsonData, true);
        File.WriteAllText(savePath, json);
        DebugLogger.Log($"[CharacterProgressManager] Saved progress to {savePath}");
    }

    private void LoadProgress()
    {
        if (!File.Exists(savePath))
        {
            DebugLogger.Log($"[CharacterProgressManager] No progress file found at {savePath}.");
            return;
        }

        try
        {
            string json = File.ReadAllText(savePath);
            var data = JsonUtility.FromJson<JsonWrapper>(json);
            if (data?.Progresses != null)
            {
                progresses = data.Progresses;
                foreach (var charData in allCharacters)
                {
                    var progress = GetProgress(charData);
                    if (progress != null)
                    {
                        charData.Level = progress.Level;
                        charData.MaxHP = progress.MaxHP;
                        charData.HP = progress.MaxHP;
                        charData.Attack = progress.Attack;
                        charData.DEF = progress.DEF;
                        charData.SPD = progress.SPD;
                        charData.Agility = progress.Agility;
                    }
                }
                DebugLogger.Log($"[CharacterProgressManager] Loaded progress for {progresses.Count} characters.");
            }
            else
            {
                DebugLogger.LogWarning($"[CharacterProgressManager] Invalid JSON format in {savePath}.");
            }
        }
        catch (System.Exception e)
        {
            DebugLogger.LogError($"[CharacterProgressManager] Error loading progress from {savePath}: {e.Message}");
            progresses.Clear();
        }
    }

    [System.Serializable]
    private class JsonWrapper
    {
        public List<CharacterProgress> Progresses;
    }
}