using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class GuideCategory
{
    public string categoryName;       // tên mục (vd: Giới Thiệu, Characters...)
    public GuideInfo[] guides;        // các trang trong mục này
}

[System.Serializable]
public class GuideInfo
{
    public string title;
    public Sprite image;
    [TextArea(3, 5)] public string description;
}

public class GuideManager : MonoBehaviour
{
    [Header("UI References")]
    public Image displayImage;
    public TMP_Text displayText;
    public TMP_Text pageText;
    public Button prevButton;
    public Button nextButton;

    [Header("Guide Data")]
    public GuideCategory[] categories;

    private int currentCategory = 0;   // mục hiện tại
    private int currentIndex = 0;      // trang hiện tại trong mục

    private void Start()
    {
        if (categories.Length > 0 && categories[0].guides.Length > 0)
        {
            ShowGuide(0, 0);
        }

        prevButton.onClick.AddListener(PrevPage);
        nextButton.onClick.AddListener(NextPage);
    }

    public void ShowGuide(int categoryIndex, int guideIndex = 0)
    {
        if (categoryIndex < 0 || categoryIndex >= categories.Length) return;
        if (guideIndex < 0 || guideIndex >= categories[categoryIndex].guides.Length) return;

        currentCategory = categoryIndex;
        currentIndex = guideIndex;

        GuideInfo info = categories[categoryIndex].guides[guideIndex];
        displayImage.sprite = info.image;
        displayText.text = info.description;

        pageText.text = $"{(guideIndex + 1).ToString("D2")} / {categories[categoryIndex].guides.Length.ToString("D2")}";

        prevButton.interactable = (guideIndex > 0);
        nextButton.interactable = (guideIndex < categories[categoryIndex].guides.Length - 1);
    }

    public void NextPage()
    {
        int nextIndex = currentIndex + 1;
        if (nextIndex < categories[currentCategory].guides.Length)
        {
            ShowGuide(currentCategory, nextIndex);
        }
    }

    public void PrevPage()
    {
        int prevIndex = currentIndex - 1;
        if (prevIndex >= 0)
        {
            ShowGuide(currentCategory, prevIndex);
        }
    }

    // Gọi hàm này khi bấm menu bên trái
    public void SelectCategory(int categoryIndex)
    {
        ShowGuide(categoryIndex, 0);
    }
}
