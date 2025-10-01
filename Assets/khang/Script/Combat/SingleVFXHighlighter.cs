using UnityEngine;
using System.Collections.Generic;

public class VFXHighlighter : MonoBehaviour
{
    public static VFXHighlighter Instance;

    [SerializeField] private GameObject vfxPrefab;
    private List<GameObject> activeVfx = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Tạo VFX riêng cho enemy
    /// </summary>
    public GameObject ShowAt(Transform enemy, Vector3 localOffset)
    {
        if (vfxPrefab == null)
        {
            Debug.LogError("VFX Prefab chưa gán trong VFXHighlighter!");
            return null;
        }

        GameObject vfx = Instantiate(vfxPrefab, enemy);
        vfx.transform.localPosition = localOffset;
        vfx.SetActive(true);

        activeVfx.Add(vfx);
        return vfx;
    }

    /// <summary>
    /// Gỡ VFX khỏi enemy (khi enemy chết hoặc bị lock lại)
    /// </summary>
    public void Hide(GameObject vfx)
    {
        if (vfx != null)
        {
            activeVfx.Remove(vfx);
            Destroy(vfx);
        }
    }

    /// <summary>
    /// Xóa hết VFX đang có (khi đổi wave)
    /// </summary>
    public void ClearAll()
    {
        foreach (var v in activeVfx)
        {
            if (v != null) Destroy(v);
        }
        activeVfx.Clear();
    }
}
