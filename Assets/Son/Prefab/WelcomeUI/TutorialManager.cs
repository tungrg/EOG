using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [Header("PlayerPrefs")]
    public string prefsKey = "HasSeenTutorial_v1";

    [Header("Panels - assign in Inspector")]
    public GameObject welcomePanel;
    public GameObject followArrowPanel;
    public GameObject clickEnemyPanel;

    [Header("Arrow")]
    public GameObject arrowPrefab;
    public float arrowHeight = 2f;

    [Header("References (optional override)")]
    public Transform firstEnemy;
    public Transform playerTransform;

    [Header("Settings")]
    public float proximityToShowClickPanel = 3f;

    [Header("Blocker UI")]
    public GameObject blockerPanel; // UI Panel full màn hình, Image alpha=0, RaycastTarget=true

    // internals
    private GameObject arrowInstance;
    private PlayerController playerController;
    private CameraController cameraController;
    private bool tutorialRunning = false;

    void Start()
    {
        if (PlayerPrefs.GetInt(prefsKey, 0) == 1)
            return;

        if (playerTransform == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (playerTransform != null)
            playerController = playerTransform.GetComponent<PlayerController>();

        cameraController = FindObjectOfType<CameraController>();

        if (firstEnemy == null)
        {
            var triggers = FindObjectsOfType<EnemyTrigger>();
            foreach (var t in triggers)
            {
                if (!t.IsLocked)
                {
                    firstEnemy = t.transform;
                    break;
                }
            }
        }

        if (blockerPanel != null)
            blockerPanel.SetActive(false);

        StartCoroutine(TutorialSequence());
    }

    IEnumerator TutorialSequence()
    {
        tutorialRunning = true;

        // Panel 1
        if (welcomePanel != null)
        {
            BlockInputs(true);
            welcomePanel.SetActive(true);
        }
        yield return new WaitUntil(() => welcomePanel == null || !welcomePanel.activeSelf);
        BlockInputs(false);

        // Arrow + Panel 2
        SpawnArrow();

        if (followArrowPanel != null)
        {
            BlockInputs(true);
            followArrowPanel.SetActive(true);
        }
        yield return new WaitUntil(() => followArrowPanel == null || !followArrowPanel.activeSelf);
        BlockInputs(false);

        // cho phép player di chuyển + xoay cam
        if (playerController != null) playerController.isMovementEnabled = true;
        if (cameraController != null) cameraController.isCameraControlEnabled = true;

        // chờ player đi gần enemy
        while (true)
        {
            if (firstEnemy == null || playerTransform == null) break;
            float d = Vector3.Distance(playerTransform.position, firstEnemy.position);
            if (d <= proximityToShowClickPanel)
            {
                if (clickEnemyPanel != null)
                {
                    BlockInputs(true);
                    clickEnemyPanel.SetActive(true);
                }
                break;
            }
            yield return null;
        }

        if (clickEnemyPanel != null) yield return new WaitUntil(() => !clickEnemyPanel.activeSelf);
        BlockInputs(false);

        if (arrowInstance != null) Destroy(arrowInstance);
        PlayerPrefs.SetInt(prefsKey, 1);
        PlayerPrefs.Save();
        tutorialRunning = false;
    }

    void SpawnArrow()
    {
        if (arrowPrefab == null || firstEnemy == null) return;
        arrowInstance = Instantiate(arrowPrefab);
        var ac = arrowInstance.GetComponent<ArrowController>();
        if (ac != null) ac.SetTargets(playerTransform, firstEnemy);
    }

    void BlockInputs(bool blocked)
    {
        if (playerController != null) playerController.isMovementEnabled = !blocked;
        if (cameraController != null) cameraController.isCameraControlEnabled = !blocked;
        if (blockerPanel != null) blockerPanel.SetActive(blocked);
    }

    // Button UI gọi
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
        PlayerPrefs.SetInt(prefsKey, 0);
        PlayerPrefs.Save();
    }

    public bool IsTutorialRunning() => tutorialRunning;
}
