using UnityEngine;

public class CameraMinimapFollow : MonoBehaviour
{
    public Transform player;     // Nhân vật chính
    public float height = 200f;  // Độ cao của camera
    public Vector3 offset = Vector3.zero; // Dịch thêm nếu cần

    void LateUpdate()
    {
        if (player == null) return;

        // Giữ nguyên góc quay, chỉ thay đổi vị trí theo Player
        transform.position = new Vector3(
            player.position.x + offset.x,
            height,
            player.position.z + offset.z
        );
    }
}
