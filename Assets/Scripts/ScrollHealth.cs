using UnityEngine;

/// <summary>
/// Manages the scroll health visual on the left side of the screen.
/// Shows progressive damage as enemies get through defenses.
/// 3 hits = Game Over.
/// </summary>
public class ScrollHealth : MonoBehaviour
{
    public static ScrollHealth Instance { get; private set; }

    [Header("Scroll Sprites (in order: clean → damaged → destroyed)")]
    [Tooltip("Main clean scroll - no damage")]
    [SerializeField] private Sprite scrollClean;

    [Tooltip("Scroll with 1 ink smudge")]
    [SerializeField] private Sprite scrollDamage1;

    [Tooltip("Scroll with more ink smudges")]
    [SerializeField] private Sprite scrollDamage2;

    [Tooltip("Scroll completely inked - game over state")]
    [SerializeField] private Sprite scrollDestroyed;

    [Header("Tutorial")]
    [Tooltip("When true, suppresses built-in narrative triggers")]
    [SerializeField] private bool isTutorialLevel = false;

    [Header("Runtime State (Read-Only)")]
    [SerializeField] private int currentDamage = 0;
    [SerializeField] private int maxDamage = 3;

    private SpriteRenderer spriteRenderer;

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

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    void Start()
    {
        // Initialize with clean scroll
        UpdateSprite();
    }
    public bool TakeDamage()
    {
        currentDamage++;

        UpdateSprite();

        if (currentDamage == 1 && !isTutorialLevel)
            NarrativeEventUI.Show("The scroll is damaged! Hold the line!");

        if (currentDamage >= maxDamage)
        {
            return true; // Game over
        }

        return false; // Still alive
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        switch (currentDamage)
        {
            case 0:
                spriteRenderer.sprite = scrollClean;
                break;
            case 1:
                spriteRenderer.sprite = scrollDamage1;
                break;
            case 2:
                spriteRenderer.sprite = scrollDamage2;
                break;
            default:
                spriteRenderer.sprite = scrollDestroyed;
                break;
        }
    }

    public int GetCurrentDamage()
    {
        return currentDamage;
    }

    public int GetRemainingLives()
    {
        return maxDamage - currentDamage;
    }
    public void Reset()
    {
        currentDamage = 0;
        UpdateSprite();
    }
}
