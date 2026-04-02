using System.Collections;
using UnityEngine;

/// <summary>
/// Enemy component that handles health, lane-based movement, and death.
/// Enemies spawn from the right and move left along their assigned lane.
/// Walk animation handled by EnemyAnimation.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    [Tooltip("Enemy health points")]
    [SerializeField] private float maxHealth = 100f;

    [Tooltip("Movement speed in units per second")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Lane Settings")]
    [Tooltip("Lane index (Y coordinate) this enemy moves in")]
    [SerializeField] private int lane = 0;

    [Header("Boundary")]
    [Tooltip("X position where enemy triggers lose condition (left edge of grid)")]
    [SerializeField] private float leftEdgeThreshold = -0.5f;

    [Header("Runtime State (Read-Only)")]
    [SerializeField] private float currentHealth;
    [SerializeField] private bool isAlive = true;

    [HideInInspector] public bool isExternallyBlocked = false;
    [HideInInspector] public float damageMultiplier = 1f;

    private bool isSpeedBuffed = false;
    private InkShield inkShield;
    private SpriteRenderer spriteRenderer;
    private Coroutine flashCoroutine;
    private Material flashMaterial;
    private Material originalMaterial;
    private bool hasTriggeredSecondLastAlarm = false;
    private bool hasTriggeredLastAlarm = false;
    private float secondLastTileX = float.MinValue;
    private float lastTileX = float.MinValue;
    private float laneWorldY = 0f;
    private float laneHeight = 1f;

    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Auto-configure thresholds based on grid
        GridGenerator gridGen = FindObjectOfType<GridGenerator>();
        if (gridGen != null)
        {
            if (leftEdgeThreshold == -0.5f)
            {
                leftEdgeThreshold = gridGen.transform.position.x - 0.5f;
            }

            // Alarm trigger positions
            GridTile secondLastTile = gridGen.GetTile(1, 0);
            if (secondLastTile != null)
                secondLastTileX = secondLastTile.transform.position.x;

            GridTile lastTile = gridGen.GetTile(0, 0);
            if (lastTile != null)
                lastTileX = lastTile.transform.position.x;

            GridTile laneTile = gridGen.GetTile(0, lane);
            if (laneTile != null)
            {
                laneWorldY = laneTile.transform.position.y;
                SpriteRenderer laneSR = laneTile.GetComponent<SpriteRenderer>();
                if (laneSR != null)
                    laneHeight = laneSR.bounds.size.y;
            }
        }

        inkShield = GetComponent<InkShield>();

        if (spriteRenderer != null)
            originalMaterial = spriteRenderer.material;
    }

    void Update()
    {
        if (!isAlive) return;

        // Move left along X-axis, Y stays constant (lane-locked)
        MoveLeft();

        // Check if entered second-last tile — trigger alarm
        if (!hasTriggeredSecondLastAlarm && secondLastTileX != float.MinValue
            && transform.position.x <= secondLastTileX)
        {
            hasTriggeredSecondLastAlarm = true;
            if (ScreenFlash.Instance != null)
                ScreenFlash.Instance.Flash(laneWorldY, laneHeight);
        }

        // Check if entered last tile — trigger alarm again
        if (!hasTriggeredLastAlarm && lastTileX != float.MinValue
            && transform.position.x <= lastTileX)
        {
            hasTriggeredLastAlarm = true;
            if (ScreenFlash.Instance != null)
                ScreenFlash.Instance.Flash(laneWorldY, laneHeight);
        }

        // Check if reached left edge (lose condition)
        if (transform.position.x < leftEdgeThreshold)
        {
            ReachDestination();
        }
    }

    /// <summary>
    /// Moves the enemy left at constant speed.
    /// Y position remains unchanged (lane-locked movement).
    /// </summary>
    private void MoveLeft()
    {
        // Check if enemy is blocked by a tower or external controller
        EnemyAttackBehavior attackBehavior = GetComponent<EnemyAttackBehavior>();
        if (attackBehavior != null && attackBehavior.IsBlocked())
        {
            return;
        }
        if (isExternallyBlocked)
        {
            return;
        }

        Vector3 newPosition = transform.position;
        newPosition.x -= moveSpeed * Time.deltaTime;
        transform.position = newPosition;
    }

    /// <summary>
    /// Called when enemy takes damage from a tower or projectile.
    /// If an active ink shield is present, the first hit is fully absorbed.
    /// </summary>
    public void TakeDamage(float damage)
    {
        TakeDamage(damage, null);
    }

    public void TakeDamage(float damage, Tower source)
    {
        if (!isAlive) return;

        // Shield absorption — if InkShield is present and active, first hit is fully absorbed.
        if (inkShield != null && inkShield.TryAbsorbDamage())
        {
            return;
        }

        currentHealth -= damage * damageMultiplier;

        if (currentHealth <= 0)
        {
            Die(source);
        }
        else
        {
            // Flash white on hit (only if still alive)
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(HitFlash());
        }
    }

    private IEnumerator HitFlash()
    {
        if (spriteRenderer == null || originalMaterial == null) yield break;

        // Create flash material once
        if (flashMaterial == null)
        {
            flashMaterial = new Material(originalMaterial);
            flashMaterial.color = new Color(1f, 1f, 1f, 0.5f);
        }

        // Swap to white-tinted copy
        spriteRenderer.material = flashMaterial;
        flashMaterial.mainTexture = originalMaterial.mainTexture;

        yield return new WaitForSeconds(0.12f);

        if (spriteRenderer != null)
            spriteRenderer.material = originalMaterial;
    }

    private void Die(Tower killingTower = null)
    {
        if (!isAlive) return;

        isAlive = false;

        // Notify WaveManager
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnEnemyDeath(killingTower, transform.position);
        }

        Destroy(gameObject);
    }


    private void ReachDestination()
    {
        if (!isAlive) return;

        isAlive = false;

        // Notify WaveManager of lose condition
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnEnemyReachedEnd();
        }

        Destroy(gameObject);
    }

    public void ApplySpeedBuff(float multiplier, float duration)
    {
        if (isSpeedBuffed) return;
        StartCoroutine(SpeedBuffCoroutine(multiplier, duration));
    }

    private IEnumerator SpeedBuffCoroutine(float multiplier, float duration)
    {
        isSpeedBuffed = true;
        float original = moveSpeed;
        moveSpeed *= multiplier;
        yield return new WaitForSeconds(duration);
        moveSpeed = original;
        isSpeedBuffed = false;
    }

    public float GetMoveSpeed()
    {
        return moveSpeed;
    }

    public void SetLane(int laneIndex)
    {
        lane = laneIndex;
    }

    public int GetLane()
    {
        return lane;
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public bool IsAlive()
    {
        return isAlive;
    }

    void OnDrawGizmos()
    {
        if (isAlive)
        {
            // Draw line showing movement direction
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.left * 2f);

            // Draw sphere colored by health percentage
            float healthPercent = currentHealth / maxHealth;
            Gizmos.color = Color.Lerp(Color.red, Color.green, healthPercent);
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
