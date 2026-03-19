using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Enemy ability that cyclically freezes player towers while the enemy is alive.
/// Freezes one tower at a time for a configurable duration, then moves to a different tower.
/// On death, all frozen towers are immediately restored.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class FreezeAbility : MonoBehaviour
{
    [Header("Freeze Settings")]
    [Tooltip("How long each tower stays frozen")]
    [SerializeField] private float freezeDuration = 3f;

    [Tooltip("Brief pause after unfreezing before picking next target")]
    [SerializeField] private float delayBetweenFreezes = 0.5f;

    [Header("Frozen Tower Sprites")]
    [Tooltip("Mapping of tower types to their frozen sprite")]
    [SerializeField] private FrozenSpriteMapping[] frozenSpriteMappings;

    private Enemy enemy;
    private Tower currentFrozenTower;
    private Sprite currentOriginalSprite;
    private List<Tower> previousTargets = new List<Tower>();

    [System.Serializable]
    public class FrozenSpriteMapping
    {
        public TowerType towerType;
        public Sprite frozenSprite;
    }

    void Start()
    {
        enemy = GetComponent<Enemy>();
        StartCoroutine(FreezeCycleCoroutine());
    }

    private IEnumerator FreezeCycleCoroutine()
    {
        // Small initial delay before first freeze
        yield return new WaitForSeconds(0.5f);

        while (enemy.IsAlive())
        {
            Tower target = PickTarget();

            if (target != null)
            {
                FreezeTower(target);
                yield return new WaitForSeconds(freezeDuration);

                // Tower may have been destroyed while frozen
                if (currentFrozenTower != null)
                {
                    UnfreezeTower(currentFrozenTower);
                }
                currentFrozenTower = null;
                currentOriginalSprite = null;

                yield return new WaitForSeconds(delayBetweenFreezes);
            }
            else
            {
                // No towers available, wait and retry
                yield return new WaitForSeconds(1f);
            }
        }
    }

    private Tower PickTarget()
    {
        // Find all placed, living towers (exclude Eraser and StickyNote)
        Tower[] allTowers = FindObjectsOfType<Tower>();
        List<Tower> validTowers = new List<Tower>();

        foreach (Tower t in allTowers)
        {
            if (t.IsPlaced() && t.towerType != TowerType.Eraser && t.towerType != TowerType.StickyNote)
                validTowers.Add(t);
        }

        if (validTowers.Count == 0) return null;

        // Try to pick a tower not in previousTargets
        List<Tower> candidates = new List<Tower>();
        foreach (Tower t in validTowers)
        {
            if (!previousTargets.Contains(t))
                candidates.Add(t);
        }

        // If all have been frozen this cycle, reset and pick from all (excluding current)
        if (candidates.Count == 0)
        {
            previousTargets.Clear();
            foreach (Tower t in validTowers)
            {
                if (t != currentFrozenTower)
                    candidates.Add(t);
            }

            // If still empty (only 1 tower total), allow re-freezing it
            if (candidates.Count == 0)
                candidates = validTowers;
        }

        Tower picked = candidates[Random.Range(0, candidates.Count)];
        previousTargets.Add(picked);
        return picked;
    }

    private void FreezeTower(Tower tower)
    {
        currentFrozenTower = tower;

        // Swap sprite to frozen variant
        SpriteRenderer sr = tower.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            currentOriginalSprite = sr.sprite;
            Sprite frozenSprite = GetFrozenSprite(tower.towerType);
            if (frozenSprite != null)
                sr.sprite = frozenSprite;
        }

        // Disable tower attack
        TowerAttack attack = tower.GetComponent<TowerAttack>();
        if (attack != null)
            attack.isFrozenDisabled = true;

        // Disable spark generation
        SparkTower sparkTower = tower.GetComponent<SparkTower>();
        if (sparkTower != null)
            sparkTower.isFrozenDisabled = true;
    }

    private void UnfreezeTower(Tower tower)
    {
        // Restore original sprite
        SpriteRenderer sr = tower.GetComponent<SpriteRenderer>();
        if (sr != null && currentOriginalSprite != null)
            sr.sprite = currentOriginalSprite;

        // Re-enable tower attack
        TowerAttack attack = tower.GetComponent<TowerAttack>();
        if (attack != null)
            attack.isFrozenDisabled = false;

        // Re-enable spark generation
        SparkTower sparkTower = tower.GetComponent<SparkTower>();
        if (sparkTower != null)
            sparkTower.isFrozenDisabled = false;
    }

    private Sprite GetFrozenSprite(TowerType type)
    {
        if (frozenSpriteMappings == null) return null;

        foreach (FrozenSpriteMapping mapping in frozenSpriteMappings)
        {
            if (mapping.towerType == type)
                return mapping.frozenSprite;
        }
        return null;
    }

    void OnDestroy()
    {
        // Unfreeze current tower
        if (currentFrozenTower != null)
        {
            UnfreezeTower(currentFrozenTower);
            currentFrozenTower = null;
        }

        // Safety net: unfreeze any towers that might still be flagged
        foreach (TowerAttack attack in FindObjectsOfType<TowerAttack>())
        {
            if (attack.isFrozenDisabled)
                attack.isFrozenDisabled = false;
        }
        foreach (SparkTower spark in FindObjectsOfType<SparkTower>())
        {
            if (spark.isFrozenDisabled)
                spark.isFrozenDisabled = false;
        }
    }
}
