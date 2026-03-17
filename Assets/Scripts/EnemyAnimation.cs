using UnityEngine;

/// <summary>
/// Handles Ink Enemy walk cycle animation.
/// Cycles through walk sprites while the enemy is moving, pauses when blocked.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class EnemyAnimation : MonoBehaviour
{
    [Header("Walk Animation")]
    [Tooltip("3 sprites for walk cycle loop")]
    [SerializeField] private Sprite[] walkSprites;

    [Tooltip("Duration of each walk animation frame (seconds)")]
    [SerializeField] private float frameDuration = 0.2f;

    private SpriteRenderer spriteRenderer;
    private EnemyAttackBehavior attackBehavior;
    private Enemy enemy;
    private int currentFrame = 0;
    private float frameTimer = 0f;
    private int cachedSortingOrder;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        attackBehavior = GetComponent<EnemyAttackBehavior>();
        enemy = GetComponent<Enemy>();
    }

    void Start()
    {
        if (spriteRenderer != null)
        {
            cachedSortingOrder = spriteRenderer.sortingOrder;
        }

        // Diagnostics
        if (walkSprites == null || walkSprites.Length == 0)
        {
            Debug.LogWarning($"[EnemyAnimation] walkSprites array is empty on {gameObject.name}.", this);
        }
        else
        {
            for (int i = 0; i < walkSprites.Length; i++)
            {
                if (walkSprites[i] == null) Debug.LogWarning($"[EnemyAnimation] walkSprites[{i}] is NULL on {gameObject.name}.", this);
            }
        }
    }

    void Update()
    {
        if (walkSprites == null || walkSprites.Length == 0 || spriteRenderer == null) return;
        if (enemy == null || !enemy.IsAlive()) return;

        // Only animate while moving (not blocked by attacking a tower)
        if (attackBehavior != null && attackBehavior.IsBlocked()) return;

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
