using UnityEngine;

/// <summary>
/// Base tower component that defines tower properties.
/// Attach this to all tower prefabs.
/// </summary>
public class Tower : MonoBehaviour
{
    [Header("Tower Properties")]
    [Tooltip("Type of tower - used to identify special behaviors")]
    public TowerType towerType;

    [Tooltip("Spark cost to place this tower")]
    public int sparkCost = 5;

    [Header("Grid Position")]
    [SerializeField] private int gridX = -1;
    [SerializeField] private int gridY = -1;

    [Header("Runtime State")]
    [SerializeField] private bool isPlaced = false;
    [SerializeField] private GridTile occupiedTile;

    public void OnTowerPlaced()
    {
        if (isPlaced) return;

        isPlaced = true;

        TowerHealth healthComponent = GetComponent<TowerHealth>();
        if (healthComponent != null && occupiedTile != null)
        {
            healthComponent.Initialize(towerType, occupiedTile);
        }
        else if (healthComponent == null)
        {
        }

        // Handle type-specific placement logic
        if (towerType == TowerType.SparkTower)
        {
            // Register with SparkManager
            SparkTower sparkTower = GetComponent<SparkTower>();
            if (sparkTower != null)
            {
                sparkTower.OnPlaced();
            }
        }
        else if (towerType == TowerType.Pencil)
        {
            // Initialize attack behavior for Pencil tower
            TowerAttack attackComponent = GetComponent<TowerAttack>();
            if (attackComponent != null)
            {
                attackComponent.InitializeAttack();
            }
            else
            {
            }
        }
        else if (towerType == TowerType.Compass)
        {
            TowerAttack attackComponent = GetComponent<TowerAttack>();
            if (attackComponent != null)
            {
                attackComponent.InitializeAttack();
            }
            else
            {
            }
        }
        else if (towerType == TowerType.Eraser)
        {
            // Eraser is a consumable - clean ink and disappear immediately
            if (occupiedTile != null && occupiedTile.IsInk)
            {
                occupiedTile.CleanInk(); // Also activates ink immunity
                occupiedTile.ReleaseTile();
                AudioManager.PlayEraser();
            }
            Destroy(gameObject);
            return;
        }

    }
    public int GetSparkCost()
    {
        return sparkCost;
    }

    public TowerType GetTowerType()
    {
        return towerType;
    }

    /// Returns whether this tower has been placed on the grid.
    public bool IsPlaced()
    {
        return isPlaced;
    }

    /// Sets the grid position for this tower.
    /// Called by TowerDrag when tower is placed on a tile.
    public void SetGridPosition(int x, int y)
    {
        gridX = x;
        gridY = y;
    }

    public int GetLane()
    {
        return gridY;
    }

    public int GetGridX()
    {
        return gridX;
    }

    public int GetGridY()
    {
        return gridY;
    }

    public void SetOccupiedTile(GridTile tile)
    {
        occupiedTile = tile;
    }
}

public enum TowerType
{
    SparkTower,    // Generates spark currency
    Pencil,        // Basic damage tower
    Compass,       // Area damage tower
    StickyNote,    // Defensive tower
    Eraser         // Special utility tower
}
