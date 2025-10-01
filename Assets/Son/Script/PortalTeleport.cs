using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PortalTeleport : MonoBehaviour
{
    [Header("Điểm đích Teleport")]
    public Transform teleportTarget;

    [Header("Cài đặt")]
    public string playerTag = "Player";
    public float teleportCooldown = 1f;

    [Header("UI")]
    public GameObject hintUI; // Panel hiển thị thông báo
    public Text hintText;
    public Button teleportButton; // Nút [Dịch chuyển]

    private bool isTeleporting = false;
    private bool playerInside = false;
    private Transform currentPlayer;

    private void Start()
    {
        if (hintUI != null) hintUI.SetActive(false);
        if (teleportButton != null)
        {
            teleportButton.gameObject.SetActive(false);
            teleportButton.onClick.AddListener(OnTeleportButtonClick);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInside = true;
            currentPlayer = other.transform;
            UpdateHint();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInside = false;
            currentPlayer = null;
            if (hintUI != null) hintUI.SetActive(false);
            if (teleportButton != null) teleportButton.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!playerInside || currentPlayer == null) return;

        // Luôn cập nhật hint khi đứng trong cổng
        UpdateHint();
    }

    private void UpdateHint()
    {
        if (hintUI == null || hintText == null || teleportButton == null) return;

        // Không cần check Lv15 hay Booyah Pass nữa
        hintText.text = "Có thể dịch chuyển";
        teleportButton.gameObject.SetActive(true);

        hintUI.SetActive(true);
    }

    private void OnTeleportButtonClick()
    {
        if (playerInside && currentPlayer != null && !isTeleporting)
        {
            StartCoroutine(TeleportPlayer(currentPlayer));
        }
    }

    private IEnumerator TeleportPlayer(Transform player)
    {
        isTeleporting = true;

        // Ẩn UI ngay khi bắt đầu teleport
        if (hintUI != null) hintUI.SetActive(false);
        if (teleportButton != null) teleportButton.gameObject.SetActive(false);

        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.position = teleportTarget.position;

        if (cc != null) cc.enabled = true;

        // Reset flag để cổng đích nhận diện lại
        playerInside = false;
        currentPlayer = null;

        yield return new WaitForSeconds(teleportCooldown);
        isTeleporting = false;
    }
}
