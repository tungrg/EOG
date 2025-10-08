using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class EnemyTrigger : MonoBehaviour
{
    [SerializeField] public EnemyTeamData enemyTeamData;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float tapInteractionRange = 10f;
    [SerializeField] private GameObject highlightEffect;
    [SerializeField] private Vector3 vfxOffset = new Vector3(0, 1.5f, 0);
    [SerializeField] private GameObject teamInfoPanel;
    [SerializeField] private GameObject enemyInfoEntryPrefab;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI enemyInfoNameHPText;
    [SerializeField] private Image enemyInfoAvatarImage;

    [Header("Tutorial")]
    public UnityEvent OnTeamInfoShown;

    private MapManager mapManager;
    private Transform playerTransform;
    private GameObject enemyInfoPanel;
    private Transform scrollViewContent;
    private bool isInRange;
    private bool isTeamInfoOpen;
    private bool isLocked = true;
    private Enemy enemyComponent;
    private GameObject myVfx;

    public bool IsLocked => isLocked;

    public void SetLocked(bool locked)
    {
        isLocked = locked;

        if (!isLocked)
        {
            if (myVfx == null && VFXHighlighter.Instance != null)
                myVfx = VFXHighlighter.Instance.ShowAt(this.transform, vfxOffset);
        }
        else
        {
            if (myVfx != null && VFXHighlighter.Instance != null)
            {
                VFXHighlighter.Instance.Hide(myVfx);
                myVfx = null;
            }
        }
    }

    private void OnDestroy()
    {
        if (myVfx != null && VFXHighlighter.Instance != null)
        {
            VFXHighlighter.Instance.Hide(myVfx);
            myVfx = null;
        }
    }

    void Start()
    {
        if (SceneManager.GetActiveScene().name != "Map")
        {
            Destroy(this);
            return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
        else
        {
            DebugLogger.LogError("No GameObject with tag 'Player' found in the scene.");
            return;
        }

        mapManager = FindFirstObjectByType<MapManager>();
        if (mapManager == null)
        {
            DebugLogger.LogError("MapManager not found in Map scene.");
            return;
        }

        enemyInfoPanel = mapManager.EnemyInfoPanel;
        if (enemyInfoPanel == null)
        {
            DebugLogger.LogError("EnemyInfoPanel not assigned in MapManager.");
            return;
        }

        scrollViewContent = teamInfoPanel.GetComponentInChildren<ScrollRect>()?.transform.Find("Viewport/Content");
        if (scrollViewContent == null)
        {
            DebugLogger.LogError("ScrollView Content not found in TeamInfoPanel.");
            return;
        }

        if (attackButton == null || closeButton == null)
        {
            DebugLogger.LogError("AttackButton or CloseButton not assigned in EnemyTrigger.");
            return;
        }

        enemyComponent = GetComponent<Enemy>();
        if (enemyComponent != null && enemyTeamData != null && enemyTeamData.Enemies.Count > 0)
            enemyComponent.SetData(enemyTeamData.Enemies[0]);

        if (highlightEffect != null) highlightEffect.SetActive(false);

        // Chỉ ẩn panel UI lúc đầu
        if (enemyInfoPanel != null) enemyInfoPanel.SetActive(false);
        if (teamInfoPanel != null) teamInfoPanel.SetActive(false);

        isTeamInfoOpen = false;
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name != "Map" || playerTransform == null || enemyTeamData == null || enemyTeamData.Enemies.Count == 0)
            return;

        float distance = Vector3.Distance(playerTransform.position, transform.position);
        isInRange = distance <= detectionRange;

        if (highlightEffect != null)
            highlightEffect.SetActive(isInRange && !isLocked);

        if (!isLocked)
            HandleTouchInput();
    }

    private void HandleTouchInput()
    {
        // Kiểm tra xem có UI nào đang mở hoặc chuột đang trên UI không
        if (UILayer.Instance != null && (UILayer.Instance.IsUIPanelActive || UILayer.Instance.IsPointerOverUI()))
        {
            return; // Thoát nếu có UI panel đang mở hoặc chuột đang trên UI
        }

        if (Input.touchCount > 0 && !isTeamInfoOpen)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began && !EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
                {
                    float distance = Vector3.Distance(playerTransform.position, transform.position);
                    if (distance <= tapInteractionRange)
                        ShowTeamInfo();
                }
            }
        }
    }

    void ShowTeamInfo()
    {
        // Kiểm tra xem có UI nào đang mở không
        if (UILayer.Instance != null && UILayer.Instance.IsUIPanelActive)
        {
            return; // Thoát nếu có UI panel khác đang mở
        }

        if (teamInfoPanel == null || scrollViewContent == null || enemyInfoEntryPrefab == null || enemyTeamData == null || enemyTeamData.Enemies.Count == 0)
        {
            DebugLogger.LogError("TeamInfoPanel, ScrollViewContent, EnemyInfoEntryPrefab, or EnemyTeamData is null/empty.");
            return;
        }

        foreach (Transform child in scrollViewContent)
            Destroy(child.gameObject);

        // Nếu có người lắng nghe, thông báo
        if (OnTeamInfoShown != null) OnTeamInfoShown.Invoke();
        teamInfoPanel.SetActive(true);
        GameObject EnemyLayout = GameObject.Find("EnemyInfoCanvas").transform.Find("EnemyLayout")?.gameObject;
        if (EnemyLayout == null)
        {
            DebugLogger.LogError("EnemyInfoCanvas/EnemyLayout GameObject not found in the scene.");
        }
        else
        {
            EnemyLayout.SetActive(true);
        }

        EnemyData mainEnemy = enemyTeamData.Enemies[0];
        if (enemyInfoNameHPText != null)
            enemyInfoNameHPText.text = $"<b>{mainEnemy.Name}</b>\nHP: {mainEnemy.HP}/{mainEnemy.MaxHP}";

        if (enemyInfoAvatarImage != null && mainEnemy.AvatarSprite != null)
        {
            enemyInfoAvatarImage.sprite = mainEnemy.AvatarSprite;
            enemyInfoAvatarImage.preserveAspect = true;
        }

        foreach (var enemy in enemyTeamData.Enemies)
        {
            GameObject entry = Instantiate(enemyInfoEntryPrefab, scrollViewContent);
            Image avatarImage = entry.GetComponentInChildren<Image>();
            TextMeshProUGUI infoText = entry.GetComponentInChildren<TextMeshProUGUI>();

            if (avatarImage != null && enemy.AvatarSprite != null)
            {
                avatarImage.sprite = enemy.AvatarSprite;
                avatarImage.preserveAspect = true;
            }

            if (infoText != null)
                infoText.text = $"<b>{enemy.Name}</b>\nElement: {enemy.Element}\nPath: {enemy.Path}";
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewContent.GetComponent<RectTransform>());
        Canvas.ForceUpdateCanvases();

        isTeamInfoOpen = true;

        var playerController = GameObject.FindWithTag("Player")?.GetComponent<PlayerController>();
        if (playerController != null) playerController.enabled = false;

        attackButton.gameObject.SetActive(true);
        attackButton.onClick.RemoveAllListeners();
        attackButton.onClick.AddListener(() => { StartCoroutine(InitiateAttackSequence()); });

        closeButton.gameObject.SetActive(true);
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(HideTeamInfo);
    }

    void HideTeamInfo()
    {
        if (teamInfoPanel != null) teamInfoPanel.SetActive(false);
        isTeamInfoOpen = false;

        attackButton.gameObject.SetActive(false);
        attackButton.onClick.RemoveAllListeners();

        closeButton.gameObject.SetActive(false);
        closeButton.onClick.RemoveAllListeners();
        GameObject EnemyLayout = GameObject.Find("EnemyInfoCanvas").transform.Find("EnemyLayout")?.gameObject;
        if (EnemyLayout == null)
        {
            DebugLogger.LogError("EnemyInfoCanvas/EnemyLayout GameObject not found in the scene.");
        }
        else
        {
            EnemyLayout.SetActive(false);
        }

        var playerController = GameObject.FindWithTag("Player")?.GetComponent<PlayerController>();
        if (playerController != null) playerController.enabled = true;

        var cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController != null) cameraController.isCameraControlEnabled = true;
    }

    void StartBattle()
    {
        if (mapManager == null) return;

        if (enemyTeamData != null && enemyTeamData.Enemies.Count > 0)
        {
            mapManager.StartBattle(enemyTeamData.Enemies);
            HideTeamInfo();
        }
    }

    public List<EnemyData> GetEnemyData() => enemyTeamData?.Enemies ?? new List<EnemyData>();

    public IEnumerator InitiateAttackSequence()
    {
        var playerController = GameObject.FindWithTag("Player")?.GetComponent<PlayerController>();
        if (playerController != null) playerController.enabled = false;

        var cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController != null) cameraController.isCameraControlEnabled = false;

        float animationDuration = 1f;
        if (enemyComponent != null && enemyTeamData != null && enemyTeamData.Enemies.Count > 0)
        {
            enemyComponent.SetAttacking(0);
            var skill = enemyTeamData.Enemies[0].Skills[0];
            animationDuration = GetAnimationDuration(enemyComponent, skill?.AnimationTrigger ?? "Attack1");
        }

        yield return new WaitForSeconds(animationDuration);

        if (enemyComponent != null) enemyComponent.ResetAttacking();

        if (playerController != null) playerController.enabled = true;
        if (cameraController != null) cameraController.isCameraControlEnabled = true;

        StartBattle();
    }

    private float GetAnimationDuration(Enemy enemy, string animationTrigger)
    {
        var animator = enemy.GetComponent<Animator>();
        if (animator == null) return 1f;

        foreach (var clip in animator.runtimeAnimatorController.animationClips)
            if (clip.name.Contains(animationTrigger))
                return clip.length;

        return 1f;
    }

    public void SetHighlight(bool active)
    {
        if (highlightEffect != null)
            highlightEffect.SetActive(active);
    }
}