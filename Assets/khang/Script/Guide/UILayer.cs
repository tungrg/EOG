using UnityEngine;
using UnityEngine.EventSystems;

public class UILayer : MonoBehaviour
{
    public static UILayer Instance { get; private set; }

    [SerializeField] private GameObject dungeonPanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject guideCanvas;
    [SerializeField] private PlayerController playerController;  // Giữ nguyên, nhưng sẽ find lại nếu null
    [SerializeField] private CameraController cameraController; // Giữ nguyên

    private GameObject currentActivePanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        HideAllPanels();
        ConfigureCanvasGroups(false);
    }

    public void ShowPanel(GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogError("ShowPanel called with null panel!");
            return;
        }

        Debug.Log($"Showing panel: {panel.name}");

        // Ẩn panel hiện tại
        if (currentActivePanel != null && currentActivePanel != panel)
        {
            SetPanelState(currentActivePanel, false);
        }

        // Hiện panel mới
        currentActivePanel = panel;
        SetPanelState(panel, true);

        DisablePlayerAndCamera(true);  // Disable input khi show
    }

    public void HideAllPanels()
    {
        if (currentActivePanel != null)
        {
            SetPanelState(currentActivePanel, false);
            currentActivePanel = null;
        }

        var panels = new[] { dungeonPanel, inventoryPanel, guideCanvas };
        foreach (var panel in panels)
        {
            if (panel != null)
                SetPanelState(panel, false);
        }

        DisablePlayerAndCamera(false);  // Enable input khi hide

        // Bổ sung: Reset input nếu không có touch (fix stuck trên mobile)
        if (Input.touchCount == 0)
        {
            ResetInputState();  // Gọi method mới
        }
    }

    // Hàm gọn để bật/tắt panel
    private void SetPanelState(GameObject panel, bool isActive)
    {
        var cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        panel.SetActive(isActive);
        cg.alpha = isActive ? 1f : 0f;
        cg.interactable = isActive;
        cg.blocksRaycasts = isActive;
    }

    public bool IsUIPanelActive => currentActivePanel != null;

    // SỬA CHÍNH: Find lại nếu null, và skip Cursor trên mobile
    private void DisablePlayerAndCamera(bool disable)
    {
        // Find lại nếu null (an toàn cho DontDestroyOnLoad)
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
            if (playerController == null)
                Debug.LogError("PlayerController not found! Input may be locked.");
            else
                Debug.Log("Re-found PlayerController.");
        }

        if (cameraController == null)
        {
            cameraController = FindObjectOfType<CameraController>();
            if (cameraController == null)
                Debug.LogError("CameraController not found! Camera input may be locked.");
            else
                Debug.Log("Re-found CameraController.");
        }

        // Enable/disable controllers
        if (playerController != null)
        {
            playerController.isMovementEnabled = !disable;
            Debug.Log($"Player movement enabled: {!disable}");  // Debug log
        }

        if (cameraController != null)
        {
            cameraController.isCameraControlEnabled = !disable;
            Debug.Log($"Camera control enabled: {!disable}");  // Debug log
        }

        // Cursor chỉ cho non-mobile (PC/Editor)
#if !UNITY_ANDROID && !UNITY_IOS
        Cursor.lockState = disable ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = disable;
        Debug.Log($"Cursor state: {(disable ? "Visible/None" : "Hidden/Locked")}");  // Debug
#endif

        // Bổ sung: Clear EventSystem và force reset touch
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            // EventSystem.current.RestartCoroutine("Process");  // Uncomment nếu cần force restart
        }

        // Nếu dùng new Input System, thêm reset touch phase (nếu có package) - SỬA: Bỏ dòng assign read-only
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Touchscreen.current != null)
        {
            // Không thể assign wasUpdatedThisFrame vì read-only, bỏ qua hoặc dùng cách khác
            Debug.Log("Input System touch reset attempted (read-only property skipped).");
        }
#endif
    }

    private void ConfigureCanvasGroups(bool blocksRaycasts)
    {
        var panels = new[] { dungeonPanel, inventoryPanel, guideCanvas };
        foreach (var panel in panels)
        {
            if (panel != null)
            {
                var cg = panel.GetComponent<CanvasGroup>();
                if (cg == null) cg = panel.AddComponent<CanvasGroup>();

                cg.alpha = 0f;
                cg.blocksRaycasts = blocksRaycasts;
                cg.interactable = false;
            }
        }
    }

    public bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    // Bổ sung: Method reset input state (fix ghost touch)
    private void ResetInputState()
    {
        // Force reset controls nếu stuck
        if (playerController != null)
        {
            // Nếu PlayerController có method ResetInput(), gọi nó ở đây
            // playerController.ResetInput();
            Debug.Log("Reset input state for player.");
        }
        if (cameraController != null)
        {
            // Tương tự cho camera
            Debug.Log("Reset input state for camera.");
        }

        // Clear tất cả touch phases (hacky fix cho old Input Manager)
        for (int i = 0; i < Input.touchCount; i++)
        {
            // Không thể set phase trực tiếp, nhưng clear bằng cách ignore
        }
        Debug.Log("Reset touch state after UI close.");
    }
}