using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Tooltip("The higher it is, the faster the camera rotates.")]
    public float sensitivity = 0.5f;
    [Tooltip("Camera Y rotation limits. X is max down, Y is max up.")]
    public Vector2 cameraLimit = new Vector2(-30f, 60f);
    [Tooltip("Distance from the player to the camera.")]
    public float distanceFromPlayer = 5f;
    [Tooltip("Smoothing factor for camera movement and rotation.")]
    public float smoothTime = 0.2f;
    [Tooltip("Layer mask for camera collision detection.")]
    public LayerMask collisionLayers;
    [Tooltip("Minimum and maximum zoom distances.")]
    public Vector2 zoomLimits = new Vector2(2f, 8f);

    private Transform player;
    private float mouseX, mouseY;
    private Vector3 currentPositionVelocity;
    private Camera mainCamera;
    public bool isCameraControlEnabled = true;

    private Vector2 touchStartPos;
    private bool isTouching = false;
    private float initialPinchDistance;

    // 🔹 Biến mới để lọc vị trí player
    private Vector3 smoothedTarget;
    private Vector3 targetVelocity;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        if (!player)
        {
            Debug.LogError("Player with tag 'Player' not found!");
            enabled = false;
            return;
        }

        mainCamera = Camera.main;
        if (!mainCamera)
        {
            Debug.LogError("Main Camera not found!");
            enabled = false;
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        mouseX = transform.eulerAngles.y;
        mouseY = transform.eulerAngles.x;

        // Khởi tạo smoothedTarget tại vị trí ban đầu
        smoothedTarget = player.position + new Vector3(0, 1.5f, 0);
    }

    void LateUpdate()
    {
        if (!player || !mainCamera || !isCameraControlEnabled) return;

        HandleTouchInput();

        // Xoay camera theo input
        Quaternion targetRotation = Quaternion.Euler(mouseY, mouseX, 0);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            smoothTime * Time.deltaTime * 60f
        );

        // 🔹 Lọc vị trí player để giảm rung
        Vector3 rawTarget = player.position + new Vector3(0, 1.5f, 0);
        smoothedTarget = Vector3.SmoothDamp(smoothedTarget, rawTarget, ref targetVelocity, 0.05f);

        // Tính toán vị trí camera mong muốn
        Vector3 cameraOffset = transform.rotation * new Vector3(0, 0, -distanceFromPlayer);
        Vector3 desiredPosition = smoothedTarget + cameraOffset;

        // Raycast tránh xuyên tường
        desiredPosition = AdjustCameraForCollisions(smoothedTarget, desiredPosition);

        // Làm mượt di chuyển camera
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentPositionVelocity,
            smoothTime
        );

        // Camera luôn nhìn vào target đã lọc
        mainCamera.transform.LookAt(smoothedTarget);
    }

    void HandleTouchInput()
    {
        // Xử lý xoay camera: Kiểm tra từng touch riêng lẻ, bỏ qua nếu nó ở nửa trái (joystick)
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.position.x > Screen.width * 0.5f) // Chỉ xử lý touch ở nửa phải
            {
                if (touch.phase == TouchPhase.Began)
                {
                    touchStartPos = touch.position;
                    isTouching = true;
                }
                else if (touch.phase == TouchPhase.Moved && isTouching)
                {
                    Vector2 delta = touch.deltaPosition;
                    mouseX += delta.x * sensitivity * Time.deltaTime;
                    mouseY -= delta.y * sensitivity * Time.deltaTime;
                    mouseY = Mathf.Clamp(mouseY, cameraLimit.x, cameraLimit.y);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    isTouching = false;
                }
            }
        }

        // Xử lý pinch zoom riêng: Chỉ nếu có đúng 2 touches và cả hai ở nửa phải (hoặc toàn màn hình nếu muốn linh hoạt hơn)
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            // Kiểm tra cả hai ở nửa phải (giữ nguyên logic cũ, hoặc bỏ điều kiện này để zoom toàn màn hình)
            if (touch0.position.x > Screen.width * 0.5f && touch1.position.x > Screen.width * 0.5f)
            {
                if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
                {
                    initialPinchDistance = Vector2.Distance(touch0.position, touch1.position);
                }
                else if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
                {
                    float currentPinchDistance = Vector2.Distance(touch0.position, touch1.position);
                    float pinchDelta = initialPinchDistance - currentPinchDistance;
                    distanceFromPlayer += pinchDelta * sensitivity * 0.01f;
                    distanceFromPlayer = Mathf.Clamp(distanceFromPlayer, zoomLimits.x, zoomLimits.y);
                    initialPinchDistance = currentPinchDistance;
                }
            }
        }
    }

    Vector3 AdjustCameraForCollisions(Vector3 from, Vector3 to)
    {
        RaycastHit hit;
        if (Physics.Linecast(from, to, out hit, collisionLayers))
        {
            float adjustedDistance = Vector3.Distance(from, hit.point) - 0.2f;
            adjustedDistance = Mathf.Max(zoomLimits.x, adjustedDistance);
            Vector3 hitPos = from + transform.rotation * new Vector3(0, 0, -adjustedDistance);

            return Vector3.Lerp(to, hitPos, 0.8f);
        }

        return to;
    }
}
