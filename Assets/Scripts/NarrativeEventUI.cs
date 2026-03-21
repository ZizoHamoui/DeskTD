using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Shows narrative text popups during gameplay. Freezes the game until dismissed.
/// If a narrative is already showing, new ones are queued and shown in order.
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
    private bool isShowing = false;

    private struct QueuedNarrative
    {
        public string message;
        public Sprite avatar;
        public System.Action onDismiss;
    }

    private readonly Queue<QueuedNarrative> narrativeQueue = new Queue<QueuedNarrative>();

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
        if (narrativePanel == null) return;

        // If a narrative is already showing, queue this one for later
        if (isShowing)
        {
            narrativeQueue.Enqueue(new QueuedNarrative
            {
                message = message,
                avatar = avatar,
                onDismiss = onDismiss
            });
            return;
        }

        DisplayNarrative(message, avatar, onDismiss);
    }

    private void DisplayNarrative(string message, Sprite avatar, System.Action onDismiss)
    {
        isShowing = true;
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
        isShowing = false;

        // Restore timeScale before callback so chained Show() calls work
        Time.timeScale = 1f;

        // Invoke callback
        var callback = onDismissCallback;
        onDismissCallback = null;
        callback?.Invoke();

        // Show next queued narrative if any
        if (narrativeQueue.Count > 0)
        {
            var next = narrativeQueue.Dequeue();
            DisplayNarrative(next.message, next.avatar, next.onDismiss);
        }
    }
}
