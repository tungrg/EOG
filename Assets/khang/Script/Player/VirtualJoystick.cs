using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public Image joystickBg;
    public Image joystickHandle; 
    public Button sprintButton;

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

        // Gắn sự kiện cho nút

        sprintButton.onClick.AddListener(() => IsSprintPressed = !IsSprintPressed); // Toggle sprint
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
}