using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton that flashes a red overlay on the screen.
/// Creates its own Canvas and Image at runtime — no scene setup required.
/// </summary>
public class ScreenFlash : MonoBehaviour
{
    public static ScreenFlash Instance { get; private set; }

    [Header("Flash Settings")]
    [SerializeField] private float flashAlpha = 0.4f;
    [SerializeField] private float flashOnDuration = 0.15f;
    [SerializeField] private float flashOffDuration = 0.1f;
    [SerializeField] private int flashCount = 3;

    private Image flashImage;
    private bool isFlashing = false;

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

        CreateFlashOverlay();
    }

    private void CreateFlashOverlay()
    {
        // Create a dedicated Canvas for the flash overlay
        GameObject canvasObj = new GameObject("ScreenFlashCanvas");
        canvasObj.transform.SetParent(transform);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // On top of everything

        // Create the red flash image
        GameObject imageObj = new GameObject("FlashImage");
        imageObj.transform.SetParent(canvasObj.transform, false);

        flashImage = imageObj.AddComponent<Image>();
        flashImage.color = new Color(1f, 0f, 0f, 0f); // Transparent red
        flashImage.raycastTarget = false; // Don't block clicks

        // Stretch to fill entire screen
        RectTransform rt = flashImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Triggers the red flash sequence (3 flashes) with alarm sound.
    /// </summary>
    public void Flash()
    {
        if (isFlashing) return;
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        isFlashing = true;

        for (int i = 0; i < flashCount; i++)
        {
            // Flash on + alarm sound together
            AudioManager.PlayAlarm();
            flashImage.color = new Color(1f, 0f, 0f, flashAlpha);
            yield return new WaitForSeconds(flashOnDuration);

            // Flash off
            flashImage.color = new Color(1f, 0f, 0f, 0f);

            if (i < flashCount - 1)
                yield return new WaitForSeconds(flashOffDuration);
        }

        isFlashing = false;
    }
}
