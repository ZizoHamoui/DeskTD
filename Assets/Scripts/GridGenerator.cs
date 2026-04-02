using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    public static GridGenerator Instance { get; private set; }

    [Header("Grid Settings")]
    public int width = 8;
    public int height = 3;

    [Tooltip("Subtracts this small amount from the spacing to close gaps. Try 0.01 if needed.")]
    public float gapOverlap = 0.0f;

    [Header("Assets")]
    public GameObject tilePrefabLight;
    public GameObject tilePrefabDark;

    [Tooltip("Ink sprite variant for light tiles — ink_tile_light_orange.png")]
    public Sprite inkSpriteLight;

    [Tooltip("Ink sprite variant for dark tiles — ink_tile_dark_orange.png")]
    public Sprite inkSpriteDark;

    private float tileSize;
    private GridTile[,] tiles;

    public int Width => width;
    public int Height => height;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CalculateTileSize();
        GenerateGrid();
    }

    void CalculateTileSize()
    {
        SpriteRenderer sr = tilePrefabLight.GetComponent<SpriteRenderer>();

        if (sr != null && sr.sprite != null)
        {
            // PPU Math: The most precise way to determine size
            // We cast to float to ensure we don't lose decimals
            tileSize = (float)sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
        }
        else
        {
            tileSize = 1.0f;
        }
    }

    void GenerateGrid()
    {
        tiles = new GridTile[width, height];

        // Calculate the actual spacing distance (Size minus any desired overlap)
        float spacing = tileSize - gapOverlap;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float posX = x * spacing;
                float posY = y * spacing;

                Vector3 spawnPos = new Vector3(posX, posY, 0) + transform.position;

                bool isEven = (x + y) % 2 == 0;
                GameObject prefabToSpawn = isEven ? tilePrefabLight : tilePrefabDark;

                GameObject newTile = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

                newTile.transform.SetParent(this.transform);
                newTile.name = $"Tile_{x}_{y}";

                // Ensure GridTile component exists
                GridTile tileComponent = newTile.GetComponent<GridTile>();
                if (tileComponent == null)
                {
                    tileComponent = newTile.AddComponent<GridTile>();
                }

                // Set grid coordinates on the tile
                tileComponent.SetGridPosition(x, y);

                // Pass the correct ink sprite variant (light or dark) for future ink conversion
                tileComponent.InkSprite = isEven ? inkSpriteLight : inkSpriteDark;

                tiles[x, y] = tileComponent;
            }
        }
    }

    public float GetTileSpacing()
    {
        return tileSize - gapOverlap;
    }

    public GridTile GetTile(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return null;
        return tiles[x, y];
    }
}