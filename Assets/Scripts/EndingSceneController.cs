using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the ending scene flow: story epilogue panels (click-to-advance with typewriter)
/// → fade to black → scrolling credits → Main Menu button.
/// Used by both EndingWin and EndingLose scenes with different Inspector configuration.
/// </summary>
public class EndingSceneController : MonoBehaviour
{
    [Header("Story Panels")]
    [Tooltip("Static background image displayed during the story")]
    [SerializeField] private Image backgroundImage;

    [Tooltip("TextMeshPro text for the typewriter story display")]
    [SerializeField] private TextMeshProUGUI storyText;

    [Tooltip("Story text for each panel — click advances to next")]
    [TextArea] [SerializeField] private string[] storyPanelTexts;

    [Tooltip("Seconds per character for the typewriter effect")]
    [SerializeField] private float timeBetweenChars = 0.03f;

    [Tooltip("'Click to continue' prompt shown after typing finishes")]
    [SerializeField] private GameObject clickPrompt;

    [Tooltip("Scroll image displayed behind story text — hidden when credits begin")]
    [SerializeField] private GameObject textScrollImage;

    [Header("Fade")]
    [Tooltip("Full-screen black panel with CanvasGroup for fade effect")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Tooltip("Duration of the fade-to-black transition in seconds")]
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Credits")]
    [Tooltip("RectTransform containing all credit entries (VerticalLayoutGroup)")]
    [SerializeField] private RectTransform creditsContainer;

    [Tooltip("Scroll speed in pixels per second")]
    [SerializeField] private float creditsScrollSpeed = 60f;

    [Header("Main Menu")]
    [Tooltip("Button shown after credits finish scrolling")]
    [SerializeField] private GameObject mainMenuButton;

    [Header("Audio")]
    [Tooltip("Music clip to play during the ending (assign later)")]
    [SerializeField] private AudioClip endingMusic;

    [Tooltip("Volume for ending music")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.5f;

    private AudioSource audioSource;
    private bool clicked;
    private bool isTyping;

    void Start()
    {
        Time.timeScale = 1f;

        // Stop any persisting gameplay or menu music
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopBackgroundMusic();
        if (MenuMusic.Instance != null)
            MenuMusic.Instance.gameObject.SetActive(false);

        // Set up local audio source and start ending music immediately
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        if (endingMusic != null)
        {
            audioSource.clip = endingMusic;
            audioSource.volume = musicVolume;
            audioSource.Play();
        }

        // Hide elements that appear later
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
        if (creditsContainer != null)
            creditsContainer.gameObject.SetActive(false);
        if (mainMenuButton != null)
            mainMenuButton.SetActive(false);
        if (clickPrompt != null)
            clickPrompt.SetActive(false);

        StartCoroutine(RunEnding());
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            clicked = true;
    }

    private IEnumerator RunEnding()
    {
        // --- Story Phase ---
        yield return StartCoroutine(StoryPhase());

        // --- Fade to Black ---
        yield return StartCoroutine(FadeToBlack());

        // Hide story elements
        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(false);
        if (storyText != null)
            storyText.gameObject.SetActive(false);
        if (textScrollImage != null)
            textScrollImage.SetActive(false);

        // --- Credits Phase ---
        yield return StartCoroutine(CreditsPhase());

        // --- Show Main Menu Button ---
        if (mainMenuButton != null)
            mainMenuButton.SetActive(true);
    }

    // ─── Story Phase ───────────────────────────────────────────

    private IEnumerator StoryPhase()
    {
        if (storyPanelTexts == null || storyPanelTexts.Length == 0)
            yield break;

        for (int i = 0; i < storyPanelTexts.Length; i++)
        {
            // Set up text and run typewriter
            storyText.text = storyPanelTexts[i];
            yield return StartCoroutine(TypewriterEffect());

            // Show click prompt and wait for click
            if (clickPrompt != null)
                clickPrompt.SetActive(true);

            clicked = false;
            yield return new WaitUntil(() => clicked);

            if (clickPrompt != null)
                clickPrompt.SetActive(false);
        }
    }

    /// <summary>
    /// Reveals text character-by-character using maxVisibleCharacters.
    /// Clicking during typing skips to full reveal.
    /// Same pattern as IntroAnimation.TextVisible().
    /// </summary>
    private IEnumerator TypewriterEffect()
    {
        isTyping = true;
        clicked = false;

        storyText.ForceMeshUpdate();
        int totalChars = storyText.textInfo.characterCount;
        int counter = 0;

        storyText.maxVisibleCharacters = 0;

        while (counter < totalChars)
        {
            // Click during typing → reveal all immediately
            if (clicked)
            {
                storyText.maxVisibleCharacters = totalChars;
                clicked = false;
                break;
            }

            counter++;
            storyText.maxVisibleCharacters = counter;
            yield return new WaitForSeconds(timeBetweenChars);
        }

        storyText.maxVisibleCharacters = totalChars;
        isTyping = false;
    }

    // ─── Fade Phase ────────────────────────────────────────────

    private IEnumerator FadeToBlack()
    {
        if (fadeCanvasGroup == null)
            yield break;

        fadeCanvasGroup.blocksRaycasts = true;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
        fadeCanvasGroup.blocksRaycasts = false;
    }

    // ─── Credits Phase ─────────────────────────────────────────

    private IEnumerator CreditsPhase()
    {
        if (creditsContainer == null)
            yield break;

        creditsContainer.gameObject.SetActive(true);

        // Force layout rebuild so we get accurate height
        LayoutRebuilder.ForceRebuildLayoutImmediate(creditsContainer);
        yield return null; // wait one frame for layout

        // Calculate scroll distance:
        // Credits start below screen. We need to scroll until the last entry
        // is visible near the top of the screen.
        Canvas canvas = creditsContainer.GetComponentInParent<Canvas>();
        float screenHeight = 0f;
        if (canvas != null)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            screenHeight = canvasRect.rect.height;
        }

        float contentHeight = creditsContainer.rect.height;
        float totalScrollDistance = contentHeight + screenHeight;

        // Position credits below the visible area
        Vector2 startPos = creditsContainer.anchoredPosition;
        startPos.y = -screenHeight;
        creditsContainer.anchoredPosition = startPos;

        float scrolled = 0f;

        while (scrolled < totalScrollDistance)
        {
            float delta = creditsScrollSpeed * Time.deltaTime;
            scrolled += delta;

            Vector2 pos = creditsContainer.anchoredPosition;
            pos.y += delta;
            creditsContainer.anchoredPosition = pos;

            yield return null;
        }
    }

    // ─── Button Callbacks ──────────────────────────────────────

    public void OnMainMenuClicked()
    {
        SceneManager.LoadScene(0);
    }
}
