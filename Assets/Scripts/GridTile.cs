using UnityEngine;

public class GridTile : MonoBehaviour
{
    [Header("Grid Coordinates")]
    public int gridX = -1;
    public int gridY = -1;

    [Header("State")]
    // Is there a tower on this tile?
    public bool isOccupied = false;

    // Optional: Reference to the tower object sitting here (for debugging)
    public GameObject currentTower;

    [Header("Ink State")]
    public bool IsInk = false;
    public InkShield CreatorEnemy = null;
    public Sprite InkSprite = null;

    [Header("Ink Immunity")]
    [Tooltip("Duration in seconds that a cleaned tile is immune to ink")]
    [SerializeField] private float inkImmuneDuration = 4f;
    private bool inkImmune = false;
    private float inkImmuneTimer = 0f;

    // Store original sprite for restoration when ink is cleaned
    private Sprite originalSprite;

    // Highlight overlay
    private GameObject highlightOverlay;
    private static Sprite sharedWhiteSprite;

    private static Sprite GetWhiteSprite()
    {
        if (sharedWhiteSprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            sharedWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
        return sharedWhiteSprite;
    }

    void Awake()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            originalSprite = sr.sprite;
        }
    }

    void Update()
    {
        if (inkImmune)
        {
            inkImmuneTimer -= Time.deltaTime;
            if (inkImmuneTimer <= 0f)
            {
                inkImmune = false;
                inkImmuneTimer = 0f;
            }
        }
    }

    public bool TryOccupy(GameObject tower)
    {
        if (isOccupied)
        {
            return false;
        }

        // Occupy the tile
        isOccupied = true;
        currentTower = tower;

        return true;
    }

    public void ReleaseTile()
    {
        isOccupied = false;
        currentTower = null;
    }

    public void SetGridPosition(int x, int y)
    {
        gridX = x;
        gridY = y;
    }

    public static event System.Action onFirstInkCreated;
    private static bool firstInkFired = false;

    public void ConvertToInk(InkShield creator)
    {
        if (IsInk) return;
        if (inkImmune) return;

        IsInk = true;
        CreatorEnemy = creator;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && InkSprite != null)
        {
            sr.sprite = InkSprite;
        }

        if (!firstInkFired)
        {
            firstInkFired = true;
            onFirstInkCreated?.Invoke();
        }
    }

    public void CleanInk()
    {
        if (!IsInk) return;

        IsInk = false;
        CreatorEnemy = null;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && originalSprite != null)
        {
            sr.sprite = originalSprite;
        }

        ActivateInkImmunity();

    }

    public void ActivateInkImmunity()
    {
        inkImmune = true;
        inkImmuneTimer = inkImmuneDuration;
    }

    /// Returns the grid X coordinate.
    public int GetGridX()
    {
        return gridX;
    }

    /// Returns the grid Y coordinate (lane).
    public int GetGridY()
    {
        return gridY;
    }

    public void ShowHighlight(float alpha)
    {
        if (highlightOverlay == null)
        {
            highlightOverlay = new GameObject("Highlight");
            highlightOverlay.transform.SetParent(transform);
            highlightOverlay.transform.localPosition = new Vector3(0, 0, -0.01f);

            SpriteRenderer overlaySR = highlightOverlay.AddComponent<SpriteRenderer>();
            overlaySR.sprite = GetWhiteSprite();

            SpriteRenderer tileSR = GetComponent<SpriteRenderer>();
            Vector3 tileSize = tileSR.bounds.size;
            highlightOverlay.transform.localScale = new Vector3(tileSize.x, tileSize.y, 1f);

            overlaySR.sortingLayerName = tileSR.sortingLayerName;
            overlaySR.sortingOrder = tileSR.sortingOrder + 1;
        }

        highlightOverlay.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, alpha);
        highlightOverlay.SetActive(true);
    }

    public void HideHighlight()
    {
        if (highlightOverlay != null)
            highlightOverlay.SetActive(false);
    }
}