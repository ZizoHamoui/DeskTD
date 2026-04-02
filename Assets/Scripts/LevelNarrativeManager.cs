using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Configurable per-level narrative beats. Attach to any level scene and fill in
/// the messages you want shown at the start, between waves, and at the end.
/// Leave a message blank to skip that beat.
/// </summary>
public class LevelNarrativeManager : MonoBehaviour
{
    [System.Serializable]
    public class WaveStartNarrative
    {
        [Tooltip("Wave index (0-based) that triggers this message when it starts")]
        public int waveIndex;
        [TextArea] public string message;
    }

    [System.Serializable]
    public class WaveEndNarrative
    {
        [Tooltip("Wave index (0-based) that triggers this message when it completes")]
        public int waveIndex;
        [TextArea] public string message;
    }

    [Header("Level Start")]
    [Tooltip("Shown at the very start of the level, before wave 1 begins")]
    [TextArea] [SerializeField] private string levelStartMessage;

    [Tooltip("Shown immediately after the level start message is dismissed")]
    [TextArea] [SerializeField] private string levelStartFollowUpMessage;

    [Header("Wave Start Narratives")]
    [SerializeField] private WaveStartNarrative[] waveStartMessages;

    [Header("Wave End Narratives")]
    [SerializeField] private WaveEndNarrative[] waveEndMessages;

    [Header("Lose Narrative")]
    [Tooltip("Shown instead of the game over screen when the scroll is destroyed")]
    [TextArea] [SerializeField] private string loseNarrativeMessage;

    [Header("End Scene")]
    [Tooltip("Scene name to load after the final win narrative. Leave blank to use default victory screen.")]
    [SerializeField] private string endSceneName;

    [Tooltip("Scene name to load after the lose narrative. Leave blank to use default game over screen.")]
    [SerializeField] private string loseEndSceneName;

    private bool waitingForDismissToStartGame = false;

    void Start()
    {
        if (WaveManager.Instance == null) return;

        WaveManager.Instance.onWaveStarting += OnWaveStarting;
        WaveManager.Instance.onWaveComplete += OnWaveComplete;

        // If there's an end-of-last-wave narrative, request deferred victory
        if (waveEndMessages != null)
        {
            int lastWave = WaveManager.Instance.GetTotalWaveCount() - 1;
            foreach (var entry in waveEndMessages)
            {
                if (entry.waveIndex == lastWave && !string.IsNullOrEmpty(entry.message))
                {
                    WaveManager.Instance.RequestDeferVictory();
                    break;
                }
            }
        }

        // Register lose narrative intercept
        if (!string.IsNullOrEmpty(loseNarrativeMessage))
        {
            GameOverlay.interceptGameOver = ShowLoseNarrative;
        }

        // Show level start message after a short delay before the first wave
        if (!string.IsNullOrEmpty(levelStartMessage))
        {
            waitingForDismissToStartGame = true;
            WaveManager.Instance.PauseAutoStart();
            StartCoroutine(ShowLevelStartAfterDelay(2f));
        }
    }

    void OnDestroy()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.onWaveStarting -= OnWaveStarting;
            WaveManager.Instance.onWaveComplete -= OnWaveComplete;
        }

        // Clear intercept so it doesn't linger across scene reloads
        GameOverlay.interceptGameOver = null;
    }

    private System.Collections.IEnumerator ShowLevelStartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        NarrativeEventUI.Show(levelStartMessage, () =>
        {
            if (!string.IsNullOrEmpty(levelStartFollowUpMessage))
            {
                NarrativeEventUI.Show(levelStartFollowUpMessage, () =>
                {
                    waitingForDismissToStartGame = false;
                    if (WaveManager.Instance != null)
                        WaveManager.Instance.ResumeAutoStart();
                });
            }
            else
            {
                waitingForDismissToStartGame = false;
                if (WaveManager.Instance != null)
                    WaveManager.Instance.ResumeAutoStart();
            }
        });
    }

    private void OnWaveStarting(int waveIndex)
    {
        if (waveStartMessages == null) return;

        foreach (var entry in waveStartMessages)
        {
            if (entry.waveIndex == waveIndex && !string.IsNullOrEmpty(entry.message))
            {
                NarrativeEventUI.Show(entry.message);
                break;
            }
        }
    }

    private void OnWaveComplete(int waveIndex)
    {
        if (waveEndMessages == null) return;

        foreach (var entry in waveEndMessages)
        {
            if (entry.waveIndex == waveIndex && !string.IsNullOrEmpty(entry.message))
            {
                if (WaveManager.Instance != null &&
                    waveIndex == WaveManager.Instance.GetTotalWaveCount() - 1)
                {
                    // Final wave — route to end scene or default victory
                    NarrativeEventUI.Show(entry.message, () =>
                    {
                        if (!string.IsNullOrEmpty(endSceneName))
                            SceneManager.LoadScene(endSceneName);
                        else if (WaveManager.Instance != null)
                            WaveManager.Instance.CompleteDeferredVictory();
                    });
                }
                else
                {
                    NarrativeEventUI.Show(entry.message);
                }
                break;
            }
        }
    }

    private void ShowLoseNarrative()
    {
        NarrativeEventUI.Show(loseNarrativeMessage, () =>
        {
            if (!string.IsNullOrEmpty(loseEndSceneName))
                SceneManager.LoadScene(loseEndSceneName);
            else
                GameOverlay.Instance?.ShowGameOverDirect();
        });
    }
}
