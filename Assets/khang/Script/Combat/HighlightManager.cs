using UnityEngine;
using System.Collections.Generic;

public class HighlightManager : MonoBehaviour
{
    private static HighlightManager instance;
    [SerializeField] private GameObject allyHighlightPrefab; // Prefab vòng sáng cho đồng đội (ví dụ: FX_blue)
    [SerializeField] private GameObject enemyHighlightPrefab; // Prefab vòng sáng cho kẻ thù (FX_red)
    private List<GameObject> activeHighlights = new List<GameObject>(); // List lưu trữ tất cả highlight đang hoạt động

    // Singleton pattern: Đảm bảo chỉ có một instance của HighlightManager
    public static HighlightManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<HighlightManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("HighlightManager");
                    instance = obj.AddComponent<HighlightManager>();
                }
            }
            return instance;
        }
    }

    // Khởi tạo Singleton và giữ object qua các scene
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Hiển thị vòng sáng cho một mục tiêu (ally hoặc enemy)
    public void ShowHighlight(Transform target, bool isAlly)
    {
        if (allyHighlightPrefab == null || enemyHighlightPrefab == null)
        {
            DebugLogger.LogError("Highlight prefab(s) not assigned in HighlightManager.");
            return;
        }

        // Chọn prefab phù hợp dựa trên loại mục tiêu
        GameObject highlightPrefab = isAlly ? allyHighlightPrefab : enemyHighlightPrefab;

        // Khởi tạo vòng sáng mới tại vị trí của target
        GameObject newHighlight = Instantiate(highlightPrefab, target.position, Quaternion.identity, target);
        newHighlight.transform.localPosition = Vector3.zero; // Đảm bảo vòng sáng nằm đúng vị trí
        activeHighlights.Add(newHighlight); // Thêm vào danh sách active highlights
        DebugLogger.Log($"[HighlightManager] Added highlight for {target.name} (isAlly: {isAlly})");
    }

    // Xóa một highlight cụ thể (nếu cần, để tương lai mở rộng)
    public void ClearSpecificHighlight(GameObject highlight)
    {
        if (highlight != null && activeHighlights.Contains(highlight))
        {
            Destroy(highlight);
            activeHighlights.Remove(highlight);
            DebugLogger.Log($"[HighlightManager] Removed specific highlight: {highlight.name}");
        }
    }

    // Xóa toàn bộ highlights
    public void ClearAllHighlights()
    {
        foreach (var highlight in activeHighlights)
        {
            if (highlight != null)
            {
                Destroy(highlight);
            }
        }
        activeHighlights.Clear();
        DebugLogger.Log("[HighlightManager] Cleared all highlights");
    }

    // Giữ lại để tương thích với code cũ, nhưng gọi ClearAllHighlights
    public void ClearHighlight()
    {
        ClearAllHighlights();
    }
}