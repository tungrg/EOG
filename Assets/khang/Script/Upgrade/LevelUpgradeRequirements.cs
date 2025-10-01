using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelUpgradeRequirements", menuName = "Data/LevelUpgradeRequirements")]
public class LevelUpgradeRequirements : ScriptableObject
{
    [System.Serializable]
    public class Requirement
    {
        public int Level; // Level cần lên (ví dụ 1 cho lên level 1->2)
        public int StoneGrade; // 1-5
        public int Quantity; // Số lượng cần
    }

    public List<Requirement> Requirements = new List<Requirement>();

    // Method để lấy requirement cho next level
    public (int grade, int quantity) GetRequirementForNextLevel(int currentLevel)
    {
        var req = Requirements.Find(r => r.Level == currentLevel);
        if (req != null)
        {
            return (req.StoneGrade, req.Quantity);
        }
        return (0, 0); // Max level hoặc không tìm thấy
    }

    // Điền dữ liệu từ Excel
    void OnEnable()
    {
        Requirements.Clear();
        // Level 1-20: Đá 1
        for (int lvl = 1; lvl <= 20; lvl++)
        {
            Requirements.Add(new Requirement { Level = lvl, StoneGrade = 1, Quantity = lvl });
        }
        // Level 21-40: Đá 2
        for (int lvl = 21; lvl <= 40; lvl++)
        {
            Requirements.Add(new Requirement { Level = lvl, StoneGrade = 2, Quantity = (lvl - 19) * 2 });
        }
        // Level 41-60: Đá 3
        for (int lvl = 41; lvl <= 60; lvl++)
        {
            Requirements.Add(new Requirement { Level = lvl, StoneGrade = 3, Quantity = (lvl - 38) * 3 });
        }
        // Level 61-70: Đá 4
        for (int lvl = 61; lvl <= 70; lvl++)
        {
            Requirements.Add(new Requirement { Level = lvl, StoneGrade = 4, Quantity = (lvl - 56) * 5 });
        }
        // Level 71-80: Đá 5
        for (int lvl = 71; lvl <= 80; lvl++)
        {
            Requirements.Add(new Requirement { Level = lvl, StoneGrade = 5, Quantity = (lvl - 61) * 10 });
        }
    }
}