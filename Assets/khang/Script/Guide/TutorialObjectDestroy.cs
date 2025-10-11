using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TutorialObjectDestroy : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private GameObject overlayPanel1;
    [SerializeField] private GameObject overlayPanel2;
    [SerializeField] private GameObject overlayPanel3;
    [SerializeField] private GameObject overlayPanel4;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button buttonUp;
    [SerializeField] private EquipmentInventoryUIManager equipmentUIManager;
    [SerializeField] private List<GameObject> watchedObjects = new List<GameObject>();

    private bool triggered = false;

    void Update()
    {
        if (triggered) return;

        if (PlayerPrefs.GetInt("FirstTimeDestroyTutorial", 1) == 1)
        {
            foreach (var obj in watchedObjects)
            {
                if (obj == null)
                {
                    triggered = true;
                    StartCoroutine(RunTutorial());
                    break;
                }
            }
        }
    }

    private IEnumerator RunTutorial()
    {
        Button[] allButtons = FindObjectsOfType<Button>();

        // --- Bước 1 ---
        overlayPanel1.SetActive(true);
        overlayPanel2.SetActive(false);
        overlayPanel3.SetActive(false);
        overlayPanel4.SetActive(false);

        SetButtonInteractable(allButtons, false);
        tutorialButton.interactable = true;

        bool tutorialPressed = false;
        UnityEngine.Events.UnityAction tutAction = () => tutorialPressed = true;
        tutorialButton.onClick.AddListener(tutAction);
        yield return new WaitUntil(() => tutorialPressed);
        tutorialButton.onClick.RemoveListener(tutAction);

        // --- Bước 2 ---
        overlayPanel1.SetActive(false);
        overlayPanel2.SetActive(true);

        SetButtonInteractable(allButtons, false);
        equipButton.interactable = true;

        bool equipPressed = false;
        UnityEngine.Events.UnityAction equipAction = () => equipPressed = true;
        equipButton.onClick.AddListener(equipAction);
        yield return new WaitUntil(() => equipPressed);
        equipButton.onClick.RemoveListener(equipAction);

        // --- Bước 3 ---
        overlayPanel2.SetActive(false);
        overlayPanel3.SetActive(true);
        SetButtonInteractable(allButtons, false);

        Transform slotsParent = null;
        if (equipmentUIManager != null)
        {
            var field = equipmentUIManager.GetType().GetField("slotsContainer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                slotsParent = field.GetValue(equipmentUIManager) as Transform;
        }

        if (slotsParent != null && slotsParent.childCount > 0)
        {
            // Giảm độ sáng các slot khác
            for (int i = 0; i < slotsParent.childCount; i++)
            {
                var img = slotsParent.GetChild(i).GetComponent<Image>();
                if (img != null)
                {
                    Color c = img.color;
                    c.a = (i == 0 ? 1f : 0.3f);
                    img.color = c;
                }

                var btn = slotsParent.GetChild(i).GetComponent<Button>();
                if (btn != null)
                    btn.interactable = (i == 0);
            }

            // Theo dõi nhấn slot đầu tiên
            Button firstSlotButton = slotsParent.GetChild(0).GetComponent<Button>();
            bool slotClicked = false;
            UnityEngine.Events.UnityAction slotAction = () => slotClicked = true;
            firstSlotButton.onClick.AddListener(slotAction);
            yield return new WaitUntil(() => slotClicked);
            firstSlotButton.onClick.RemoveListener(slotAction);

            // --- Reset lại màu & tương tác ---
            for (int i = 0; i < slotsParent.childCount; i++)
            {
                var img = slotsParent.GetChild(i).GetComponent<Image>();
                if (img != null)
                {
                    Color c = img.color;
                    c.a = 1f;
                    img.color = c;
                }

                var btn = slotsParent.GetChild(i).GetComponent<Button>();
                if (btn != null)
                    btn.interactable = true;
            }
        }

        // --- Bước 4 ---
        overlayPanel3.SetActive(false);
        overlayPanel4.SetActive(true);

        SetButtonInteractable(allButtons, false);
        if (buttonUp != null) buttonUp.interactable = true;

        bool upPressed = false;
        UnityEngine.Events.UnityAction upAction = () => upPressed = true;
        if (buttonUp != null) buttonUp.onClick.AddListener(upAction);
        yield return new WaitUntil(() => upPressed);
        if (buttonUp != null) buttonUp.onClick.RemoveListener(upAction);

        // --- Kết thúc ---
        overlayPanel4.SetActive(false);
        SetButtonInteractable(allButtons, true);

        PlayerPrefs.SetInt("FirstTimeDestroyTutorial", 0);
        PlayerPrefs.Save();
        triggered = false;

        Debug.Log("<color=green>[Tutorial]</color> Destroy tutorial completed!");
    }

    private void SetButtonInteractable(Button[] buttons, bool state)
    {
        foreach (var btn in buttons)
        {
            if (btn != null)
                btn.interactable = state;
        }
    }
}
