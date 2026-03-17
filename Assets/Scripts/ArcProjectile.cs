using UnityEngine;

public class ArcProjectile : MonoBehaviour, IProjectile
{
    [Header("Arc Settings")]
    [Tooltip("Height of the arc trajectory peak")]
    [SerializeField] private float arcHeight = 5f;

    [Tooltip("Arc sharpness - lower values produce steeper rise/drop (0.3-1.0)")]
    [SerializeField] private float arcSharpness = 0.4f;

    [Tooltip("Total flight time in seconds")]
    [SerializeField] private float flightDuration = 1f;

    [Header("Splash Settings")]
    [Tooltip("Radius of splash damage on impact")]
    [SerializeField] private float splashRadius = 1.5f;

    [Tooltip("Layer mask for detecting enemies on impact")]
    [SerializeField] private LayerMask enemyLayerMask;

    [Header("Runtime State (Read-Only)")]
    [SerializeField] private float damage;
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private Vector3 targetPosition;
    [SerializeField] private float elapsedTime;
    [SerializeField] private bool hasLanded = false;

    private Tower sourceTower;

    void Awake()
    {
        if (enemyLayerMask == 0)
        {
            enemyLayerMask = LayerMask.GetMask("Enemy");
        }
    }

    public void Initialize(Enemy targetEnemy, float damageAmount, Tower sourceTower = null)
    {
        damage = damageAmount;
        this.sourceTower = sourceTower;
        startPosition = transform.position;

        if (targetEnemy != null)
        {
            targetPosition = targetEnemy.transform.position;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (hasLanded) return;

        elapsedTime += Time.deltaTime;
        float t = elapsedTime / flightDuration;

        if (t >= 1f)
        {
            Land();
            return;
        }

        Vector3 previousPosition = transform.position;

        Vector3 position = Vector3.Lerp(startPosition, targetPosition, t);
        position.y += arcHeight * Mathf.Pow(4f * t * (1f - t), arcSharpness);
        transform.position = position;

        Vector3 moveDirection = (position - previousPosition).normalized;
        if (moveDirection != Vector3.zero)
        {
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void Land()
    {
        hasLanded = true;
        transform.position = targetPosition;

        Collider2D[] hits = Physics2D.OverlapCircleAll(targetPosition, splashRadius, enemyLayerMask);

        foreach (Collider2D hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null && enemy.IsAlive())
            {
                enemy.TakeDamage(damage, sourceTower);
            }
        }

        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        if (!hasLanded && Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, splashRadius);
        }
    }
}
