using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;

public class MapManager : MonoBehaviour
{
    [SerializeField] private TeamData teamData;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject teamSetupPanel;
    [SerializeField] private GameObject enemyInfoPanel;
    [SerializeField] private TMPro.TextMeshProUGUI enemyInfoText;

    [SerializeField] private List<Transform> waveReturnPositions = new List<Transform>();

    [SerializeField] private GameObject waveCongratsPanel;
    [SerializeField] private TextMeshProUGUI waveCongratsText;
    [SerializeField] private Button waveCongratsOkButton;

    [SerializeField] private int totalWaves = 3;

    private List<EnemyTrigger> enemies = new List<EnemyTrigger>();

    // Cho phép EnemyTrigger truy cập panel thông tin
    public GameObject EnemyInfoPanel => enemyInfoPanel;

    // 🔹 Thêm mới từ bản 2
    public Transform PlayerTransform => playerTransform;

    void Start()
    {
        if (teamData == null || playerTransform == null || teamSetupPanel == null || enemyInfoPanel == null || enemyInfoText == null)
        {
            DebugLogger.LogError("MapManager components not assigned.");
            return;
        }

        // ✅ Gọi xử lý wave trước
        bool justClearedWave = HandleWaveCompleteReturn();

        // ✅ Nếu vừa hoàn thành wave thì KHÔNG khôi phục PreCombatPos
        if (!justClearedWave && PlayerPrefs.HasKey("PreCombatPosX"))
        {
            float x = PlayerPrefs.GetFloat("PreCombatPosX");
            float y = PlayerPrefs.GetFloat("PreCombatPosY");
            float z = PlayerPrefs.GetFloat("PreCombatPosZ");
            playerTransform.position = new Vector3(x, y, z);

            PlayerPrefs.DeleteKey("PreCombatPosX");
            PlayerPrefs.DeleteKey("PreCombatPosY");
            PlayerPrefs.DeleteKey("PreCombatPosZ");
            PlayerPrefs.Save();
        }

        teamSetupPanel.SetActive(false);
        enemyInfoPanel.SetActive(false);
        enemies.AddRange(FindObjectsByType<EnemyTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None));
    }

    public void StartBattle(List<EnemyData> selectedEnemies)
    {
        PlayerPrefs.SetFloat("PreCombatPosX", playerTransform.position.x);
        PlayerPrefs.SetFloat("PreCombatPosY", playerTransform.position.y);
        PlayerPrefs.SetFloat("PreCombatPosZ", playerTransform.position.z);
        PlayerPrefs.Save();

        teamData.SelectedEnemies = selectedEnemies;
        teamData.CurrentBattleType = BattleType.Map;

        if (teamData.SelectedCombatants.Count == 0)
            teamSetupPanel.SetActive(true);
        else
            SceneManager.LoadScene("BattleScene");
    }

    public void ConfirmTeamAndStartBattle()
    {
        if (teamData.SelectedCombatants.Count > 0)
        {
            teamSetupPanel.SetActive(false);
            SceneManager.LoadScene("BattleScene");
        }
    }

    private bool HandleWaveCompleteReturn()
    {
        if (PlayerPrefs.GetInt("WaveCompleted", 0) != 1) return false;

        // Lấy số wave vừa hoàn tất để hiển thị (1-based)
        int completedWaveDisplay = PlayerPrefs.GetInt("LastCompletedWaveDisplay", 1);

        // ✅ Dịch chuyển đến điểm chỉ định cho wave vừa xong
        if (completedWaveDisplay >= 1
            && waveReturnPositions != null
            && waveReturnPositions.Count >= completedWaveDisplay
            && waveReturnPositions[completedWaveDisplay - 1] != null)
        {
            playerTransform.position = waveReturnPositions[completedWaveDisplay - 1].position;
        }

        // ✅ Xoá PreCombatPos để không override vị trí mới
        PlayerPrefs.DeleteKey("PreCombatPosX");
        PlayerPrefs.DeleteKey("PreCombatPosY");
        PlayerPrefs.DeleteKey("PreCombatPosZ");

        // ✅ Xoá luôn PlayerPos để PlayerController không load lại vị trí cũ
        PlayerPrefs.DeleteKey("PlayerPosX");
        PlayerPrefs.DeleteKey("PlayerPosY");
        PlayerPrefs.DeleteKey("PlayerPosZ");

        PlayerPrefs.Save();

        // ✅ Hiện panel chúc mừng
        if (waveCongratsPanel != null)
        {
            waveCongratsPanel.SetActive(true);
            if (waveCongratsText != null)
            {
                bool hasNext = completedWaveDisplay < (totalWaves > 0 ? totalWaves : waveReturnPositions.Count);
                waveCongratsText.text = hasNext
                    ? $"Chúc mừng bạn đã vượt qua Wave {completedWaveDisplay}! Wave {completedWaveDisplay + 1} sẽ có những quái mạnh hơn — sẵn sàng nhé!"
                    : $"Chúc mừng bạn đã hoàn thành tất cả các Wave!";
            }

            // Khoá điều khiển cho tới khi bấm OK
            var pc = playerTransform != null ? playerTransform.GetComponent<PlayerController>() : null;
            if (pc != null) pc.enabled = false;
            var cam = FindFirstObjectByType<CameraController>();
            if (cam != null) cam.isCameraControlEnabled = false;

            if (waveCongratsOkButton != null)
            {
                waveCongratsOkButton.onClick.RemoveAllListeners();
                waveCongratsOkButton.onClick.AddListener(() =>
                {
                    waveCongratsPanel.SetActive(false);
                    // Mở lại điều khiển
                    var pc2 = playerTransform != null ? playerTransform.GetComponent<PlayerController>() : null;
                    if (pc2 != null) pc2.enabled = true;
                    var cam2 = FindFirstObjectByType<CameraController>();
                    if (cam2 != null) cam2.isCameraControlEnabled = true;
                });
            }
        }

        // Reset cờ để panel chỉ xuất hiện 1 lần sau khi vừa clear wave
        PlayerPrefs.SetInt("WaveCompleted", 0);
        PlayerPrefs.Save();
        return true;
    }

  

    public void ShowTeamSetupPanel()
    {
        teamSetupPanel.SetActive(true);
    }

    public void ShowEnemyInfo(EnemyData enemyData)
    {
        if (enemyInfoPanel != null && enemyInfoText != null && enemyData != null)
        {
            enemyInfoPanel.SetActive(true);
            enemyInfoText.text = $"{enemyData.Name}\n{enemyData.HP}/{enemyData.MaxHP}";
        }
        else
        {
            DebugLogger.LogError("EnemyInfoPanel, EnemyInfoText, or enemyData is null in ShowEnemyInfo.");
        }
    }

    public void HideEnemyInfo()
    {
        if (enemyInfoPanel != null)
        {
            enemyInfoPanel.SetActive(false);
        }
    }
}
