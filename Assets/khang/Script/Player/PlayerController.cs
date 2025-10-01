using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Tooltip("Speed at which the character moves.")]
    public float velocity = 5f;
    [Tooltip("Additional speed while sprinting.")]
    public float sprintAdittion = 3.5f;
    [Tooltip("Gravity force.")]
    public float gravity = 9.8f;

    bool isSprinting = false;
    bool isMoving = false;
    public bool isMovementEnabled = true;

    float inputHorizontal;
    float inputVertical;
    bool inputSprint;

    CharacterController cc;
    Animator animator;
    VirtualJoystick joystick;

    // 🔹 Hướng di chuyển mượt để chống rung khi rẽ
    private Vector3 smoothMoveDir = Vector3.zero;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        joystick = FindObjectOfType<VirtualJoystick>();
        if (!joystick)
            DebugLogger.LogError("VirtualJoystick not found in scene!");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 🔹 Load vị trí cuối cùng
        LoadPlayerPosition();
    }

    void Update()
    {
        if (!isMovementEnabled)
        {
            inputHorizontal = inputVertical = 0;
            inputSprint = false;
            isMoving = isSprinting = false;
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsSprinting", false);
            return;
        }

        if (joystick != null)
        {
            inputHorizontal = joystick.InputDirection.x;
            inputVertical = joystick.InputDirection.y;
            inputSprint = joystick.IsSprintPressed;
        }

        isSprinting = inputSprint && (inputHorizontal != 0 || inputVertical != 0);
        isMoving = inputHorizontal != 0 || inputVertical != 0;

        animator.SetBool("IsMoving", isMoving);
        animator.SetBool("IsSprinting", isSprinting);
    }

    private void FixedUpdate()
    {
        if (!isMovementEnabled) return;

        float velocityAdittion = isSprinting ? sprintAdittion : 0;
        float directionX = inputHorizontal * (velocity + velocityAdittion);
        float directionZ = inputVertical * (velocity + velocityAdittion);

        // Tính hướng di chuyển dựa theo camera
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;
        forward.y = right.y = 0;   // bỏ trục Y
        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = forward * directionZ + right * directionX;

        // 🔹 Làm mượt hướng di chuyển để chống rung
        if (moveDir.sqrMagnitude > 0.001f)
        {
            smoothMoveDir = Vector3.Lerp(smoothMoveDir, moveDir, 10f * Time.deltaTime);

            Quaternion targetRotation = Quaternion.LookRotation(smoothMoveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.deltaTime);
        }

        // ✅ Gravity cơ bản
        Vector3 movement = moveDir + (Vector3.down * gravity);

        cc.Move(movement * Time.deltaTime);

        // 🔹 Chỉ lưu vị trí khi player thực sự di chuyển
        if (movement.sqrMagnitude > 0.0001f)
            SavePlayerPosition();
    }

    public void SavePlayerPosition()
    {
        PlayerPrefs.SetFloat("PlayerPosX", transform.position.x);
        PlayerPrefs.SetFloat("PlayerPosY", transform.position.y);
        PlayerPrefs.SetFloat("PlayerPosZ", transform.position.z);
        PlayerPrefs.Save();
    }

    private void LoadPlayerPosition()
    {
        if (PlayerPrefs.HasKey("PlayerPosX"))
        {
            float x = PlayerPrefs.GetFloat("PlayerPosX");
            float y = PlayerPrefs.GetFloat("PlayerPosY");
            float z = PlayerPrefs.GetFloat("PlayerPosZ");
            cc.enabled = false;
            transform.position = new Vector3(x, y, z);
            cc.enabled = true;
        }
    }
}
