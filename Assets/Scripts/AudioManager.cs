using UnityEngine;

/// <summary>
/// Singleton that manages all audio playback in the game.
/// Handles background music and sound effects.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music Clips")]
    [Tooltip("Background music that loops continuously")]
    [SerializeField] private AudioClip backgroundMusic;

    [Header("Sound Effect Clips")]
    [Tooltip("Plays when a wave starts")]
    [SerializeField] private AudioClip roarClip;

    [Tooltip("Plays when a tower is successfully placed")]
    [SerializeField] private AudioClip plopClip;

    [Tooltip("Plays when eraser successfully cleans an ink tile")]
    [SerializeField] private AudioClip eraserClip;

    [Tooltip("Plays when tower placement fails")]
    [SerializeField] private AudioClip errorClip;

    [Tooltip("Plays when an enemy attacks a tower")]
    [SerializeField] private AudioClip enemyAttackClip;

    [Tooltip("Plays when a projectile hits an enemy")]
    [SerializeField] private AudioClip shotHitClip;

    [Tooltip("Plays when an enemy reaches the second-last tile")]
    [SerializeField] private AudioClip alarmClip;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.5f;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1.0f;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Create AudioSource components if not assigned
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }

    void Start()
    {
        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;

        PlayBackgroundMusic();
    }

    public void PlayBackgroundMusic()
    {
        if (backgroundMusic != null && musicSource != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    public static void PlayRoar()
    {
        if (Instance != null && Instance.roarClip != null)
        {
            Instance.sfxSource.PlayOneShot(Instance.roarClip, Instance.sfxVolume);
        }
    }

    public static void PlayPlop()
    {
        if (Instance != null && Instance.plopClip != null)
        {
            Instance.sfxSource.PlayOneShot(Instance.plopClip, Instance.sfxVolume);
        }
    }

    public static void PlayEraser()
    {
        if (Instance != null && Instance.eraserClip != null)
        {
            Instance.sfxSource.PlayOneShot(Instance.eraserClip, Instance.sfxVolume);
        }
    }

    public static void PlayError()
    {
        if (Instance != null && Instance.errorClip != null)
        {
            Instance.sfxSource.PlayOneShot(Instance.errorClip, Instance.sfxVolume);
        }
    }

    public static void PlayEnemyAttack()
    {
        if (Instance != null && Instance.enemyAttackClip != null)
        {
            Instance.sfxSource.PlayOneShot(Instance.enemyAttackClip, Instance.sfxVolume * 0.3f);
        }
    }
    public static void PlayShotHit()
    {
        if (Instance != null && Instance.shotHitClip != null)
        {
            Instance.sfxSource.PlayOneShot(Instance.shotHitClip, Instance.sfxVolume);
        }
    }

    public static void PlayAlarm()
    {
        if (Instance != null && Instance.alarmClip != null)
        {
            Instance.sfxSource.PlayOneShot(Instance.alarmClip, Instance.sfxVolume);
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    public void StopBackgroundMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    public void PauseBackgroundMusic()
    {
        if (musicSource != null)
        {
            musicSource.Pause();
        }
    }

    public void ResumeBackgroundMusic()
    {
        if (musicSource != null)
        {
            musicSource.UnPause();
        }
    }
}
