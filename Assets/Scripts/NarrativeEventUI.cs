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

    [Header("Avatar")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private Sprite defaultAvatar;

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
            Instance.ShowNarrative(message, onDismiss: onDismiss);
    }

    public static void Show(string message, Sprite avatar)
    {
        if (Instance != null)
            Instance.ShowNarrative(message, avatar: avatar);
    }

    public static void Show(string message, Sprite avatar, System.Action onDismiss)
    {
        if (Instance != null)
            Instance.ShowNarrative(message, avatar: avatar, onDismiss: onDismiss);
    }

    private void ShowNarrative(string message, Sprite avatar = null, System.Action onDismiss = null)
    {
        if (Time.timeScale == 0f) return;
        if (narrativePanel == null) return;

        onDismissCallback = onDismiss;

        if (narrativeText != null)
            narrativeText.text = message;

        // Show avatar — use provided sprite, fall back to default, hide if neither exists
        if (avatarImage != null)
        {
            Sprite sprite = avatar != null ? avatar : defaultAvatar;
            if (sprite != null)
            {
                avatarImage.sprite = sprite;
                avatarImage.gameObject.SetActive(true);
            }
            else
            {
                avatarImage.gameObject.SetActive(false);
            }
        }

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
