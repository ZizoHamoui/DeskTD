using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TowerDrag : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask tileLayerMask;
    public LayerMask towerLayerMask;

    [Header("State")]
    private Vector3 startPosition;
    private bool isDragging = false;
    private bool isLocked = false;
    private bool isShopTower = true;

    private Collider2D myCollider;
    private SpriteRenderer myRenderer;
    private int originalSortingOrder;
    private GridTile currentTile; // Tile this tower was placed on
    private List<GridTile> highlightedTiles = new List<GridTile>();

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        myRenderer = GetComponent<SpriteRenderer>();
        originalSortingOrder = myRenderer.sortingOrder;
    }

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        // 1. If we are locked (placed) or game is paused, do nothing
        if (isLocked) return;
        if (Time.timeScale == 0f) return;

        // 2. SHOP TOWERS: Spawn a copy when clicked
        if (isShopTower && !isDragging)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(mousePos, towerLayerMask);
            bool mouseOver = hit != null && hit.gameObject == this.gameObject;

            if (mouseOver && Input.GetMouseButtonDown(0))
            {
                SpawnAndDragCopy();
            }
        }

        // 3. COPIES: Handle Dragging Movement
        if (isDragging)
        {
            DragTower();

            // 4. Check for Mouse Up (release)
            if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
            }
        }
    }

    private void SpawnAndDragCopy()
    {
        GameObject copy = Instantiate(this.gameObject, transform.position, Quaternion.identity);

        // Get the TowerDrag component from the copy
        TowerDrag copyDrag = copy.GetComponent<TowerDrag>();

        // Mark it as a copy (not a shop tower)
        copyDrag.isShopTower = false;

        // Set the copy's return position to this shop tower's position
        copyDrag.startPosition = this.transform.position;

        // Start dragging the copy immediately
        copyDrag.StartDrag();
    }

    public void StartDrag()
    {
        isDragging = true;

        // Visual feedback: Pop to top layer
        myRenderer.sortingOrder = 99;

        myCollider.enabled = false;
    }

    private void DragTower()
    {
        // Convert screen mouse pos to world pos
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0; // Lock to 2D plane
        transform.position = mousePos;

        UpdateHighlights(mousePos);
    }

    private void UpdateHighlights(Vector3 mousePos)
    {
        ClearHighlights();

        Collider2D hitCollider = Physics2D.OverlapPoint((Vector2)mousePos, tileLayerMask);
        if (hitCollider == null) return;

        GridTile hoverTile = hitCollider.GetComponent<GridTile>();
        if (hoverTile == null) return;

        // Highlight the hovered tile at 80% white
        hoverTile.ShowHighlight(0.8f);
        highlightedTiles.Add(hoverTile);

        // Check tower type for range highlighting
        Tower towerComponent = GetComponent<Tower>();
        if (towerComponent == null || GridGenerator.Instance == null) return;

        TowerType type = towerComponent.GetTowerType();
        int gridX = hoverTile.GetGridX();
        int gridY = hoverTile.GetGridY();

        if (type == TowerType.Pencil)
        {
            // Highlight up to 3 tiles to the right, stop at mug
            int maxRange = Mathf.Min(gridX + 4, GridGenerator.Instance.Width);
            for (int x = gridX + 1; x < maxRange; x++)
            {
                GridTile tile = GridGenerator.Instance.GetTile(x, gridY);
                if (tile == null) continue;

                // Stop at mug
                if (tile.currentTower != null && tile.currentTower.GetComponent<Mug>() != null)
                    break;

                tile.ShowHighlight(0.5f);
                highlightedTiles.Add(tile);
            }
        }
        else if (type == TowerType.Compass)
        {
            // Highlight all tiles to the right in same row
            for (int x = gridX + 1; x < GridGenerator.Instance.Width; x++)
            {
                GridTile tile = GridGenerator.Instance.GetTile(x, gridY);
                if (tile == null) continue;

                tile.ShowHighlight(0.5f);
                highlightedTiles.Add(tile);
            }
        }
    }

    private void ClearHighlights()
    {
        for (int i = 0; i < highlightedTiles.Count; i++)
        {
            if (highlightedTiles[i] != null)
                highlightedTiles[i].HideHighlight();
        }
        highlightedTiles.Clear();
    }

    private void EndDrag()
    {
        isDragging = false;
        myRenderer.sortingOrder = originalSortingOrder;
        ClearHighlights();

        // Check drop target
        CheckForDrop();

        myCollider.enabled = true;
    }

    private void CheckForDrop()
    {
        // Raycast at mouse position to find a tile
        Vector2 mousePos2D = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hitCollider = Physics2D.OverlapPoint(mousePos2D, tileLayerMask);

        if (hitCollider != null)
        {
            GridTile tile = hitCollider.GetComponent<GridTile>();

            if (tile != null)
            {

                // Check ink tile placement restrictions
                Tower towerComponent = GetComponent<Tower>();
                bool isEraser = false;
                bool hasMug = tile.currentTower != null && tile.currentTower.GetComponent<Mug>() != null;

                if (towerComponent != null)
                {
                    isEraser = towerComponent.GetTowerType() == TowerType.Eraser;

                    if (tile.IsInk && !isEraser)
                    {
                        ErrorMessageUI.Show("Cannot place on ink tile");
                        ReturnToStart();
                        return;
                    }

                    if (!tile.IsInk && !hasMug && isEraser)
                    {
                        ErrorMessageUI.Show("Eraser only works on ink");
                        ReturnToStart();
                        return;
                    }

                    // Eraser on inked mug tile: clean ink without removing the mug
                    if (isEraser && hasMug && tile.IsInk)
                    {
                        int cost = towerComponent.GetSparkCost();
                        if (SparkManager.Instance != null && !SparkManager.Instance.TrySpendSparks(cost))
                        {
                            ErrorMessageUI.Show("Not enough sparks");
                            ReturnToStart();
                            return;
                        }
                        tile.CleanInk();
                        AudioManager.PlayEraser();
                        Destroy(gameObject);
                        return;
                    }

                    if (isEraser && hasMug && !tile.IsInk)
                    {
                        ErrorMessageUI.Show("Eraser only works on ink");
                        ReturnToStart();
                        return;
                    }
                }

                if (tile.TryOccupy(this.gameObject))
                {
                    // Check if we can afford this tower
                    if (towerComponent != null)
                    {
                        int cost = towerComponent.GetSparkCost();

                        if (SparkManager.Instance != null)
                        {
                            if (SparkManager.Instance.TrySpendSparks(cost))
                            {
                                // Success: Snap and Lock
                                transform.position = tile.transform.position;
                                currentTile = tile; // Store tile reference
                                LockTower();
                                return;
                            }
                            else
                            {
                                ErrorMessageUI.Show("Not enough sparks");
                                tile.ReleaseTile(); // We'll need to add this method to GridTile
                                ReturnToStart();
                                return;
                            }
                        }
                        else
                        {
                            transform.position = tile.transform.position;
                            currentTile = tile; // Store tile reference
                            LockTower();
                            return;
                        }
                    }
                    else
                    {
                        transform.position = tile.transform.position;
                        currentTile = tile; // Store tile reference
                        LockTower();
                        return;
                    }
                }
                else
                {
                    ErrorMessageUI.Show("Tile already occupied");
                }
            }
        }

        // Failure: Return to bench
        ReturnToStart();
    }

    private void LockTower()
    {
        isLocked = true;

        // Switch tower to "Tower" layer for enemy detection
        int towerLayer = LayerMask.NameToLayer("Tower");
        if (towerLayer != -1)
            gameObject.layer = towerLayer;

        // Notify the Tower component that it has been placed
        Tower towerComponent = GetComponent<Tower>();
        if (towerComponent != null)
        {
            // Set grid position from tile coordinates
            if (currentTile != null)
            {
                int gridX = currentTile.GetGridX();
                int gridY = currentTile.GetGridY();
                towerComponent.SetGridPosition(gridX, gridY);
                towerComponent.SetOccupiedTile(currentTile);
            }

            towerComponent.OnTowerPlaced();
        }

        AudioManager.PlayPlop();
    }

    private void ReturnToStart()
    {
        // Play error sound only for failed placement attempts (copies), not drag cancels
        if (!isShopTower)
        {
            AudioManager.PlayError();
        }

        if (isShopTower)
        {
            transform.position = startPosition;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
}