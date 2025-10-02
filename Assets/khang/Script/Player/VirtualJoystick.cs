using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public Image joystickBg;
    public Image joystickHandle; 
    public Button sprintButton;

    [Header("Sprint Button Colors")]
    public Color normalColor = Color.white;
    public Color sprintActiveColor = Color.green;

    private Vector2 inputVector;
    private Vector2 joystickStartPos;
    private float joystickRadius;
    public bool IsJumpPressed { get; private set; }
    public bool IsSprintPressed { get; private set; }
    public Vector2 InputDirection => inputVector;

    void Start()
    {
        joystickRadius = joystickBg.GetComponent<RectTransform>().sizeDelta.x * 0.5f;
        joystickStartPos = joystickHandle.GetComponent<RectTransform>().anchoredPosition;

        // Set initial button color
        if (sprintButton != null)
        {
            UpdateSprintButtonColor();
            
            // Gắn sự kiện cho nút
            sprintButton.onClick.AddListener(() => {
                IsSprintPressed = !IsSprintPressed;
                UpdateSprintButtonColor();
            });
        }
    }

    void Update()
    {
        // Reset jump sau 1 frame để mô phỏng nhấn phím
       
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 touchPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBg.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out touchPos))
        {
            touchPos.x /= joystickRadius;
            touchPos.y /= joystickRadius;
            inputVector = new Vector2(touchPos.x, touchPos.y);
            inputVector = (inputVector.magnitude > 1f) ? inputVector.normalized : inputVector;

            joystickHandle.GetComponent<RectTransform>().anchoredPosition =
                joystickStartPos + inputVector * joystickRadius;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        joystickHandle.GetComponent<RectTransform>().anchoredPosition = joystickStartPos;
    }

    private void UpdateSprintButtonColor()
    {
        if (sprintButton != null)
        {
            // Find all Image components in children
            Image[] images = sprintButton.GetComponentsInChildren<Image>();
            
            // Look for the child Image (not the button's own Image if it has one)
            Image childImage = null;
            foreach (Image img in images)
            {
                if (img.transform != sprintButton.transform)
                {
                    childImage = img;
                    break;
                }
            }
            
            if (childImage != null)
            {
                childImage.color = IsSprintPressed ? sprintActiveColor : normalColor;
            }
            else
            {
                // If no child Image found, use the first Image found (could be the button itself)
                if (images.Length > 0)
                {
                    images[0].color = IsSprintPressed ? sprintActiveColor : normalColor;
                }
                else
                {
                    DebugLogger.LogError("Sprint button doesn't have any Image component!");
                }
            }
        }
    }
}