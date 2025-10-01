using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[CreateAssetMenu(fileName = "InventoryData", menuName = "Data/InventoryData")]
public class InventoryData : ScriptableObject
{
    [System.Serializable]
    public class ItemEntry
    {
        public ItemData Item;
        public int Quantity;
        public int SlotIndex;
    }

    [System.Serializable]
    private class JsonItemEntry
    {
        public string ItemId;
        public int Quantity;
        public int SlotIndex;
    }

    [System.Serializable]
    private class JsonInventoryData
    {
        public List<JsonItemEntry> Items;
    }

    public List<ItemEntry> Items = new List<ItemEntry>();
    public int Max_slots = 12;
    [SerializeField] private List<ItemData> allItems;
    public event System.Action OnInventoryChanged;

    private string SavePath => Path.Combine(Application.persistentDataPath, "inventory.json");

    public List<ItemData> GetAllItems()
    {
        return allItems;
    }

    void OnEnable()
    {
        // 🔹 Chỉ load JSON nếu file tồn tại và Items hiện tại trống
        if (File.Exists(SavePath))
        {
            if (Items == null || Items.Count == 0)
            {
                LoadFromJson();
            }
        }
    }

    public void AddItem(ItemData item)
    {
        if (item == null)
        {
            DebugLogger.LogWarning("[InventoryData] Attempted to add null item");
            return;
        }

        DebugLogger.Log($"[InventoryData] Adding item with ID: {item.Id}, Name: {item.ItemName}");
        var existingItem = Items.Find(i => i.Item != null && i.Item.Id == item.Id);
        if (existingItem != null)
        {
            existingItem.Quantity++;
            DebugLogger.Log($"[InventoryData] Increased {item.ItemName} to x{existingItem.Quantity} in slot {existingItem.SlotIndex}");
        }
        else
        {
            int slotIndex = GetNextEmptySlot();
            if (slotIndex >= 0)
            {
                Items.Add(new ItemEntry { Item = item, Quantity = 1, SlotIndex = slotIndex });
                DebugLogger.Log($"[InventoryData] Added {item.ItemName} x1 to slot {slotIndex}");
            }
            else
            {
                DebugLogger.LogWarning($"[InventoryData] No empty slots available for {item.ItemName}");
            }
        }

        // 🔹 Fix: reindex toàn bộ lại để slot không bị bỏ trống
        ReindexItems();

        SaveToJson();
        OnInventoryChanged?.Invoke();
    }

    // Hàm mới
    private void ReindexItems()
    {
        for (int i = 0; i < Items.Count; i++)
        {
            Items[i].SlotIndex = i;
        }
    }

    public bool RemoveOne(ItemData item)
    {
        var entry = Items.Find(i => i.Item != null && i.Item.Id == item.Id);
        if (entry == null) return false;

        entry.Quantity--;
        if (entry.Quantity <= 0)
        {
            Items.Remove(entry);
        }

        SaveToJson();
        OnInventoryChanged?.Invoke();
        return true;
    }

    private int GetNextEmptySlot()
    {
        for (int i = 0; i < Max_slots; i++)
        {
            if (!Items.Exists(item => item.SlotIndex == i))
            {
                return i;
            }
        }
        return -1;
    }

    public void Clear()
    {
        Items.Clear();
        SaveToJson();
        OnInventoryChanged?.Invoke();
    }

