using UnityEngine;
using System.Collections;

/// <summary>
/// Handles enemy attack behavior when encountering towers.
/// Manages detection, blocking, lunge animation, and damage dealing.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class EnemyAttackBehavior : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Layer mask for detecting towers")]
    [SerializeField] private LayerMask towerLayerMask;

    [Tooltip("Detection radius for finding towers on same tile")]
    [SerializeField] private float detectionRadius = 0.6f;

    [Tooltip("Y position threshold for lane matching")]
    [SerializeField] private float laneThreshold = 0.1f;

    [Header("Attack Settings")]
    [Tooltip("Damage dealt per attack")]
    [SerializeField] private float attackDamage = 1f;

    [Tooltip("Time between attacks (seconds)")]
    [SerializeField] private float attackCooldown = 1.0f;

    [Header("Animation Settings")]
    [Tooltip("Distance to lunge forward during attack")]
    [SerializeField] private float lungeDistance = 0.3f;

    [Tooltip("Duration of lunge animation (seconds)")]
    [SerializeField] private float lungeDuration = 0.15f;

    [Header("Runtime State (Read-Only)")]
    [SerializeField] private EnemyState currentState = EnemyState.Moving;
    [SerializeField] private Tower currentTarget;
    [SerializeField] private bool isBlocked = false;

    private Enemy enemy;
    private Vector3 originalPosition;
    private Coroutine attackCoroutine;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    void Update()
    {
        if (!enemy.IsAlive()) return;

        // State machine logic
        switch (currentState)
        {
            case EnemyState.Moving:
                CheckForTowerCollision();
                break;

            case EnemyState.Attacking:
                // Attack coroutine is running
                // Verify target still exists
                if (currentTarget == null || IsTargetDestroyed())
                {
                    ResumeMovement();
                }
                break;

            case EnemyState.Lunging:
            case EnemyState.Returning:
                // Animation in progress - managed by coroutine
                break;
        }
    }

    private void CheckForTowerCollision()
    {
        // Use overlap circle to find towers in detection range
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, towerLayerMask);

        if (hits.Length == 0) return;

        // Filter to towers in same lane and ahead of enemy
        Tower nearestTower = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            Tower tower = hit.GetComponent<Tower>();
            if (tower == null || !tower.IsPlaced()) continue;

            // Check if tower is in same lane (Y coordinate match)
            if (!IsInSameLane(tower.transform.position)) continue;

            // Only detect towers ahead (to the left or at same X)
            float towerX = tower.transform.position.x;
            float enemyX = transform.position.x;
            if (towerX > enemyX) continue; // Tower is behind enemy

            // Find nearest tower
            float distance = Vector3.Distance(transform.position, tower.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTower = tower;
            }
        }

        // If tower found, start attacking
        if (nearestTower != null)
        {
            StartAttacking(nearestTower);
        }
    }

    private bool IsInSameLane(Vector3 position)
    {
        float enemyY = transform.position.y;
        float positionY = position.y;
        return Mathf.Abs(positionY - enemyY) < laneThreshold;
    }

    private void StartAttacking(Tower tower)
    {
        if (currentState != EnemyState.Moving) return;

        currentTarget = tower;
        currentState = EnemyState.Attacking;
        isBlocked = true;


        // Start attack sequence coroutine
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }
        attackCoroutine = StartCoroutine(AttackSequence());
    }

    private IEnumerator AttackSequence()
    {
        while (currentTarget != null && !IsTargetDestroyed())
        {
            // Wait for cooldown before attacking
            yield return new WaitForSeconds(attackCooldown);

            // Verify target still exists
            if (currentTarget == null || IsTargetDestroyed())
            {
                break;
            }

            // Perform lunge animation and deal damage
            yield return StartCoroutine(PerformLunge());
        }

        // Tower destroyed or lost - resume movement
        ResumeMovement();
    }

    private IEnumerator PerformLunge()
    {
        if (currentTarget == null) yield break;

        currentState = EnemyState.Lunging;
        originalPosition = transform.position;

        // Calculate lunge position (toward tower)
        Vector3 targetPosition = currentTarget.transform.position;
        Vector3 direction = (targetPosition - originalPosition).normalized;
        Vector3 lungePosition = originalPosition + direction * lungeDistance;

        // Lunge forward
        float elapsed = 0f;
        while (elapsed < lungeDuration)
        {
            transform.position = Vector3.Lerp(originalPosition, lungePosition, elapsed / lungeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = lungePosition;

        // Deal damage at peak of lunge
        DealDamageToTower();

        // Return to original position
        currentState = EnemyState.Returning;
        elapsed = 0f;
        while (elapsed < lungeDuration)
        {
            transform.position = Vector3.Lerp(lungePosition, originalPosition, elapsed / lungeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPosition;

        currentState = EnemyState.Attacking;
    }

    private void DealDamageToTower()
    {
        if (currentTarget == null) return;

        TowerHealth towerHealth = currentTarget.GetComponent<TowerHealth>();
        if (towerHealth != null && !towerHealth.IsDestroyed())
        {
            towerHealth.TakeDamage(attackDamage);
            AudioManager.PlayEnemyAttack();
        }
    }

    private bool IsTargetDestroyed()
    {
        if (currentTarget == null) return true;

        TowerHealth towerHealth = currentTarget.GetComponent<TowerHealth>();
        if (towerHealth == null) return true;

        return towerHealth.IsDestroyed();
    }

    /// Resumes normal movement after tower is destroyed.
    private void ResumeMovement()
    {
        isBlocked = false;
        currentState = EnemyState.Moving;
        currentTarget = null;

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

    }

    public bool IsBlocked()
    {
        return isBlocked;
    }

    public EnemyState GetCurrentState()
    {
        return currentState;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Draw detection radius
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Orange
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw line to current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }

        // Draw lane indicator
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f); // Yellow
        Gizmos.DrawLine(
            transform.position + Vector3.left * detectionRadius,
            transform.position + Vector3.right * detectionRadius
        );
    }
}

public enum EnemyState
{
    Moving,      // Normal movement along lane
    Attacking,   // Stopped at tower, attacking
    Lunging,     // Animation - moving toward tower
    Returning    // Animation - returning to position
}
