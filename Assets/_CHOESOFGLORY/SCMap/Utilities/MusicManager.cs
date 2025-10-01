using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Nhạc nền")]
    public AudioClip menuMusic;
    public AudioClip gameMusic;
    public AudioClip combatMusic;

    private AudioSource audioSource;

    void Awake()
    {
        // Đảm bảo chỉ có 1 MusicManager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.volume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;

        // ✅ Nhóm scene KHÔNG phát nhạc
        if (sceneName == "Loading" || sceneName == "Loading 1")
        {
            StopMusic();
            return;
        }

        // Nhóm scene Menu
        if (sceneName == "MainMenu0" || sceneName == "Login" || sceneName == "Sever")
        {
            PlayMusic(menuMusic);
        }
        // Nhóm scene Game Exploration
        else if (sceneName == "Map" || sceneName == "GlobalSetting" || sceneName == "AudioSetting" || sceneName == "AccountManagement")
        {
            PlayMusic(gameMusic);
        }
        // Nhóm scene Combat
        else if (sceneName == "BattleScene")
        {
            PlayMusic(combatMusic);
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (audioSource.clip == clip) return; // Tránh phát lại nhạc đang chạy
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void StopMusic()
    {
        audioSource.Stop();
        audioSource.clip = null;
    }

    public void PlayCombatMusic()
    {
        PlayMusic(combatMusic);
    }

    public void PlayGameMusic()
    {
        PlayMusic(gameMusic);
    }

    public void SetVolume(float volume)
    {
        audioSource.volume = volume;
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }
}
