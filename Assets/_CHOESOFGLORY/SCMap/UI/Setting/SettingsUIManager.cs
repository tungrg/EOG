using UnityEngine;

public class SettingsUIManager : MonoBehaviour
{
    [Header("Panel Âm thanh")]
    public GameObject AudioSettingPanel;

    [Header("Camera Controller")]
    public CameraController cameraController; // Kéo script CameraController vào đây trong Inspector

    private void Start()
    {
        // Ban đầu ẩn panel
        AudioSettingPanel.SetActive(false);
    }

    // Mở panel âm thanh
    public void OpenAudioSetting()
    {
        AudioSettingPanel.SetActive(true);

        // Khóa camera
        if (cameraController != null)
            cameraController.isCameraControlEnabled = false;
    }

    // Đóng panel âm thanh -> quay về gameplay
    public void CloseAudioSetting()
    {
        AudioSettingPanel.SetActive(false);

        // Mở lại camera
        if (cameraController != null)
            cameraController.isCameraControlEnabled = true;
    }
}
