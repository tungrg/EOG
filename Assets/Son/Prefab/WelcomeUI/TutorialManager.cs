using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    [Header("PlayerPrefs Keys")]
    public string prefsKey_AllTutorialDone = "HasSeenTutorial_v3";
    public string prefsKey_FirstEnemyDone = "HasDoneFirstTutorialStep";
    public string prefsKey_TutorialProgress = "TutorialEnemyProgress"; // số enemy tutorial đã xong

    [Header("Panels - assign in Inspector")]
    public GameObject welcomePanel;
    public GameObject followArrowPanel;
    public GameObject clickEnemyPanel;

    [Header("Arrow")]
    public GameObject arrowPrefab;
    public float arrowHeight = 2f;

    [Header("References")]
    public Transform playerTransform;

    [Header("Settings")]
    public float proximityToShowClickPanel = 3f;
    public List<Transform> enemiesToTeach = new List<Transform>(); // 3 con quái đầu tiên

    [Header("Enemy Arrows (3 con đầu)")]
    public List<GameObject> enemyArrows = new List<GameObject>(); // gán 3 prefab arrow này vào từng con quái trong Inspector

    [Header("Blocker UI")]
    public GameObject blockerPanel;

    // internals
    private GameObject arrowInstance;
    private PlayerController playerController;
    private CameraController cameraController;
    private bool tutorialRunning = false;

    void Start()
    {
        if (PlayerPrefs.GetInt(prefsKey_AllTutorialDone, 0) == 1)
            return;

        if (playerTransform == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (playerTransform != null)
            playerController = playerTransform.GetComponent<PlayerController>();

        cameraController = FindObjectOfType<CameraController>();

        if (blockerPanel != null)
            blockerPanel.SetActive(false);

        StartCoroutine(TutorialSequence());

        SpawnArrowForNextEnemy(); // 🟢 ADDED: Tự spawn mũi tên đúng tiến trình hiện tại

        ShowEnemyArrow(PlayerPrefs.GetInt(prefsKey_TutorialProgress, 0));
    }

    IEnumerator TutorialSequence()
    {
        tutorialRunning = true;

        if (enemiesToTeach.Count == 0)
        {
            Debug.LogWarning("TutorialManager: Chưa gán 3 enemy đầu tiên vào danh sách enemiesToTeach!");
            tutorialRunning = false;
            yield break;
        }

        bool firstTutorialDone = PlayerPrefs.GetInt(prefsKey_FirstEnemyDone, 0) == 1;
        int tutorialProgress = PlayerPrefs.GetInt(prefsKey_TutorialProgress, 0); // 0–3

        int startIndex = tutorialProgress; // bắt đầu từ con chưa xong

        if (startIndex >= enemiesToTeach.Count)
        {
            FinishTutorial();
            yield break;
        }

        // Nếu chưa xong bước đầu (welcome + follow)
        if (!firstTutorialDone && startIndex == 0)
        {
            Transform firstEnemy = enemiesToTeach[0];

            if (welcomePanel != null)
            {
                BlockInputs(true);
                welcomePanel.SetActive(true);
            }
            yield return new WaitUntil(() => welcomePanel == null || !welcomePanel.activeSelf);
            BlockInputs(false);

            SpawnArrow(firstEnemy);

            if (followArrowPanel != null)
            {
                BlockInputs(true);
                followArrowPanel.SetActive(true);
            }
            yield return new WaitUntil(() => followArrowPanel == null || !followArrowPanel.activeSelf);
            BlockInputs(false);

            if (arrowInstance != null) Destroy(arrowInstance);

            if (playerController != null) playerController.isMovementEnabled = true;
            if (cameraController != null) cameraController.isCameraControlEnabled = true;

            yield return StartCoroutine(WaitAndShowClickPanel(firstEnemy));
            MarkEnemyComplete(1);
            PlayerPrefs.SetInt(prefsKey_FirstEnemyDone, 1);
            PlayerPrefs.Save();

            startIndex = 1; // chuyển sang enemy kế
        }
        else
        {
            if (playerController != null) playerController.isMovementEnabled = true;
            if (cameraController != null) cameraController.isCameraControlEnabled = true;
        }

        // --- Enemy 2 trở đi ---
        for (int i = startIndex; i < enemiesToTeach.Count; i++)
        {
            var enemy = enemiesToTeach[i];
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

            yield return StartCoroutine(WaitAndShowClickPanel(enemy));
            MarkEnemyComplete(i + 1); // i là 0-based
        }

        FinishTutorial();
    }

    IEnumerator WaitAndShowClickPanel(Transform enemy)
    {
        if (enemy == null || playerTransform == null) yield break;
        if (!enemy.gameObject.activeInHierarchy) yield break;

        while (true)
        {
            if (enemy == null || playerTransform == null) yield break;
            if (!enemy.gameObject.activeInHierarchy) yield break;

            float d = Vector3.Distance(playerTransform.position, enemy.position);
            if (d <= proximityToShowClickPanel)
                break;

            yield return null;
        }

        if (clickEnemyPanel != null)
        {
            BlockInputs(true);
            clickEnemyPanel.SetActive(true);
            yield return new WaitUntil(() => !clickEnemyPanel.activeSelf);
            BlockInputs(false);
        }
    }

    void SpawnArrow(Transform target)
    {
        if (arrowPrefab == null || target == null) return;
        arrowInstance = Instantiate(arrowPrefab);
        var ac = arrowInstance.GetComponent<ArrowController>();
        if (ac != null) ac.SetTargets(playerTransform, target);
    }

    void BlockInputs(bool blocked)
    {
        if (playerController != null) playerController.isMovementEnabled = !blocked;
        if (cameraController != null) cameraController.isCameraControlEnabled = !blocked;
        if (blockerPanel != null) blockerPanel.SetActive(blocked);
    }

    // --- Helper methods ---
    void MarkEnemyComplete(int count)
    {
        int current = PlayerPrefs.GetInt(prefsKey_TutorialProgress, 0);
        if (count > current)
        {
            PlayerPrefs.SetInt(prefsKey_TutorialProgress, count);
            PlayerPrefs.Save();
        }
    }

    void FinishTutorial()
    {
        PlayerPrefs.SetInt(prefsKey_AllTutorialDone, 1);
        PlayerPrefs.Save();
        tutorialRunning = false;

        if (arrowInstance != null) // 🟢 ADDED
            Destroy(arrowInstance); // mũi tên biến mất vĩnh viễn
    }

    // UI callbacks
    public void CloseWelcome() { if (welcomePanel != null) welcomePanel.SetActive(false); }
    public void CloseFollowArrow() { if (followArrowPanel != null) followArrowPanel.SetActive(false); }
    public void CloseClickEnemy() { if (clickEnemyPanel != null) clickEnemyPanel.SetActive(false); }

    public void OnPlayerClickedEnemy()
    {
        if (clickEnemyPanel != null && clickEnemyPanel.activeSelf)
            clickEnemyPanel.SetActive(false);
    }

    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(prefsKey_AllTutorialDone);
        PlayerPrefs.DeleteKey(prefsKey_FirstEnemyDone);
        PlayerPrefs.DeleteKey(prefsKey_TutorialProgress);
        PlayerPrefs.Save();
    }

    public bool IsTutorialRunning() => tutorialRunning;

    // 🟢 ADDED: Spawn mũi tên cho enemy tương ứng với tiến trình hiện tại
    void SpawnArrowForNextEnemy()
    {
        int tutorialProgress = PlayerPrefs.GetInt(prefsKey_TutorialProgress, 0);
        if (tutorialProgress >= enemiesToTeach.Count)
        {
            if (arrowInstance != null) Destroy(arrowInstance);
            return;
        }

        Transform nextEnemy = enemiesToTeach[tutorialProgress];
        if (nextEnemy != null && nextEnemy.gameObject.activeInHierarchy)
        {
            SpawnArrow(nextEnemy);
        }
    }

    // 🟢 ADDED: Gọi từ script Enemy khi enemy chết
    public void OnEnemyKilled(Transform enemyTransform)
    {
        int tutorialProgress = PlayerPrefs.GetInt(prefsKey_TutorialProgress, 0);

        if (tutorialProgress < enemiesToTeach.Count &&
            enemiesToTeach[tutorialProgress] == enemyTransform)
        {
            MarkEnemyComplete(tutorialProgress + 1);

            if (arrowInstance != null)
                Destroy(arrowInstance);

            SpawnArrowForNextEnemy();
        }
        ShowEnemyArrow(PlayerPrefs.GetInt(prefsKey_TutorialProgress, 0));
    }

    void ShowEnemyArrow(int index)
    {
        if (enemyArrows == null || enemyArrows.Count == 0) return;

        for (int i = 0; i < enemyArrows.Count; i++)
        {
            if (enemyArrows[i] != null)
                enemyArrows[i].SetActive(i == index);
        }
    }
}
