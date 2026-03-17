using UnityEngine;

/// <summary>
/// Obstacle that blocks tower placement and projectiles, but lets enemies pass through.
/// Spawned at scene start on random tiles by MugSpawner.
/// </summary>
public class Mug : MonoBehaviour
{
    [Header("Runtime State (Read-Only)")]
    [SerializeField] private GridTile occupiedTile;

    public void Initialize(GridTile tile)
    {
        occupiedTile = tile;

        // Occupy the tile to block tower placement
        if (tile != null)
        {
            tile.TryOccupy(this.gameObject);
        }

        // Snap to tile position
        transform.position = tile.transform.position;

    }

    public GridTile GetOccupiedTile()
    {
        return occupiedTile;
    }
}
