using UnityEngine;

/// <summary>
/// Handles Ink Brute walk cycle animation.
/// Cycles through walk sprites while the Brute is advancing, pauses when blocked.
/// Mirrors EnemyAnimation but references InkBruteController instead of EnemyAttackBehavior.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class InkBruteAnimation : MonoBehaviour
{
    [Header("Walk Animation")]
    [Tooltip("Sprites for walk cycle loop")]
    [SerializeField] private Sprite[] walkSprites;

    [Tooltip("Duration of each walk animation frame (seconds)")]
    [SerializeField] private float frameDuration = 0.2f;

    private SpriteRenderer spriteRenderer;
    private InkBruteController bruteController;
    private Enemy enemy;
    private int currentFrame = 0;
    private float frameTimer = 0f;
    private int cachedSortingOrder;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        bruteController = GetComponent<InkBruteController>();
        enemy = GetComponent<Enemy>();
    }

    void Start()
    {
        if (spriteRenderer != null)
        {
            cachedSortingOrder = spriteRenderer.sortingOrder;
        }
    }

    void Update()
    {
        if (walkSprites == null || walkSprites.Length == 0 || spriteRenderer == null) return;
        if (enemy == null || !enemy.IsAlive()) return;

        // Only animate while advancing (not blocked)
        if (bruteController != null && bruteController.IsBlocked()) return;

        frameTimer += Time.deltaTime;
        if (frameTimer >= frameDuration)
        {
            frameTimer = 0f;
            currentFrame = (currentFrame + 1) % walkSprites.Length;
            spriteRenderer.sprite = walkSprites[currentFrame];
            spriteRenderer.sortingOrder = cachedSortingOrder;
        }
    }
}