    public bool MergeStones(ElementType element, int lowGrade)
    {
        var lowStones = Items
            .Where(i => i.Item is StoneData stone && stone.Element == element && stone.Grade == lowGrade)
            .ToList();

        int totalLow = lowStones.Sum(i => i.Quantity);
        if (totalLow < 10)
        {
            DebugLogger.LogWarning($"[InventoryData] Not enough stones (need 10 grade {lowGrade} {element}).");
            return false;
        }

        var highStoneData = allItems.FirstOrDefault(item => item is StoneData stone && stone.Element == element && stone.Grade == lowGrade + 1) as StoneData;
        if (highStoneData == null)
        {
            DebugLogger.LogWarning($"[InventoryData] No higher grade stone found for {element} grade {lowGrade + 1}.");
            return false;
        }

        int remainingToRemove = 10;
        foreach (var entry in lowStones.ToList())
        {
            if (remainingToRemove <= 0) break;
            int remove = Mathf.Min(remainingToRemove, entry.Quantity);
            entry.Quantity -= remove;
            if (entry.Quantity <= 0)
            {
                Items.Remove(entry);
            }
            remainingToRemove -= remove;
        }

        AddItem(highStoneData);

        DebugLogger.Log($"[InventoryData] Merged 10 grade {lowGrade} {element} stones into 1 grade {lowGrade + 1} stone.");
        SaveToJson();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void RemoveItem(int slotIndex)
    {
        var itemToRemove = Items.Find(i => i.SlotIndex == slotIndex);
        if (itemToRemove != null)
        {
            RemoveOne(itemToRemove.Item); // chỉ trừ đi 1
            DebugLogger.Log($"[InventoryData] Removed one {itemToRemove.Item.ItemName} from slot {slotIndex}");
        }
        else
        {
            DebugLogger.LogWarning($"[InventoryData] No item found in slot {slotIndex} to remove.");
        }
    }

    public void SaveToJson()
    {
        var jsonItems = new List<JsonItemEntry>();
        foreach (var item in Items)
        {
            if (item.Item == null || string.IsNullOrEmpty(item.Item.Id))
            {
                DebugLogger.LogWarning($"[InventoryData] Skipping item with null or empty ID in slot {item.SlotIndex}");
                continue;
            }
            jsonItems.Add(new JsonItemEntry
            {
                ItemId = item.Item.Id,
                Quantity = item.Quantity,
                SlotIndex = item.SlotIndex
            });
        }

        var jsonData = new JsonInventoryData { Items = jsonItems };
        string json = JsonUtility.ToJson(jsonData, true);
        File.WriteAllText(SavePath, json);
        DebugLogger.Log($"[InventoryData] Saved to {SavePath}");
    }

    public void LoadFromJson()
    {
        if (!File.Exists(SavePath))
        {
            DebugLogger.Log($"[InventoryData] No save file found at {SavePath}, starting with empty inventory.");
            Items.Clear(); // Reset rỗng khi không có file
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            var jsonData = JsonUtility.FromJson<JsonInventoryData>(json);

            if (jsonData?.Items == null)
            {
                DebugLogger.LogWarning($"[InventoryData] Invalid JSON format in {SavePath}, resetting inventory.");
                Items.Clear();
                return;
            }

            Items.Clear(); // Quan trọng: xóa list trong asset trước khi load
            foreach (var jsonItem in jsonData.Items)
            {
                var itemData = allItems.Find(i => i != null && i.Id == jsonItem.ItemId);
                if (itemData != null)
                {
                    Items.Add(new ItemEntry
                    {
                        Item = itemData,
                        Quantity = jsonItem.Quantity,
                        SlotIndex = jsonItem.SlotIndex
                    });
                    DebugLogger.Log($"[InventoryData] Loaded {itemData.ItemName} x{jsonItem.Quantity} in slot {jsonItem.SlotIndex}");
                }
                else
                {
                    DebugLogger.LogWarning($"[InventoryData] Item with ID {jsonItem.ItemId} not found in allItems");
                }
            }
            OnInventoryChanged?.Invoke();
        }
        catch (System.Exception e)
        {
            DebugLogger.LogError($"[InventoryData] Error loading JSON from {SavePath}: {e.Message}");
            Items.Clear();
        }
    }

    public void AddItems(List<ItemData> itemList)
    {
        if (itemList == null) return;
        foreach (var item in itemList)
        {
            AddItem(item);
        }
    }

    public void SortItems()
    {
        // Chỉ sắp xếp đá, giữ nguyên trang bị
        var stones = Items
            .Where(i => i.Item is StoneData)
            .OrderBy(i => ((StoneData)i.Item).Element)
            .ThenBy(i => ((StoneData)i.Item).Grade)
            .ThenByDescending(i => i.Quantity)
            .ToList();

        var equipments = Items.Where(i => i.Item is not StoneData).ToList();

        Items.Clear();

        // Gán lại SlotIndex từ 0 trở đi cho đá
        for (int i = 0; i < stones.Count; i++)
        {
            stones[i].SlotIndex = i;
            Items.Add(stones[i]);
        }

        // Đặt tiếp các trang bị sau đá
        for (int i = 0; i < equipments.Count; i++)
        {
            int nextIndex = Items.Count;
            equipments[i].SlotIndex = nextIndex;
            Items.Add(equipments[i]);
        }

        SaveToJson();
        OnInventoryChanged?.Invoke();
        DebugLogger.Log("[InventoryData] Stones sorted by Element, Grade, Quantity. Equipment unaffected.");
    }
}