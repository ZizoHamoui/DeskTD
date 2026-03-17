using UnityEngine;
using TMPro;

/// <summary>
/// Displays the current wave number out of total waves.
/// Attach to a GameObject with a TextMeshProUGUI component.
/// </summary>
public class WaveUI : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("TextMeshPro text component to display wave info")]
    [SerializeField] private TextMeshProUGUI waveText;

    [Header("Display Format")]
    [Tooltip("Format string for wave display. {0} = current wave, {1} = total waves")]
    [SerializeField] private string format = "{0}/{1}";

    private int lastDisplayedWave = -1;

    void Start()
    {
        if (waveText == null)
        {
            waveText = GetComponent<TextMeshProUGUI>();
        }

        if (waveText == null)
        {
            return;
        }

        UpdateDisplay();
    }

    void Update()
    {
        if (WaveManager.Instance == null || waveText == null) return;

        int currentWave = WaveManager.Instance.GetCurrentWaveIndex();

        // Only update text when wave changes (optimization)
        if (currentWave != lastDisplayedWave)
        {
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        if (WaveManager.Instance == null || waveText == null) return;

        // Display as 1-based (Wave 1/3 instead of Wave 0/3)
        int displayWave = WaveManager.Instance.GetCurrentWaveIndex() + 1;
        int totalWaves = WaveManager.Instance.GetTotalWaveCount();

        // Clamp display wave to total (don't show Wave 4/3 after victory)
        displayWave = Mathf.Min(displayWave, totalWaves);

        waveText.text = string.Format(format, displayWave, totalWaves);
        lastDisplayedWave = WaveManager.Instance.GetCurrentWaveIndex();
    }
}
