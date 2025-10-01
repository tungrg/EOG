using UnityEngine;

public class MinimapToggle : MonoBehaviour
{
    [Header("Gán UI Minimap")]
    public GameObject miniMapSmall;  // UI minimap nhỏ
    public GameObject miniMapBig;    // UI minimap lớn

    private bool isBig = false;      // trạng thái hiện tại

    // Hàm toggle
    public void ToggleMap()
    {
        isBig = !isBig; // đảo trạng thái

        // Set hiển thị
        miniMapSmall.SetActive(!isBig);
        miniMapBig.SetActive(isBig);
    }
}
