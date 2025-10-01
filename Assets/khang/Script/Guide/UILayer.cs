using UnityEngine;
using UnityEngine.EventSystems; // Thêm để kiểm tra UI interaction

public class UILayer : MonoBehaviour
{
    public static UILayer Instance { get; private set; }

    [SerializeField] private GameObject dungeonPanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject guideCanvas;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CameraController cameraController;

    private GameObject currentActivePanel;

    private void Awake()
    {
        // Đảm bảo chỉ có một instance của UILayer
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
        // Ẩn tất cả các panel khi bắt đầu
        HideAllPanels();
        // Đảm bảo CanvasGroup được cấu hình đúng khi khởi tạo
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
        if (currentActivePanel != null && currentActivePanel != panel)
        {
            currentActivePanel.SetActive(false);
            var oldCanvasGroup = currentActivePanel.GetComponent<CanvasGroup>();
            if (oldCanvasGroup != null) oldCanvasGroup.blocksRaycasts = false;
        }

        panel.SetActive(true);
        currentActivePanel = panel;

        var canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
        else
        {
            Debug.LogWarning($"Panel {panel.name} does not have CanvasGroup component! Adding one.");
            canvasGroup = panel.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        DisablePlayerAndCamera(true);
    }

    public void HideAllPanels()
    {
        // Ẩn tất cả các panel
        if (currentActivePanel != null)
        {
            currentActivePanel.SetActive(false);
            var canvasGroup = currentActivePanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
            currentActivePanel = null;
        }

        if (dungeonPanel != null)
        {
            dungeonPanel.SetActive(false);
            var canvasGroup = dungeonPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        }
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            var canvasGroup = inventoryPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        }
        if (guideCanvas != null)
        {
            guideCanvas.SetActive(false);
            var canvasGroup = guideCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        }

        // Kích hoạt lại điều khiển người chơi và camera
        DisablePlayerAndCamera(false);
    }

    // Thêm thuộc tính để kiểm tra xem UI có đang hiển thị không
    public bool IsUIPanelActive => currentActivePanel != null;

    private void DisablePlayerAndCamera(bool disable)
    {
        if (playerController != null)
            playerController.isMovementEnabled = !disable;
        if (cameraController != null)
            cameraController.isCameraControlEnabled = !disable;

        Cursor.lockState = disable ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = disable;
    }

    // Hàm mới để cấu hình CanvasGroup ban đầu
    private void ConfigureCanvasGroups(bool blocksRaycasts)
    {
        var panels = new[] { dungeonPanel, inventoryPanel, guideCanvas };
        foreach (var panel in panels)
        {
            if (panel != null)
            {
                var canvasGroup = panel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = panel.AddComponent<CanvasGroup>();
                    Debug.Log($"Added CanvasGroup to {panel.name}");
                }
                canvasGroup.blocksRaycasts = blocksRaycasts;
                canvasGroup.interactable = true; // Đảm bảo panel có thể tương tác
            }
        }
    }

    // Hàm kiểm tra xem chuột có đang trên UI không
    public bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}