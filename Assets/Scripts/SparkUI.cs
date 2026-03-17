using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the current spark count in the UI.
/// Updates automatically when spark count changes.
/// </summary>
public class SparkUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text component to display spark count")]
    public Text sparkText;

    [Header("Display Settings")]
    [Tooltip("Format string for display. Use {0} for spark count.")]
    public string displayFormat = "Sparks: {0}";

    [Tooltip("How often to update the display (in seconds). Lower = more responsive but more performance cost.")]
    public float updateInterval = 0.1f;

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
        }
    }

    public void ForceUpdate()
    {
        UpdateDisplay();
    }
}
