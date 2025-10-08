using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TutorialObjectDestroy : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private GameObject overlayPanel1;    // Overlay đầu tiên
    [SerializeField] private Button tutorialButton;       // Nút tutorial đầu tiên
    //[SerializeField] private GameObject overlayPanel2;    // Overlay thứ 2
    [SerializeField] private Button equipButton;          // Nút Equip
    [SerializeField] private List<GameObject> watchedObjects = new List<GameObject>(); // Danh sách object theo dõi

    private bool triggered = false;

    void Update()
    {
        if (triggered) return;

        // Kiểm tra nếu có ít nhất 1 object trong list bị destroy
        if (PlayerPrefs.GetInt("FirstTimeDestroyTutorial", 1) == 1)
        {
            foreach (var obj in watchedObjects)
            {
                if (obj == null) // object này đã bị Destroy
                {
                    triggered = true;
                    StartCoroutine(RunTutorial());
                    break; // chỉ cần trigger 1 lần
                }
            }
        }
    }

    private IEnumerator RunTutorial()
    {
        // Tìm tất cả button trong scene
        Button[] allButtons = FindObjectsOfType<Button>();

        // --- Bước 1 ---
        overlayPanel1.SetActive(true);

        foreach (var btn in allButtons) btn.interactable = false;
        tutorialButton.interactable = true;

        bool tutorialPressed = false;
        tutorialButton.onClick.AddListener(() => tutorialPressed = true);
        yield return new WaitUntil(() => tutorialPressed);

        // --- Bước 2 ---
        overlayPanel1.SetActive(false);
        //overlayPanel2.SetActive(true);

        foreach (var btn in allButtons) btn.interactable = false;
        equipButton.interactable = true;

        bool equipPressed = false;
        equipButton.onClick.AddListener(() => equipPressed = true);
        yield return new WaitUntil(() => equipPressed);

        // --- Kết thúc ---
        //overlayPanel2.SetActive(false);
        foreach (var btn in allButtons) btn.interactable = true;

        PlayerPrefs.SetInt("FirstTimeDestroyTutorial", 0);
        PlayerPrefs.Save();

        Debug.Log("[Tutorial] Destroy tutorial completed.");
    }
}
