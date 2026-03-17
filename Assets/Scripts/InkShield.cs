using UnityEngine;

/// <summary>
/// Manages the ink trail shield on an enemy. Polls the tile beneath the enemy
/// each frame. Converts clean tiles to ink (Follower Rule: creator gets no shield).
/// Enemies on ink they did not create get a 1 HP shield that absorbs one hit,
/// then enters a configurable cooldown before reactivating.
/// Shield is displayed as a child overlay sprite so it doesn't interfere with walk animation.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class InkShield : MonoBehaviour
{
    [Header("Shield Settings")]
    [Tooltip("Cooldown after shield is destroyed before it can reactivate (seconds)")]
    [SerializeField] private float cooldownDuration = 5f;

    [Tooltip("Number of hits the shield can absorb before breaking")]
    [SerializeField] private int maxHits = 2;

    [Tooltip("Distance from tile center at which the enemy is considered to have 'reached' that tile. Ink conversion and shield evaluation only trigger once this threshold is crossed.")]
    [SerializeField] private float centerReachRadius = 1f;

    [Header("Visuals")]
    [Tooltip("Shield icon sprite overlaid on the enemy when shield is active")]
    [SerializeField] private Sprite shieldOverlaySprite;

    [Tooltip("Sorting order offset above the enemy's SpriteRenderer")]
    [SerializeField] private int shieldOverlaySortingOffset = 1;

    [Header("Runtime State (Read-Only)")]
    [SerializeField] private bool shieldActive = false;
    [SerializeField] private bool onCooldown = false;
    [SerializeField] private float cooldownTimer = 0f;
    [SerializeField] private int hitsRemaining = 0;

    private Enemy enemy;
    private SpriteRenderer spriteRenderer;
    private GameObject shieldOverlay;
    private LayerMask gridLayerMask;
    private GridTile currentTile;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        gridLayerMask = LayerMask.GetMask("Grid");

        // Create child overlay for shield visual
        shieldOverlay = new GameObject("ShieldOverlay");
        shieldOverlay.transform.SetParent(transform, false);
        SpriteRenderer overlayRenderer = shieldOverlay.AddComponent<SpriteRenderer>();
        if (shieldOverlaySprite != null)
        {
            overlayRenderer.sprite = shieldOverlaySprite;
        }
        overlayRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
        overlayRenderer.sortingOrder = spriteRenderer.sortingOrder + shieldOverlaySortingOffset;
        shieldOverlay.SetActive(false);
    }

    void Update()
    {
        if (!enemy.IsAlive()) return;
        if (onCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                onCooldown = false;
                cooldownTimer = 0f;
                // Falls through to re-evaluation below
            }
        }

        Collider2D hit = Physics2D.OverlapPoint((Vector2)transform.position, gridLayerMask);
        GridTile overlappedTile = (hit != null) ? hit.GetComponent<GridTile>() : null;

        if (overlappedTile != null)
        {
            float dist = Vector2.Distance((Vector2)transform.position, (Vector2)overlappedTile.transform.position);
            if (dist <= centerReachRadius)
            {
                currentTile = overlappedTile;
            }
            // else: enemy is between tile centers — keep currentTile as previous tile
        }
        else
        {
            currentTile = null; // Off-grid
        }

        // --- Convert clean tile to ink -------------------------------------
        if (currentTile != null && !currentTile.IsInk)
        {
            currentTile.ConvertToInk(this);
            // This enemy is the creator — no shield from this tile
            if (shieldActive) DeactivateShield();
            return;
        }

        // --- Evaluate desired shield state ---------------------------------
        bool shouldHaveShield =
            currentTile != null &&
            currentTile.IsInk &&
            currentTile.CreatorEnemy != this &&   // Follower Rule
            !onCooldown;

        if (shouldHaveShield && !shieldActive)
            ActivateShield();
        else if (!shouldHaveShield && shieldActive)
            DeactivateShield();
    }

    public bool TryAbsorbDamage()
    {
        if (!shieldActive) return false;

        hitsRemaining--;
        if (hitsRemaining <= 0)
        {
            DeactivateShield();
            onCooldown = true;
            cooldownTimer = cooldownDuration;
        }
        return true;
    }

    private void ActivateShield()
    {
        shieldActive = true;
        hitsRemaining = maxHits;
        shieldOverlay.SetActive(true);
    }

    private void DeactivateShield()
    {
        shieldActive = false;
        shieldOverlay.SetActive(false);
    }
}
