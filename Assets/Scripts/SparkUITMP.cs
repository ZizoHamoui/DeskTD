using UnityEngine;
using TMPro;

public class SparkUITMP : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("TextMeshProUGUI component to display spark count")]
    public TextMeshProUGUI sparkText;

    [Header("Display Settings")]
    [Tooltip("Format string for display. Use {0} for spark count.")]
    public string displayFormat = "Sparks: {0}";

    [Tooltip("How often to update the display (in seconds). Lower = more responsive but more performance cost.")]
    public float updateInterval = 0.1f;

    [Header("Visual Feedback (Optional)")]
    [Tooltip("Color when player has enough sparks")]
    public Color normalColor = Color.white;

    [Tooltip("Color when player has low sparks (below this threshold)")]
    public Color lowSparkColor = Color.red;

    [Tooltip("Spark threshold for low spark warning")]
    public int lowSparkThreshold = 5;

    [Tooltip("Enable color change based on spark amount")]
    public bool enableColorFeedback = false;

    private float updateTimer = 0f;
    private int lastKnownSparkCount = -1;

    void Start()
    {
        if (sparkText == null)
        {
            enabled = false;
            return;
        }

        // Initial update
        UpdateDisplay();
    }

    void Update()
    {
        // Update at intervals to avoid unnecessary updates every frame
        updateTimer += Time.deltaTime;

        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        if (SparkManager.Instance == null)
        {
            sparkText.text = "Sparks: --";
            return;
        }

        int currentSparks = SparkManager.Instance.GetCurrentSparks();

        // Only update text if value changed (optimization)
        if (currentSparks != lastKnownSparkCount)
        {
            sparkText.text = string.Format(displayFormat, currentSparks);
            lastKnownSparkCount = currentSparks;

            // Optional: Color feedback
            if (enableColorFeedback)
            {
                sparkText.color = currentSparks <= lowSparkThreshold ? lowSparkColor : normalColor;
            }
        }
    }

    public void ForceUpdate()
    {
        UpdateDisplay();
    }
}
