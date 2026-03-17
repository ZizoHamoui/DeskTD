using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonClickSFX : MonoBehaviour
{
    public static ButtonClickSFX Instance { get; private set; }

    [SerializeField] private AudioClip clickClip;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;

    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.ignoreListenerPause = true;
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
        foreach (Button btn in FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            btn.onClick.AddListener(PlayClick);
        }
    }

    public void PlayClick()
    {
        if (clickClip != null)
            audioSource.PlayOneShot(clickClip, volume);
    }
}
