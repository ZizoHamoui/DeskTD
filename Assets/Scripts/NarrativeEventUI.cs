using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows narrative text popups during gameplay. Freezes the game until dismissed.
/// </summary>
public class NarrativeEventUI : MonoBehaviour
{
    public static NarrativeEventUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject narrativePanel;
    [SerializeField] private TextMeshProUGUI narrativeText;
    [SerializeField] private Button continueButton;

    private System.Action onDismissCallback;

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

        if (narrativePanel != null)
            narrativePanel.SetActive(false);
    }

    void Start()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(Dismiss);
    }

    public static void Show(string message)
    {
        if (Instance != null)
            Instance.ShowNarrative(message);
    }

    public static void Show(string message, System.Action onDismiss)
    {
        if (Instance != null)
            Instance.ShowNarrative(message, onDismiss);
    }

    private void ShowNarrative(string message, System.Action onDismiss = null)
    {
        if (Time.timeScale == 0f) return;
        if (narrativePanel == null) return;

        onDismissCallback = onDismiss;

        if (narrativeText != null)
            narrativeText.text = message;

        narrativePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void Dismiss()
    {
        if (narrativePanel == null) return;

        narrativePanel.SetActive(false);
        Time.timeScale = 1f;

        // Invoke callback after restoring timeScale so chained Show() calls work
        var callback = onDismissCallback;
        onDismissCallback = null;
        callback?.Invoke();
    }
}
