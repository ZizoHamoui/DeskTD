using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Spawns mug obstacles on random tiles at scene start.
/// Attach to a scene GameObject (e.g., GridGenerator or a dedicated Spawner object).
/// </summary>
public class MugSpawner : MonoBehaviour
{
    [Header("Spawning Configuration")]
    [Tooltip("Mug prefab to spawn")]
    [SerializeField] private GameObject mugPrefab;

    [Tooltip("Number of mugs to spawn")]
    [SerializeField] private int mugCount = 2;

    [Header("Spawn Restrictions")]
    [Tooltip("Minimum X column to spawn mugs (0 = leftmost)")]
    [SerializeField] private int minColumn = 1;

    [Tooltip("Maximum X column to spawn mugs (exclusive)")]
    [SerializeField] private int maxColumn = 7;

    [Header("References")]
    [SerializeField] private GridGenerator gridGenerator;

    void Start()
    {
        // Find GridGenerator if not assigned
        if (gridGenerator == null)
        {
            gridGenerator = FindObjectOfType<GridGenerator>();
        }

        if (gridGenerator == null)
        {
            return;
        }

        if (mugPrefab == null)
        {
            return;
        }

        // Wait one frame for grid to be generated
        StartCoroutine(SpawnMugsDelayed());
    }

    private IEnumerator SpawnMugsDelayed()
    {
        // Wait for GridGenerator.Start() to complete
        yield return null;

        SpawnMugs();
    }

    private void SpawnMugs()
    {
        // Get all available tiles from grid
        List<GridTile> availableTiles = GetAvailableTiles();

        if (availableTiles.Count < mugCount)
        {
        }

        // Track used lanes to ensure mugs spawn on different lanes
        HashSet<int> usedLanes = new HashSet<int>();
        int spawnedCount = 0;

        // Spawn mugs on random tiles, one per lane
        for (int i = 0; i < mugCount && availableTiles.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availableTiles.Count);
            GridTile tile = availableTiles[randomIndex];
            availableTiles.RemoveAt(randomIndex);

            int lane = tile.GetGridY();

            // Skip if this lane already has a mug
            if (usedLanes.Contains(lane))
            {
                i--; // Retry with another tile
                continue;
            }

            usedLanes.Add(lane);
            SpawnMugOnTile(tile);
            spawnedCount++;

            // Remove all other tiles in this lane from available pool
            availableTiles.RemoveAll(t => t.GetGridY() == lane);
        }

    }

    private List<GridTile> GetAvailableTiles()
    {
        List<GridTile> tiles = new List<GridTile>();

        // Find all GridTile children of GridGenerator
        GridTile[] allTiles = gridGenerator.GetComponentsInChildren<GridTile>();

        foreach (GridTile tile in allTiles)
        {
            // Check column restrictions
            int x = tile.GetGridX();
            if (x >= minColumn && x < maxColumn && !tile.isOccupied)
            {
                tiles.Add(tile);
            }
        }

        return tiles;
    }

    private void SpawnMugOnTile(GridTile tile)
    {
        GameObject mugObject = Instantiate(mugPrefab, tile.transform.position, Quaternion.identity);
        mugObject.name = $"Mug_{tile.GetGridX()}_{tile.GetGridY()}";

        Mug mug = mugObject.GetComponent<Mug>();
        if (mug != null)
        {
            mug.Initialize(tile);
        }
        else
        {
        }
    }
}
