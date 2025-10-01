using UnityEngine;
using UnityEngine.EventSystems;

public class EnemyInfoPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private UIManager uiManager;

    void Start()
    {
        uiManager = FindFirstObjectByType<UIManager>(); // Thay đổi từ FindObjectOfType
        if (uiManager == null)
        {
            DebugLogger.LogError("UIManager not found for EnemyInfoPanel.");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (uiManager == null) return;

        // Ưu tiên lấy enemy trước
        ICombatant selected = Enemy.GetSelectedEnemy();
        if (selected == null)
        {
            selected = Combatant.GetSelectedPlayer();
        }

        if (selected != null)
        {
            uiManager.ShowDetail(selected); // Dùng hàm gộp
        }
    }



    public void OnPointerExit(PointerEventData eventData)
    {
        if (uiManager != null)
        {
            uiManager.HideEnemyDetail();
        }
    }
}