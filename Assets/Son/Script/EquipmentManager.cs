using UnityEngine;
using System.Collections.Generic;
using System.IO;
using static ItemData;

[System.Serializable]
public class EquippedSet
{
    public string CharacterId; // từ CombatantData.CharacterId
    public string GlovesId;
    public string ArmorId;
    public string PantsId;
    public string WeaponId;
}

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    public event System.Action<string /*CharacterId*/> OnEquipmentChanged;

    [SerializeField] private InventoryData inventoryData;

    // Luôn đồng bộ từ InventoryData, không cần gắn tay trong Inspector
    [HideInInspector][SerializeField] private List<ItemData> allItems;

    private Dictionary<string, EquippedSet> byChar = new(); // key = CharacterId
    private string SavePath => Path.Combine(Application.persistentDataPath, "equipment.json");

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SyncAllItems();
        Load();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        SyncAllItems();
    }
#endif

    private void SyncAllItems()
    {
        if (inventoryData != null)
            allItems = inventoryData.GetAllItems();
        else
            allItems = new List<ItemData>();
    }

    // Trang bị 1 món cho 1 nhân vật
    public bool Equip(CombatantData character, EquipmentData equip)
    {
        if (!IsCompatible(character, equip)) return false;

        if (!byChar.TryGetValue(character.CharacterId, out var set))
        {
            set = new EquippedSet { CharacterId = character.CharacterId };
            byChar[character.CharacterId] = set;
        }

        // Un-equip cũ (nếu có) -> trả item về inventory
        ItemData previous = null;
        switch (equip.Slot)
        {
            case EquipmentSlot.Gloves:
                if (!string.IsNullOrEmpty(set.GlovesId)) previous = FindItemById(set.GlovesId);
                set.GlovesId = equip.Id;
                break;
            case EquipmentSlot.Armor:
                if (!string.IsNullOrEmpty(set.ArmorId)) previous = FindItemById(set.ArmorId);
                set.ArmorId = equip.Id;
                break;
            case EquipmentSlot.Pants:
                if (!string.IsNullOrEmpty(set.PantsId)) previous = FindItemById(set.PantsId);
                set.PantsId = equip.Id;
                break;
            case EquipmentSlot.Weapon:
                if (!string.IsNullOrEmpty(set.WeaponId)) previous = FindItemById(set.WeaponId);
                set.WeaponId = equip.Id;
                break;
        }

        // Trả món cũ về inventory nếu có
        if (previous != null) inventoryData.AddItem(previous);

        // Lấy 1 món mới từ inventory (giảm 1)
        inventoryData.RemoveOne(equip);

        Save();
        OnEquipmentChanged?.Invoke(character.CharacterId);
        return true;
    }

    public void Unequip(CombatantData character, EquipmentSlot slot)
    {
        if (!byChar.TryGetValue(character.CharacterId, out var set)) return;

        string id = null;
        switch (slot)
        {
            case EquipmentSlot.Gloves: id = set.GlovesId; set.GlovesId = null; break;
            case EquipmentSlot.Armor: id = set.ArmorId; set.ArmorId = null; break;
            case EquipmentSlot.Pants: id = set.PantsId; set.PantsId = null; break;
            case EquipmentSlot.Weapon: id = set.WeaponId; set.WeaponId = null; break;
        }
        if (!string.IsNullOrEmpty(id))
        {
            var item = FindItemById(id);
            if (item != null) inventoryData.AddItem(item);
        }
        Save();
        OnEquipmentChanged?.Invoke(character.CharacterId);
    }

    public EquippedSet GetEquipped(CombatantData character)
    {
        byChar.TryGetValue(character.CharacterId, out var set);
        return set;
    }

    public bool IsCompatible(CombatantData c, EquipmentData e)
    {
        if (e.Slot == EquipmentSlot.Weapon)
            return c.AllowedWeapon == e.WeaponType; // vũ khí chỉ đúng nhân vật
        // 3 món còn lại ai cũng dùng được
        return true;
    }

    // ======================
    //  Tính tổng bonus từ trang bị (cộng dồn)
    // ======================
    public (int atk, int def, int hp, int spd, int agi, float cr, float cd, float bonusDmg, float res)
    GetTotalBonus(CombatantData character)
    {
        int atk = 0, def = 0, hp = 0, spd = 0, agi = 0;
        float cr = 0f, cd = 0f, bonusDmg = 0f, res = 0f;

        if (byChar.TryGetValue(character.CharacterId, out var set))
        {
            foreach (var id in new[] { set.GlovesId, set.ArmorId, set.PantsId, set.WeaponId })
            {
                if (string.IsNullOrEmpty(id)) continue;
                var eq = FindItemById(id) as EquipmentData;
                if (eq == null) continue;

                atk += eq.AttackBonus;
                def += eq.DefBonus;
                hp += eq.HpBonus;
                spd += eq.SpdBonus;
                agi += eq.AgilityBonus;
                cr += eq.CritRateBonus;
                cd += eq.CritDMGBonus;

                // --- CHỈ SỐ DAMAGE ĐẶC BIỆT (CỘNG DỒN) ---
                bonusDmg += eq.BonusDMG;

                // --- Kháng (cũng cộng dồn) ---
                res += eq.RES;
            }
        }
        return (atk, def, hp, spd, agi, cr, cd, bonusDmg, res);
    }

    // ======================
    //  Lấy chỉ số thực tế (gốc + bonus)
    // ======================
    public int GetEffectiveAttack(CombatantData c) => c.Attack + GetTotalBonus(c).atk;
    public int GetEffectiveDEF(CombatantData c) => c.DEF + GetTotalBonus(c).def;
    public int GetEffectiveMaxHP(CombatantData c) => c.MaxHP + GetTotalBonus(c).hp;
    public int GetEffectiveSPD(CombatantData c) => c.SPD + GetTotalBonus(c).spd;
    public int GetEffectiveAgility(CombatantData c) => c.Agility + GetTotalBonus(c).agi;
    public float GetEffectiveCritRate(CombatantData c) => c.CritRate + GetTotalBonus(c).cr;
    public float GetEffectiveCritDMG(CombatantData c) => c.CritDMG + GetTotalBonus(c).cd;
    public float GetEffectiveBonusDMG(CombatantData c) => c.BonusDMG + GetTotalBonus(c).bonusDmg;
    public float GetEffectiveRES(CombatantData c) => c.RES + GetTotalBonus(c).res;

    // --- Save/Load ---
    [System.Serializable]
    private class EquipmentSave
    {
        public List<EquippedSet> list = new();
    }

    private void Save()
    {
        var data = new EquipmentSave { list = new List<EquippedSet>(byChar.Values) };
        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
    }

    private void Load()
    {
        byChar.Clear();
        if (!File.Exists(SavePath)) return;
        var data = JsonUtility.FromJson<EquipmentSave>(File.ReadAllText(SavePath));
        if (data?.list == null) return;
        foreach (var s in data.list) byChar[s.CharacterId] = s;
    }

    private ItemData FindItemById(string id)
    {
        if (allItems == null || allItems.Count == 0)
            SyncAllItems();
        return allItems.Find(i => i != null && i.Id == id);
    }

    public List<EquipmentData> GetAllEquips()
    {
        if (allItems == null) return new List<EquipmentData>();
        return allItems.FindAll(i => i is EquipmentData).ConvertAll(i => (EquipmentData)i);
    }

    public static void DebugEffectiveStats(CombatantData c)
    {
        var em = EquipmentManager.Instance;
        if (em == null) { Debug.LogWarning("No EquipmentManager"); return; }

        var t = em.GetTotalBonus(c);
        Debug.Log($"[{c.CharacterId}] Bonus => ATK+{t.atk}, DEF+{t.def}, HP+{t.hp}, SPD+{t.spd}, AGI+{t.agi}, CR+{t.cr}, CD+{t.cd}, BDMG+{t.bonusDmg}, RES+{t.res}");
        Debug.Log($"[{c.CharacterId}] Effective => ATK:{em.GetEffectiveAttack(c)}, DEF:{em.GetEffectiveDEF(c)}, HP:{em.GetEffectiveMaxHP(c)}, SPD:{em.GetEffectiveSPD(c)}");
    }
}
