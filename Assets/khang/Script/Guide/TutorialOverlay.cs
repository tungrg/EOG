using UnityEngine;
using UnityEngine.UI;

public class TutorialOverlay : MonoBehaviour
{
    [SerializeField] private GameObject overlayPanel;
    [SerializeField] private Image highlightImage;

    public void ShowOverlay(RectTransform target)
    {
        overlayPanel.SetActive(true);

        RectTransform highlightRect = highlightImage.GetComponent<RectTransform>();

        // Convert world position sang local theo canvas
        Vector3 worldPos = target.position;
        Vector3 localPos = highlightRect.parent.InverseTransformPoint(worldPos);

        highlightRect.localPosition = localPos;
        highlightRect.sizeDelta = target.sizeDelta * 1.3f;

        // Đảm bảo highlight nằm trên overlay
        highlightRect.SetAsLastSibling();
    }


    public void HideOverlay()
    {
        overlayPanel.SetActive(false);
    }
}
