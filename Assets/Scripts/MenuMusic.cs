using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuMusic : MonoBehaviour
{
    public static MenuMusic Instance { get; private set; }

    [SerializeField] private AudioClip menuMusicClip;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.5f;

    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = false;
        }
        else
        {
            Destroy(gameObject);
            return;
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex >= 4) // Level scenes start at index 4
        {
            audioSource.Stop();
        }
        else
        {
            if (!audioSource.isPlaying && menuMusicClip != null)
            {
                audioSource.clip = menuMusicClip;
                audioSource.volume = volume;
                audioSource.Play();
            }
        }
    }
}
