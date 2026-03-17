using UnityEngine;

/// <summary>
/// Projectile behavior for tower attacks.
/// Moves toward target enemy and deals damage on collision.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour, IProjectile
{
    [Header("Movement Settings")]
    [Tooltip("Projectile speed in units per second")]
    [SerializeField] private float speed = 10f;

    [Header("Lifetime Settings")]
    [Tooltip("Time before projectile auto-destructs (seconds)")]
    [SerializeField] private float lifetime = 3f;

    [Header("Runtime State (Read-Only)")]
    [SerializeField] private Enemy target;
    [SerializeField] private float damage;
    [SerializeField] private Vector3 targetPosition;
    [SerializeField] private bool hasHit = false;

    private float spawnTime;
    private Tower sourceTower;

    public void Initialize(Enemy targetEnemy, float damageAmount, Tower sourceTower = null)
    {
        target = targetEnemy;
        damage = damageAmount;
        this.sourceTower = sourceTower;
        spawnTime = Time.time;

        if (target != null)
        {
            targetPosition = target.transform.position;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (hasHit) return;

        // Check lifetime timeout
        if (Time.time > spawnTime + lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Update target position if target still exists
        if (target != null && target.IsAlive())
        {
            targetPosition = target.transform.position;
        }

        // Move toward target position
        MoveTowardsTarget();
    }

    private void MoveTowardsTarget()
    {
        // Calculate direction to target
        Vector3 direction = (targetPosition - transform.position).normalized;

        // Move projectile
        transform.position += direction * speed * Time.deltaTime;

        // Optional: Rotate projectile to face direction
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        // Check if we've passed the target (failsafe)
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget < 0.1f)
        {
            // Close enough - trigger hit
            if (target != null && target.IsAlive())
            {
                OnHitEnemy(target);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // Check if we hit a mug (obstacle)
        Mug mug = other.GetComponent<Mug>();
        if (mug != null)
        {
            OnHitMug();
            return;
        }

        // Check if we hit an enemy
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            OnHitEnemy(enemy);
        }
    }
    private void OnHitMug()
    {
        if (hasHit) return;

        hasHit = true;
        Destroy(gameObject);
    }

    /// <summary>
    /// Handles hitting an enemy.
    /// </summary>
    private void OnHitEnemy(Enemy enemy)
    {
        if (hasHit) return;
        if (enemy == null || !enemy.IsAlive()) return;

        hasHit = true;

        // Deal damage to enemy
        enemy.TakeDamage(damage, sourceTower);


        // Destroy projectile
        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        if (target != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }
}
