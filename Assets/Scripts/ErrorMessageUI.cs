using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Displays temporary error messages to the player.
/// Singleton pattern for easy access from anywhere.
/// </summary>
public class ErrorMessageUI : MonoBehaviour
{
    public static ErrorMessageUI Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("TextMeshProUGUI component to display error messages")]
    [SerializeField] private TextMeshProUGUI errorText;

    [Header("Display Settings")]
    [Tooltip("How long the message stays visible (seconds)")]
    [SerializeField] private float displayDuration = 2f;

    [Tooltip("Color for error messages")]
    [SerializeField] private Color errorColor = Color.red;

    private Coroutine hideCoroutine;

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
    }

    void Start()
    {
        if (errorText == null)
        {
            enabled = false;
            return;
        }

        // Start hidden
        errorText.text = "";
        errorText.color = errorColor;
    }

    public void ShowError(string message)
    {
        if (errorText == null) return;

        // Cancel any pending hide
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        errorText.text = message;
        hideCoroutine = StartCoroutine(HideAfterDelay());

    }

    public static void Show(string message)
    {
        if (Instance != null)
        {
            Instance.ShowError(message);
        }
        else
        {
        }
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        errorText.text = "";
        hideCoroutine = null;
    }

    /// <summary>
    /// Immediately hides the error message.
    /// </summary>
    public void HideError()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (errorText != null)
        {
            errorText.text = "";
        }
    }
}
