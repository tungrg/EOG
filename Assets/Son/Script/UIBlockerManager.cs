using UnityEngine;
using System.Collections.Generic;

public class UIBlockerManager : MonoBehaviour
{
    [Header("Tutorial Panels cần khóa input ngoài khi active")]
    public List<GameObject> tutorialPanels = new List<GameObject>();

    [Header("References")]
    public PlayerController playerController;
    public CameraController cameraController;
    public GameObject globalBlockerPrefab; // 1 UI Panel full screen (Image trong Canvas, raycastTarget = true)

    [Header("World Objects cần khóa click")]
    public List<GameObject> worldClickables = new List<GameObject>();

    private GameObject globalBlockerInstance;
    private Dictionary<GameObject, bool> colliderStates = new Dictionary<GameObject, bool>();

    void Start()
    {
        if (playerController == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p) playerController = p.GetComponent<PlayerController>();
        }

        if (cameraController == null)
            cameraController = FindObjectOfType<CameraController>();

        // Spawn blocker trong Canvas (ẩn mặc định)
        if (globalBlockerPrefab != null)
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                globalBlockerInstance = Instantiate(globalBlockerPrefab, canvas.transform);
                globalBlockerInstance.SetActive(false);
            }
        }

        // Lưu trạng thái collider ban đầu
        foreach (var obj in worldClickables)
        {
            if (obj != null)
            {
                Collider col = obj.GetComponent<Collider>();
                if (col != null)
                    colliderStates[obj] = col.enabled;
            }
        }
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        bool anyPanelActive = false;

        foreach (var panel in tutorialPanels)
        {
            if (panel != null && panel.activeInHierarchy) // 🔹 dùng activeInHierarchy để check chính xác cả khi panel con
            {
                anyPanelActive = true;
                break;
            }
        }

        if (anyPanelActive)
            BlockInputs();
        else
            UnblockInputs();
    }

    void BlockInputs()
    {
        if (playerController != null) playerController.isMovementEnabled = false;
        if (cameraController != null) cameraController.isCameraControlEnabled = false;

        if (globalBlockerInstance != null && !globalBlockerInstance.activeSelf)
            globalBlockerInstance.SetActive(true);

        // 🔹 Disable collider để không click được object trong world
        foreach (var obj in worldClickables)
        {
            if (obj == null) continue;
            Collider col = obj.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    void UnblockInputs()
    {
        if (playerController != null) playerController.isMovementEnabled = true;
        if (cameraController != null) cameraController.isCameraControlEnabled = true;

        if (globalBlockerInstance != null && globalBlockerInstance.activeSelf)
            globalBlockerInstance.SetActive(false);

        // 🔹 Khôi phục trạng thái collider
        foreach (var obj in worldClickables)
        {
            if (obj == null) continue;
            Collider col = obj.GetComponent<Collider>();
            if (col != null && colliderStates.ContainsKey(obj))
                col.enabled = colliderStates[obj];
        }
    }

    public void RegisterPanel(GameObject panel)
    {
        if (!tutorialPanels.Contains(panel))
            tutorialPanels.Add(panel);
    }

    public void UnregisterPanel(GameObject panel)
    {
        if (tutorialPanels.Contains(panel))
            tutorialPanels.Remove(panel);
    }
}
