using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Data/EnemyData")]
public class EnemyData : ScriptableObject
{
    public string Name;
    public int MaxHP;
    public int HP;
    public int Attack;
    public int DEF;
    public int Agility;
    public int SPD;
    public float BonusDMG;
    public float Vulnerability;
    public float RES;
    public ElementType Element;
    public string Path;
    public AttackType AttackType;
    public AttackRange AttackRange;
    public float SlowAmount;
    public int SlowDuration;
    public SkillData[] Skills = new SkillData[3];
    //public int Skill2ManaCost = 50;
    public float Skill2Chance = 0.3f;
    public int Skill3ManaCost = 100;
    //public float Skill3Chance = 0.2f;
    public GameObject Prefab;
    public Sprite AvatarSprite;
    public GameObject HealthBarPrefab;
    public bool IsFixedHealthBar = false;
    public Vector3 HealthBarOffset = new Vector3(0, 1.5f, 0);
    public int Level = 1;
}