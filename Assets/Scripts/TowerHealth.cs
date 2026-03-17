using UnityEngine;

/// <summary>
/// Manages tower health, damage reception, and destruction.
/// Towers can be damaged by enemies and are destroyed when health reaches 0.
/// </summary>
[RequireComponent(typeof(Tower))]
public class TowerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Current health of the tower")]
    [SerializeField] private float currentHealth;

    [Tooltip("Maximum health based on tower type")]
    [SerializeField] private float maxHealth;

    [Header("References")]
    [SerializeField] private TowerType towerType;
    [SerializeField] private GridTile occupiedTile;

    [Header("Runtime State (Read-Only)")]
    [SerializeField] private bool isDestroyed = false;

    // StickyNote defensive quip tracking
    private int hitsSinceLastQuip;
    private int quipThreshold;

    public void Initialize(TowerType type, GridTile tile)
    {
        towerType = type;
        occupiedTile = tile;

        // Set max health based on tower type
        if (type == TowerType.StickyNote)
        {
            maxHealth = 4f; // Defensive tower - takes 4 hits
        }
        else
        {
            maxHealth = 2f; // Standard towers - take 2 hits
        }

        currentHealth = maxHealth;

        // Initialize quip tracking and damage sprite for StickyNote
        if (type == TowerType.StickyNote)
        {
            hitsSinceLastQuip = 0;
            quipThreshold = Random.Range(2, 5); // 2, 3, or 4

            StickyNoteAnimation anim = GetComponent<StickyNoteAnimation>();
            if (anim != null) anim.UpdateDamageSprite(currentHealth, maxHealth);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;

        currentHealth -= damage;

        // StickyNote defensive quip: show taunt after random number of hits (if still alive)
        if (towerType == TowerType.StickyNote && currentHealth > 0)
        {
            hitsSinceLastQuip++;
            if (hitsSinceLastQuip >= quipThreshold)
            {
                Tower tower = GetComponent<Tower>();
                if (tower != null)
                {
                    KillQuipDisplay.ShowDefensiveQuip(tower);
                }
                hitsSinceLastQuip = 0;
                quipThreshold = Random.Range(2, 5); // 2, 3, or 4
            }
        }

        // Update StickyNote damage sprite via animation component
        if (towerType == TowerType.StickyNote)
        {
            StickyNoteAnimation anim = GetComponent<StickyNoteAnimation>();
            if (anim != null) anim.UpdateDamageSprite(currentHealth, maxHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDestroyed) return;

        isDestroyed = true;

        Tower tower = GetComponent<Tower>();

        // Release the occupied tile so new towers can be placed
        if (occupiedTile != null)
        {
            occupiedTile.ReleaseTile();
        }
        else
        {
        }

        // Type-specific cleanup handled by OnDestroy hooks in other components
        // (e.g., SparkTower.OnDestroy() unregisters from SparkManager)

        // Destroy the tower GameObject
        Destroy(gameObject);
    }

    public bool IsDestroyed()
    {
        return isDestroyed;
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public float GetHealthPercentage()
    {
        if (maxHealth <= 0) return 0f;
        return currentHealth / maxHealth;
    }

    void OnDrawGizmos()
    {
        if (isDestroyed) return;

        // Draw health bar above tower
        Vector3 position = transform.position + Vector3.up * 0.6f;
        float healthPercent = currentHealth / maxHealth;

        // Health bar background (red)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(position + Vector3.left * 0.3f, position + Vector3.right * 0.3f);

        // Health bar foreground (green)
        Gizmos.color = Color.green;
        Vector3 healthEnd = position + Vector3.left * 0.3f + Vector3.right * (0.6f * healthPercent);
        Gizmos.DrawLine(position + Vector3.left * 0.3f, healthEnd);
    }
}
