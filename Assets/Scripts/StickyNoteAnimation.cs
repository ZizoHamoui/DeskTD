using UnityEngine;

/// <summary>
/// Handles StickyNote tower damage state animation.
/// Swaps sprite based on how many hits the tower has taken (4 HP = 4 sprites).
/// </summary>
[RequireComponent(typeof(TowerHealth))]
public class StickyNoteAnimation : MonoBehaviour
{
    [Header("Damage Sprites")]
    [Tooltip("4 sprites for damage progression (full, hit1, hit2, hit3)")]
    [SerializeField] private Sprite[] damageSprites;

    private SpriteRenderer spriteRenderer;
    private int cachedSortingOrder;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        if (spriteRenderer != null)
        {
            cachedSortingOrder = spriteRenderer.sortingOrder;
        }

        // Diagnostics
        if (damageSprites == null || damageSprites.Length == 0)
        {
            Debug.LogWarning($"[StickyNoteAnimation] damageSprites array is empty on {gameObject.name}.", this);
        }
        else
        {
            for (int i = 0; i < damageSprites.Length; i++)
            {
                if (damageSprites[i] == null) Debug.LogWarning($"[StickyNoteAnimation] damageSprites[{i}] is NULL on {gameObject.name}.", this);
            }
        }
    }

    public void SetUpgradedSprites(Sprite[] newDamageSprites)
    {
        if (newDamageSprites == null || newDamageSprites.Length == 0) return;
        damageSprites = newDamageSprites;
        if (spriteRenderer != null && newDamageSprites[0] != null)
            spriteRenderer.sprite = newDamageSprites[0];
    }

    public void UpdateDamageSprite(float currentHealth, float maxHealth)
    {
        if (damageSprites == null || damageSprites.Length == 0 || spriteRenderer == null) return;

        int hitsTaken = Mathf.RoundToInt(maxHealth - currentHealth);
        int spriteIndex = Mathf.Clamp(hitsTaken, 0, damageSprites.Length - 1);
        spriteRenderer.sprite = damageSprites[spriteIndex];
        spriteRenderer.sortingOrder = cachedSortingOrder;
    }
}
