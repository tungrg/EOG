using UnityEngine;

public class UIAnchor : MonoBehaviour
{
    public Transform objectToFollow; // Transform của kẻ thù (null nếu cố định)
    private Camera mainCamera;
    private Vector3 offset; // Offset từ EnemyData
    private Vector3 fixedPosition; // Vị trí cố định nếu IsFixedHealthBar = true

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            DebugLogger.LogError("Main Camera not found for UIAnchor on " + gameObject.name);
            return;
        }

        // Lấy offset từ EnemyData nếu có
        Enemy enemy = objectToFollow ? objectToFollow.GetComponent<Enemy>() : null;
        if (enemy != null && enemy.GetData() != null)
        {
            offset = enemy.GetData().HealthBarOffset;
            if (enemy.GetData().IsFixedHealthBar)
            {
                fixedPosition = transform.position; // Lưu vị trí ban đầu nếu cố định
                objectToFollow = null; // Không follow nếu cố định
            }
        }
        else
        {
            offset = new Vector3(0, 1.5f, 0); // Offset mặc định nếu không tìm thấy EnemyData
        }
    }

    public void SetFollowTransform(Transform followTransform)
    {
        objectToFollow = followTransform;
        if (objectToFollow == null)
        {
            fixedPosition = transform.position; // Lưu vị trí cố định nếu không follow
        }
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;

        // Hướng thanh HP về camera
        transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);

        // Cập nhật vị trí
        if (objectToFollow != null)
        {
            // Follow enemy
            transform.position = objectToFollow.position + offset;
        }
        else
        {
            // Giữ vị trí cố định
            transform.position = fixedPosition;
        }
    }
}