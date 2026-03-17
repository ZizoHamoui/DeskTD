using UnityEngine;

/// <summary>
/// Generic tower attack behavior for detecting and attacking enemies.
/// Handles lane-based enemy detection and projectile firing.
/// Animation is handled by separate per-tower scripts (PencilAnimation, CompassAnimation).
/// </summary>
[RequireComponent(typeof(Tower))]
public class TowerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("Detection radius for finding enemies (should be wide enough to cover lane)")]
    [SerializeField] private float detectionRadius = 10f;

    [Tooltip("Time between attacks (seconds)")]
    [SerializeField] private float fireRate = 1f;

    [Tooltip("Damage dealt per hit")]
    [SerializeField] private float damage = 25f;

    [Header("Projectile Settings")]
    [Tooltip("Projectile prefab to spawn")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("Offset from tower center where projectile spawns")]
    [SerializeField] private Vector3 projectileSpawnOffset = Vector3.zero;

    [Header("Detection Settings")]
    [Tooltip("Layer mask for detecting enemies")]
    [SerializeField] private LayerMask enemyLayerMask;

    [Tooltip("Maximum Y distance difference for lane matching")]
    [SerializeField] private float laneThreshold = 0.1f;

    [Header("Runtime State (Read-Only)")]
    [SerializeField] private Enemy currentTarget;
    [SerializeField] private float lastAttackTime = 0f;
    [SerializeField] private bool isActive = false;

    private Tower tower;
    private CompassAnimation compassAnimation;
    private PencilAnimation pencilAnimation;

    void Awake()
    {
        tower = GetComponent<Tower>();
        compassAnimation = GetComponent<CompassAnimation>();
        pencilAnimation = GetComponent<PencilAnimation>();
    }

    public void InitializeAttack()
    {
        if (projectilePrefab == null)
        {
            return;
        }

        isActive = true;
        lastAttackTime = Time.time; // Initialize to current time
    }

    void Update()
    {
        if (!isActive) return;

        // Find and select target
        currentTarget = FindTarget();

        // Attack if target exists and cooldown ready
        if (currentTarget != null && Time.time >= lastAttackTime + fireRate)
        {
            // Compass uses wind-up animation with delayed projectile
            if (compassAnimation != null && !compassAnimation.IsAnimating)
            {
                lastAttackTime = Time.time;
                Enemy target = currentTarget;
                compassAnimation.PlayWindUp(() =>
                {
                    // Re-check target validity; use current target if original died
                    Enemy attackTarget = (target != null && target.IsAlive()) ? target : currentTarget;
                    if (attackTarget != null && attackTarget.IsAlive())
                    {
                        FireProjectile(attackTarget);
                    }
                });
            }
            else if (compassAnimation == null)
            {
                Attack(currentTarget);
                lastAttackTime = Time.time;
            }
        }
    }

    private Enemy FindTarget()
    {
        // Use overlap circle to find all enemies in detection range
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, enemyLayerMask);

        if (hits.Length == 0) return null;

        // Filter to enemies in same lane and find nearest
        Enemy nearestEnemy = null;
        float nearestX = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy == null || !enemy.IsAlive()) continue;

            // Check if enemy is in same lane (Y coordinate match)
            if (IsInSameLane(enemy.transform.position))
            {
                // Select enemy with lowest X (closest to left edge = most dangerous)
                float enemyX = enemy.transform.position.x;
                if (enemyX < nearestX)
                {
                    nearestX = enemyX;
                    nearestEnemy = enemy;
                }
            }
        }

        return nearestEnemy;
    }

    private bool IsInSameLane(Vector3 position)
    {
        float towerY = transform.position.y;
        float positionY = position.y;
        return Mathf.Abs(positionY - towerY) < laneThreshold;
    }

    private void Attack(Enemy target)
    {
        if (target == null || !target.IsAlive()) return;

        FireProjectile(target);

        // Notify pencil animation if present
        if (pencilAnimation != null)
        {
            pencilAnimation.PlayShootFlash();
        }
    }

    public void FireProjectile(Enemy target)
    {
        Vector3 spawnPosition = transform.position + projectileSpawnOffset;
        GameObject projectileObject = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

        IProjectile projectile = projectileObject.GetComponent<IProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(target, damage, tower);
            AudioManager.PlayShotHit();
        }
        else
        {
            Destroy(projectileObject);
        }
    }

    void OnDrawGizmos()
    {
        if (!isActive) return;

        // Draw detection radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw line to current target
        if (currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }

        // Draw lane indicator (horizontal line)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange
        Gizmos.DrawLine(
            transform.position + Vector3.left * detectionRadius,
            transform.position + Vector3.right * detectionRadius
        );
    }

    public bool IsActive()
    {
        return isActive;
    }

    public Enemy GetCurrentTarget()
    {
        return currentTarget;
    }

    public float GetFireRate()
    {
        return 1f / fireRate;
    }
}
