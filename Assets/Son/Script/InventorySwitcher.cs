using UnityEngine;
using UnityEngine.UI;

public class InventorySwitcher : MonoBehaviour
{
    [SerializeField] private InventoryUIManager stoneUI;                  // túi đồ 1 (Stone only) – script cũ đã gọn
    [SerializeField] private EquipmentInventoryUIManager equipUI;         // túi đồ 2 (script mới)
    [SerializeField] private Button switchButton;                         // nút chuyển
    [SerializeField] private Button openInventoryButton;                  // nút mở túi đồ (ban đầu mở Stone)

    private bool showingStone = true;

    private void Start()
    {
        if (openInventoryButton != null)
            openInventoryButton.onClick.AddListener(OpenStoneFirst);

        if (switchButton != null)
            switchButton.onClick.AddListener(Switch);
    }

    private void OpenStoneFirst()
    {
        showingStone = true;
        stoneUI.ShowInventoryPanel(); // hàm cũ của bạn
        equipUI.HidePanel();
    }

    private void Switch()
    {
        showingStone = !showingStone;
        if (showingStone)
        {
            stoneUI.ShowInventoryPanel();
            equipUI.HidePanel();
        }
        else
        {
            equipUI.ShowPanel();
            stoneUI.HideInventoryPanel();
        }
    }
}
