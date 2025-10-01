using UnityEngine;

public class UIController : MonoBehaviour
{
    [SerializeField] private GameObject guideCanvas;

    public void OpenGuide()
    {
        Debug.Log("HelpButton clicked!");
        UILayer.Instance.ShowPanel(guideCanvas);
    }

    public void CloseGuide()
    {
        UILayer.Instance.HideAllPanels();
    }
}