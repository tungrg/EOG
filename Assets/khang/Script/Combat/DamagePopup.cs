using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float disappearTimer = 1f;
    private Vector3 moveVector;
    private ObjectPool objectPool;
    private Camera mainCamera;

    public static void Create(Vector3 position, int damage, bool isCritical)
    {
        ObjectPool pool = FindFirstObjectByType<CombatManager>().GetComponent<ObjectPool>(); // Thay đổi từ FindObjectOfType
        if (pool == null)
        {
            DebugLogger.LogError("ObjectPool not found in CombatManager for DamagePopup.");
            return;
        }

        GameObject prefab = Resources.Load<GameObject>("Prefabs/DamagePopup");
        if (prefab == null)
        {
            DebugLogger.LogError("DamagePopup prefab not found in Resources/Prefabs. Ensure 'DamagePopup.prefab' exists in Assets/Resources/Prefabs.");
            return;
        }

        // Khởi tạo pool nếu chưa có
        pool.InitializePool(prefab, 10); // Pool 10 instance cho DamagePopup
        GameObject popupObj = pool.GetObject(prefab.name, position, Quaternion.identity);
        if (popupObj == null)
        {
            DebugLogger.LogError("Failed to get DamagePopup from ObjectPool.");
            return;
        }

        var popup = popupObj.GetComponent<DamagePopup>();
        if (popup != null)
        {
            popup.Setup(damage, isCritical);
        }
        else
        {
            DebugLogger.LogError("DamagePopup component missing on instantiated prefab.");
        }
    }

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null)
        {
            textMesh = gameObject.AddComponent<TextMeshPro>();
            DebugLogger.LogWarning("TextMeshPro component was missing on DamagePopup and has been added dynamically.");
        }
        textMesh.fontSize = 15; // Đặt cỡ chữ thành 15
        textMesh.alignment = TextAlignmentOptions.Center;
        moveVector = new Vector3(0, 1f, 0);
        objectPool = FindFirstObjectByType<CombatManager>().GetComponent<ObjectPool>(); // Thay đổi từ FindObjectOfType
        if (objectPool == null)
        {
            DebugLogger.LogError("ObjectPool not found in CombatManager during DamagePopup Awake.");
        }
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            DebugLogger.LogError("MainCamera not found in scene for DamagePopup.");
        }
    }

    private void Setup(int damage, bool isCritical)
    {
        if (textMesh == null)
        {
            DebugLogger.LogError("TextMeshPro is null in DamagePopup.Setup.");
            return;
        }
        textMesh.text = damage.ToString();
        textMesh.color = isCritical ? new Color(1, 0, 0, 1) : new Color(1, 1, 1, 1); // Reset alpha
        disappearTimer = 1f; // Reset timer khi tái sử dụng
        gameObject.SetActive(true); // Đảm bảo GameObject được kích hoạt
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return; // Ngăn Update chạy nếu không active

        // Di chuyển popup
        transform.position += moveVector * Time.deltaTime;

        // Quay popup về phía camera
        if (mainCamera != null)
        {
            Vector3 directionToCamera = (mainCamera.transform.position - transform.position).normalized;
            directionToCamera.y = 0; // Giữ hướng ngang để tránh nghiêng
            if (directionToCamera != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(-directionToCamera); // Quay ngược để mặt chữ hướng về camera
                transform.rotation = targetRotation;
            }
        }

        // Xử lý timer và hiệu ứng mờ dần
        disappearTimer -= Time.deltaTime;
        if (disappearTimer <= 0)
        {
            if (objectPool != null)
            {
                objectPool.ReturnObject(gameObject); // Trả về pool
            }
            else
            {
                DebugLogger.LogError("ObjectPool is null in DamagePopup.Update. Destroying GameObject.");
                Destroy(gameObject);
            }
        }
        else if (disappearTimer < 0.3f && textMesh != null) // Mờ dần trong 0.3 giây cuối
        {
            Color color = textMesh.color;
            color.a = disappearTimer / 0.3f; // Giảm alpha tuyến tính
            textMesh.color = color;
        }
    }
}