using UnityEngine;

/// <summary>
/// Handles Spark Tower fill-level animation.
/// Swaps sprite based on the tower's generation cycle timer:
///   Sprite 1 at 0s, Sprite 2 at 3s, Sprite 3 at 6s, Sprite 4 at 9s.
/// </summary>
[RequireComponent(typeof(SparkTower))]
public class SparkTowerAnimation : MonoBehaviour
{
    [Header("Fill Level Sprites")]
    [Tooltip("4 sprites for cycle progress: 0s, 3s, 6s, 9s")]
    [SerializeField] private Sprite[] fillSprites;

    private SpriteRenderer spriteRenderer;
    private SparkTower sparkTower;
    private int lastFillIndex = -1;
    private int cachedSortingOrder;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        sparkTower = GetComponent<SparkTower>();
    }

    void Start()
    {
        if (spriteRenderer != null)
        {
            cachedSortingOrder = spriteRenderer.sortingOrder;
        }

        // Diagnostics
        if (fillSprites == null || fillSprites.Length == 0)
        {
            Debug.LogWarning($"[SparkTowerAnimation] fillSprites array is empty on {gameObject.name}.", this);
        }
        else
        {
            for (int i = 0; i < fillSprites.Length; i++)
            {
                if (fillSprites[i] == null) Debug.LogWarning($"[SparkTowerAnimation] fillSprites[{i}] is NULL on {gameObject.name}.", this);
            }
        }
    }

    void Update()
    {
        if (fillSprites == null || fillSprites.Length == 0 || spriteRenderer == null) return;
        if (sparkTower == null) return;

        float timer = sparkTower.CycleTimer;
        float interval = sparkTower.CycleInterval;
        int fillIndex;

        // Sprite thresholds at 0%, 30%, 60%, 90% of cycle
        if (timer < interval * 0.3f)
            fillIndex = 0;
        else if (timer < interval * 0.6f)
            fillIndex = 1;
        else if (timer < interval * 0.9f)
            fillIndex = 2;
        else
            fillIndex = 3;

        fillIndex = Mathf.Clamp(fillIndex, 0, fillSprites.Length - 1);

        if (fillIndex != lastFillIndex)
        {
            spriteRenderer.sprite = fillSprites[fillIndex];
            spriteRenderer.sortingOrder = cachedSortingOrder;
            lastFillIndex = fillIndex;
        }
    }
}
