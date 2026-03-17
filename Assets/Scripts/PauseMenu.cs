using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the pause menu UI. Freezes the game via Time.timeScale
/// and provides Resume and Quit functionality.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The full-screen pause overlay panel")]
    [SerializeField] private GameObject pausePanel;

    private bool isPaused;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        isPaused = false;
    }

    /// <summary>
    /// Called by the pause button. Only pauses if the game isn't already frozen
    /// (e.g. by the win/lose overlay).
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    private void Pause()
    {
        // Don't pause if the game is already stopped (victory/game over)
        if (Time.timeScale == 0f) return;

        isPaused = true;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void Resume()
    {
        isPaused = false;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    public void Quit()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}
