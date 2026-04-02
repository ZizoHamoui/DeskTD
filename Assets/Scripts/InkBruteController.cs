using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// FSM + Behavior Tree controller for the Ink Brute enemy.
/// Manages 5 states: Advance, Engage, Charge, Rally, Recovery.
/// Two behavior trees (Rally and Charge) are nested inside FSM states
/// to handle decision logic.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class InkBruteController : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Layer mask for detecting towers")]
    [SerializeField] private LayerMask towerLayerMask;

    [Tooltip("Number of tiles ahead for tower detection")]
    [SerializeField] private int detectionTileRange = 2;

    [Tooltip("Y position threshold for lane matching")]
    [SerializeField] private float laneThreshold = 0.1f;

    [Header("Standard Attack Settings")]
    [Tooltip("Damage dealt per standard lunge attack")]
    [SerializeField] private float attackDamage = 1f;

    [Tooltip("Time between standard attacks (seconds)")]
    [SerializeField] private float attackCooldown = 1.0f;

    [Tooltip("Distance to lunge forward during standard attack")]
    [SerializeField] private float lungeDistance = 0.3f;

    [Tooltip("Duration of lunge animation (seconds)")]
    [SerializeField] private float lungeDuration = 0.15f;

    [Header("Charge Settings")]
    [Tooltip("Damage dealt by charge attack")]
    [SerializeField] private float chargeDamage = 2f;

    [Tooltip("Movement speed during charge (units/sec)")]
    [SerializeField] private float chargeSpeed = 8f;

    [Tooltip("Cooldown between charge attacks (seconds)")]
    [SerializeField] private float chargeCooldown = 10f;

    [Header("Rally Settings")]
    [Tooltip("Speed multiplier applied to rallied enemies")]
    [SerializeField] private float rallySpeedMultiplier = 1.5f;

    [Tooltip("How long the speed buff lasts on rallied enemies (seconds)")]
    [SerializeField] private float rallyBuffDuration = 3f;

    [Tooltip("Cooldown between rally abilities (seconds)")]
    [SerializeField] private float rallyCooldown = 10f;

    [Tooltip("Duration of rally animation pause (seconds)")]
    [SerializeField] private float rallyAnimDuration = 1f;

    [Tooltip("Tint color applied to rallied enemies")]
    [SerializeField] private Color rallyTintColor = new Color(1f, 0.7f, 0.3f, 1f);

    [Header("Recovery Settings")]
    [Tooltip("Duration of recovery state after charge (seconds)")]
    [SerializeField] private float recoveryDuration = 5f;

    [Tooltip("Damage multiplier during recovery (e.g. 2 = double damage)")]
    [SerializeField] private float doubleDamageMultiplier = 2f;

    [Header("Ability Timing")]
    [Tooltip("Initial delay before abilities can activate after spawning (seconds)")]
    [SerializeField] private float initialAbilityDelay = 15f;

    [Header("Runtime State (Read-Only)")]
    [SerializeField] private BruteState currentState = BruteState.Advance;
    [SerializeField] private Tower currentTarget;

    // Timers
    private float abilityTimer;
    private float chargeTimer;
    private float rallyTimer;
    private float attackTimer;

    // Computed detection distance in world units
    private float detectionDistance;

    // References
    private Enemy enemy;
    private Coroutine activeCoroutine;

    // Behavior Trees
    private BTSelector rallyBT;
    private BTSelector chargeBT;

    void Start()
    {
        enemy = GetComponent<Enemy>();

        // Compute detection distance from grid tile spacing
        if (GridGenerator.Instance != null)
        {
            float tileSpacing = GridGenerator.Instance.GetTileSpacing();
            detectionDistance = detectionTileRange * tileSpacing;
        }
        else
        {
            detectionDistance = 2f;
        }

        // Initialize timers
        abilityTimer = initialAbilityDelay;
        chargeTimer = 0f;
        rallyTimer = 0f;
        attackTimer = 0f;

        // Build behavior trees
        BuildBehaviorTrees();
    }

    /// <summary>
    /// Constructs the two behavior trees used by the Ink Brute.
    /// BT-A (Rally): Evaluated during Advance to decide whether to rally allies.
    /// BT-B (Charge): Evaluated during Advance when a tower is detected, to decide charge vs engage.
    /// </summary>
    private void BuildBehaviorTrees()
    {
        // BT-A: Rally Decision Tree
        // [Selector]
        // +-- [Sequence "Rally Check"]
        // |   +-- [Condition] >=2 allied enemies within rally range (adjacent lanes)
        // |   +-- [Condition] Rally cooldown expired
        // |   +-- [Action] Transition to Rally state
        // +-- [Sequence "Normal March"]
        //     +-- [Condition] No tower in detection range
        //     +-- [Action] Continue moving (no-op)
        rallyBT = new BTSelector(
            new BTSequence(
                new BTCondition(() => CountNearbyAllies() >= 2),
                new BTCondition(() => rallyTimer <= 0f),
                new BTAction(() =>
                {
                    TransitionToRally();
                    return BTStatus.Success;
                })
            ),
            new BTSequence(
                new BTCondition(() => !IsTowerInDetectionRange()),
                new BTAction(() => BTStatus.Success)
            )
        );

        // BT-B: Charge Decision Tree
        // [Selector]
        // +-- [Sequence "Charge Attack"]
        // |   +-- [Condition] Tower within detection range in same lane
        // |   +-- [Condition] Charge cooldown expired
        // |   +-- [Action] Transition to Charge state
        // +-- [Sequence "Standard Engage"]
        //     +-- [Condition] Tower in detection range (fallback)
        //     +-- [Action] Transition to Engage state
        chargeBT = new BTSelector(
            new BTSequence(
                new BTCondition(() => currentTarget != null),
                new BTCondition(() => chargeTimer <= 0f),
                new BTAction(() =>
                {
                    TransitionToCharge();
                    return BTStatus.Success;
                })
            ),
            new BTSequence(
                new BTCondition(() => currentTarget != null),
                new BTAction(() =>
                {
                    TransitionToEngage();
                    return BTStatus.Success;
                })
            )
        );
    }

    void Update()
    {
        if (!enemy.IsAlive()) return;

        // Tick down timers
        if (abilityTimer > 0f) abilityTimer -= Time.deltaTime;
        if (chargeTimer > 0f) chargeTimer -= Time.deltaTime;
        if (rallyTimer > 0f) rallyTimer -= Time.deltaTime;
        if (attackTimer > 0f) attackTimer -= Time.deltaTime;

        switch (currentState)
        {
            case BruteState.Advance:
                HandleAdvanceState();
                break;

            case BruteState.Engage:
                HandleEngageState();
                break;

            case BruteState.Charge:
            case BruteState.Rally:
            case BruteState.Recovery:
                // These states are driven by coroutines
                break;
        }
    }

    /// <summary>
    /// Advance state: Move forward, evaluate BT-A (Rally), scan for towers, evaluate BT-B (Charge vs Engage).
    /// </summary>
    private void HandleAdvanceState()
    {
        enemy.isExternallyBlocked = false;

        // If abilities are available, evaluate Rally BT first
        if (abilityTimer <= 0f)
        {
            rallyBT.Tick();
            if (currentState != BruteState.Advance) return;
        }

        // Scan for tower in detection range
        Tower tower = ScanForTower();
        if (tower != null)
        {
            currentTarget = tower;

            // If abilities available, evaluate Charge BT (charge vs standard engage)
            if (abilityTimer <= 0f)
            {
                chargeBT.Tick();
            }
            else
            {
                // Abilities not ready yet, go straight to Engage
                TransitionToEngage();
            }
        }
    }

    /// <summary>
    /// Engage state: Verify target, run lunge attack loop via coroutine.
    /// </summary>
    private void HandleEngageState()
    {
        if (currentTarget == null || IsTargetDestroyed())
        {
            TransitionToAdvance();
        }
    }

    // ─── State Transitions ───

    private void TransitionToAdvance()
    {
        StopActiveCoroutine();
        currentState = BruteState.Advance;
        currentTarget = null;
        enemy.isExternallyBlocked = false;
        enemy.damageMultiplier = 1f;
    }

    private void TransitionToEngage()
    {
        currentState = BruteState.Engage;
        enemy.isExternallyBlocked = true;
        StartActiveCoroutine(EngageCoroutine());
    }

    private void TransitionToCharge()
    {
        currentState = BruteState.Charge;
        enemy.isExternallyBlocked = true;
        chargeTimer = chargeCooldown;
        StartActiveCoroutine(ChargeCoroutine());
    }

    private void TransitionToRally()
    {
        currentState = BruteState.Rally;
        enemy.isExternallyBlocked = true;
        rallyTimer = rallyCooldown;
        StartActiveCoroutine(RallyCoroutine());
    }

    private void TransitionToRecovery()
    {
        currentState = BruteState.Recovery;
        enemy.isExternallyBlocked = true;
        enemy.damageMultiplier = doubleDamageMultiplier;
        StartActiveCoroutine(RecoveryCoroutine());
    }

    // ─── Coroutines ───

    private IEnumerator EngageCoroutine()
    {
        while (currentTarget != null && !IsTargetDestroyed())
        {
            yield return new WaitForSeconds(attackCooldown);

            if (currentTarget == null || IsTargetDestroyed())
                break;

            yield return PerformLunge();
        }

        if (currentState == BruteState.Engage)
            TransitionToAdvance();
    }

    private IEnumerator PerformLunge()
    {
        if (currentTarget == null) yield break;

        Vector3 originalPosition = transform.position;
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

        // Deal damage at peak
        DealDamageToTower(attackDamage);

        // Return to original position
        elapsed = 0f;
        while (elapsed < lungeDuration)
        {
            transform.position = Vector3.Lerp(lungePosition, originalPosition, elapsed / lungeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPosition;
    }

    private IEnumerator ChargeCoroutine()
    {
        if (currentTarget == null)
        {
            TransitionToRecovery();
            yield break;
        }

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = currentTarget.transform.position;

        // Dash toward tower
        while (currentTarget != null && Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            transform.position += direction * chargeSpeed * Time.deltaTime;
            yield return null;
        }

        // Deal charge damage (even if tower was destroyed mid-charge, skip damage)
        if (currentTarget != null)
        {
            DealDamageToTower(chargeDamage);
        }

        // Return to start position
        float returnDuration = 0.3f;
        float elapsed = 0f;
        Vector3 currentPos = transform.position;
        while (elapsed < returnDuration)
        {
            transform.position = Vector3.Lerp(currentPos, startPosition, elapsed / returnDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = startPosition;

        // Always enter recovery after charge
        TransitionToRecovery();
    }

    private IEnumerator RallyCoroutine()
    {
        // Snapshot allies in adjacent lanes
        List<Enemy> allies = GetNearbyAllies();

        // Brief scale-up for visual feedback on the Brute
        Vector3 originalScale = transform.localScale;
        transform.localScale = originalScale * 1.15f;

        // Apply speed buff and tint to each ally
        foreach (Enemy ally in allies)
        {
            if (ally != null && ally.IsAlive())
            {
                ally.ApplySpeedBuff(rallySpeedMultiplier, rallyBuffDuration);
                StartCoroutine(TintEnemy(ally, rallyBuffDuration));
            }
        }

        // Hold rally pose
        yield return new WaitForSeconds(rallyAnimDuration);

        // Restore Brute scale
        transform.localScale = originalScale;

        if (currentState == BruteState.Rally)
            TransitionToAdvance();
    }

    private IEnumerator RecoveryCoroutine()
    {
        yield return new WaitForSeconds(recoveryDuration);

        enemy.damageMultiplier = 1f;

        if (currentState == BruteState.Recovery)
            TransitionToAdvance();
    }

    /// <summary>
    /// Tints a rallied enemy for the buff duration, then restores original color.
    /// Runs independently on this MonoBehaviour so the buff persists even if the
    /// target enemy has no special components.
    /// </summary>
    private IEnumerator TintEnemy(Enemy target, float duration)
    {
        SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        Color original = sr.color;
        sr.color = rallyTintColor;

        yield return new WaitForSeconds(duration);

        // Enemy may have been destroyed during buff
        if (sr != null)
            sr.color = original;
    }

    // ─── Detection & Queries ───

    /// <summary>
    /// Scans for the nearest tower ahead in the same lane within detection range.
    /// </summary>
    private Tower ScanForTower()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionDistance, towerLayerMask);

        if (hits.Length == 0) return null;

        Tower nearestTower = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            Tower tower = hit.GetComponent<Tower>();
            if (tower == null || !tower.IsPlaced()) continue;

            // Same lane check
            if (Mathf.Abs(tower.transform.position.y - transform.position.y) >= laneThreshold)
                continue;

            // Only towers ahead (to the left)
            if (tower.transform.position.x > transform.position.x) continue;

            float distance = Vector3.Distance(transform.position, tower.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTower = tower;
            }
        }

        return nearestTower;
    }

    private bool IsTowerInDetectionRange()
    {
        return ScanForTower() != null;
    }

    /// <summary>
    /// Counts allied enemies in the same lane and adjacent lanes (excluding self).
    /// </summary>
    private int CountNearbyAllies()
    {
        int myLane = enemy.GetLane();
        int count = 0;

        Enemy[] allEnemies = FindObjectsOfType<Enemy>();
        foreach (Enemy e in allEnemies)
        {
            if (e == enemy) continue;
            if (!e.IsAlive()) continue;
            if (Mathf.Abs(e.GetLane() - myLane) <= 1)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Gets a snapshot list of allied enemies in adjacent lanes (excluding self).
    /// </summary>
    private List<Enemy> GetNearbyAllies()
    {
        int myLane = enemy.GetLane();
        List<Enemy> allies = new List<Enemy>();

        Enemy[] allEnemies = FindObjectsOfType<Enemy>();
        foreach (Enemy e in allEnemies)
        {
            if (e == enemy) continue;
            if (!e.IsAlive()) continue;
            if (Mathf.Abs(e.GetLane() - myLane) <= 1)
                allies.Add(e);
        }

        return allies;
    }

    private void DealDamageToTower(float damage)
    {
        if (currentTarget == null) return;

        TowerHealth towerHealth = currentTarget.GetComponent<TowerHealth>();
        if (towerHealth != null && !towerHealth.IsDestroyed())
        {
            towerHealth.TakeDamage(damage);
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

    // ─── Coroutine Management ───

    private void StartActiveCoroutine(IEnumerator routine)
    {
        StopActiveCoroutine();
        activeCoroutine = StartCoroutine(routine);
    }

    private void StopActiveCoroutine()
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }
    }

    // ─── Public API ───

    /// <summary>
    /// Returns true when the Brute should not be moving (used by InkBruteAnimation).
    /// </summary>
    public bool IsBlocked()
    {
        return currentState != BruteState.Advance;
    }

    public BruteState GetCurrentState()
    {
        return currentState;
    }

    // ─── Gizmos ───

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Detection range
        Gizmos.color = new Color(0.8f, 0.2f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionDistance);

        // Current target line
        if (currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }

        // State indicator
        switch (currentState)
        {
            case BruteState.Rally:
                Gizmos.color = new Color(1f, 0.7f, 0.3f, 0.5f);
                Gizmos.DrawWireSphere(transform.position, detectionDistance * 0.5f);
                break;
            case BruteState.Recovery:
                Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.5f);
                Gizmos.DrawWireSphere(transform.position, 0.4f);
                break;
        }
    }
}

public enum BruteState
{
    Advance,
    Engage,
    Charge,
    Rally,
    Recovery
}
