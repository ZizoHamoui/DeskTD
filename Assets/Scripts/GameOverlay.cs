using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;

/// <summary>
/// Manages the win/lose overlay screens using the same visual style as the pause menu.
/// Win shows Next Level + Main Menu. Lose shows Retry + Main Menu.
/// </summary>
public class GameOverlay : MonoBehaviour
{
    public static GameOverlay Instance { get; private set; }

    /// <summary>
    /// Set this to intercept the game over screen. If set, it is invoked instead of
    /// showing the panel, and then cleared. Used by LevelNarrativeManager for lose narratives.
    /// </summary>
    public static Action interceptGameOver;

    [Header("UI References")]
    [Tooltip("The overlay panel GameObject (same layout as pause menu)")]
    [SerializeField] private GameObject overlayPanel;

    [Tooltip("TextMeshProUGUI component for the result text")]
    [SerializeField] private TextMeshProUGUI resultText;

    [Tooltip("Next Level button — shown on win only")]
    [SerializeField] private GameObject nextLevelButton;

    [Tooltip("Retry button — shown on lose only")]
    [SerializeField] private GameObject retryButton;

    [Header("Display Settings")]
    [Tooltip("Text to display on victory")]
    [SerializeField] private string winText = "WIN";

    [Tooltip("Text to display on loss")]
    [SerializeField] private string loseText = "LOST";

    [Header("Colors")]
    [Tooltip("Text color for victory")]
    [SerializeField] private Color winColor = Color.green;

    [Tooltip("Text color for loss")]
    [SerializeField] private Color loseColor = Color.red;

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

        // Hide overlay at start
        if (overlayPanel != null)
        {
            overlayPanel.SetActive(false);
        }
    }

    public void ShowVictory()
    {
        ShowOverlay(winText, winColor, isVictory: true);
    }

    /// Shows the game over overlay, or fires the intercept if one is registered.
    public void ShowGameOver()
    {
        if (interceptGameOver != null)
        {
            Action intercept = interceptGameOver;
            interceptGameOver = null;
            intercept.Invoke();
            return;
        }

        ShowGameOverDirect();
    }

    /// Shows the game over overlay unconditionally, bypassing any intercept.
    public void ShowGameOverDirect()
    {
        ShowOverlay(loseText, loseColor, isVictory: false);
    }

    private void ShowOverlay(string text, Color color, bool isVictory)
    {
        if (overlayPanel == null)
        {
            return;
        }

        if (resultText != null)
        {
            resultText.text = text;
            resultText.color = color;
        }

        // Win: show Next Level, hide Retry
        // Lose: show Retry, hide Next Level
        if (nextLevelButton != null)
        {
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
            bool hasNextLevel = isVictory && nextIndex < SceneManager.sceneCountInBuildSettings;
            nextLevelButton.SetActive(hasNextLevel);
        }

        if (retryButton != null)
        {
            retryButton.SetActive(!isVictory);
        }

        overlayPanel.SetActive(true);

        // Pause the game
        Time.timeScale = 0f;
    }

    public void HideOverlay()
    {
        if (overlayPanel != null)
        {
            overlayPanel.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    /// Called by the Next Level button.
    public void NextLevel()
    {
        Time.timeScale = 1f;
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
    }

    /// Called by the Retry button. Reloads the current level.
    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// Called by the Main Menu button.
    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}
