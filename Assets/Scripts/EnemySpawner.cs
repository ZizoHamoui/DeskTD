using UnityEngine;
using System.Linq;

/// <summary>
/// Handles spawning of enemies at specific lanes and positions.
/// Works in conjunction with WaveManager to spawn enemies based on wave configuration.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning Configuration")]
    [Tooltip("Enemy prefab to spawn")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("X position where enemies spawn (right edge of grid). Auto-calculated if 0.")]
    [SerializeField] private float spawnXPosition = 0f;

    [Tooltip("Spacing between lanes (should match GridGenerator spacing). Auto-calculated if 0.")]
    [SerializeField] private float laneSpacing = 0f;

    [Tooltip("Y offset for lane 0 (bottom lane). Auto-calculated if not set.")]
    [SerializeField] private float laneYOffset = 0f;

    [Header("Auto-Configuration")]
    [Tooltip("Reference to GridGenerator to auto-calculate spawn positions")]
    [SerializeField] private GridGenerator gridGenerator;

    [Header("Debug")]
    [Tooltip("Show spawn position gizmos in editor")]
    [SerializeField] private bool showSpawnGizmos = true;

    private int laneCount = 3;

    void Start()
    {
        AutoConfigureFromGrid();
    }

    private void AutoConfigureFromGrid()
    {
        // Try to find GridGenerator if not assigned
        if (gridGenerator == null)
        {
            gridGenerator = FindObjectOfType<GridGenerator>();
        }

        if (gridGenerator != null)
        {
            // Get grid dimensions
            int gridWidth = gridGenerator.width;
            int gridHeight = gridGenerator.height;

            // Set lane count from grid height
            laneCount = gridHeight;

            // Calculate tile size from grid
            GameObject lightTile = gridGenerator.tilePrefabLight;
            if (lightTile != null)
            {
                SpriteRenderer sr = lightTile.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    float tileSize = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
                    float spacing = tileSize - gridGenerator.gapOverlap;

                    // Always auto-calculate from grid to avoid stale serialized values
                    laneSpacing = spacing;
                    spawnXPosition = (gridWidth * spacing) + gridGenerator.transform.position.x + 0.5f;
                    laneYOffset = gridGenerator.transform.position.y;
                }
            }

            string lanePositions = string.Join(", ", System.Linq.Enumerable.Range(0, laneCount).Select(i => (laneYOffset + (i * laneSpacing)).ToString("F2")));
        }
        else
        {
        }
    }

    public void SpawnEnemy(int lane)
    {

        if (enemyPrefab == null)
        {
            return;
        }

        // Calculate spawn position
        Vector3 spawnPosition = GetSpawnPosition(lane);

        // Instantiate enemy
        GameObject enemyObject = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        if (enemyObject == null)
        {
            return;
        }

        enemyObject.name = $"Enemy_Lane{lane}_{Time.time}";

        // Configure enemy
        Enemy enemy = enemyObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.SetLane(lane);
        }
        else
        {
        }

    }


    private Vector3 GetSpawnPosition(int lane)
    {
        float yPosition = laneYOffset + (lane * laneSpacing);
        return new Vector3(spawnXPosition, yPosition, 0f);
    }

    public bool IsConfigured()
    {
        if (enemyPrefab == null)
        {
            return false;
        }

        Enemy enemyComponent = enemyPrefab.GetComponent<Enemy>();
        if (enemyComponent == null)
        {
            return false;
        }

        return true;
    }

    [ContextMenu("Auto-Calculate Lane Spacing from Grid")]
    public void AutoCalculateLaneSpacing()
    {
        GridGenerator gridGenerator = FindObjectOfType<GridGenerator>();
        if (gridGenerator != null)
        {

            laneSpacing = 1f;

        }
        else
        {
        }
    }

    public int GetLaneCount()
    {
        return laneCount;
    }

    void OnDrawGizmos()
    {
        if (!showSpawnGizmos) return;

        // Draw spawn positions for all lanes
        int gizmoLaneCount = laneCount > 0 ? laneCount : 3; // Fallback to 3 for editor preview
        for (int lane = 0; lane < gizmoLaneCount; lane++)
        {
            Vector3 spawnPos = GetSpawnPosition(lane);

            // Draw sphere at spawn point
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f); // Orange
            Gizmos.DrawSphere(spawnPos, 0.3f);

            // Draw arrow showing movement direction (left)
            Gizmos.color = Color.red;
            Gizmos.DrawLine(spawnPos, spawnPos + Vector3.left * 2f);

            // Draw lane label
#if UNITY_EDITOR
            UnityEditor.Handles.Label(spawnPos + Vector3.up * 0.5f, $"Lane {lane}");
#endif
        }

        // Draw spawn X line
        Gizmos.color = Color.yellow;
        float lineTop = laneYOffset + ((gizmoLaneCount - 1) * laneSpacing) + 1f;
        Gizmos.DrawLine(
            new Vector3(spawnXPosition, laneYOffset - 1f, 0f),
            new Vector3(spawnXPosition, lineTop, 0f)
        );
    }
}
