using UnityEngine;

/// <summary>
/// Handles clicking on placed towers to show upgrade arrow and trigger upgrades.
/// Add to Pencil and StickyNote tower prefabs.
/// </summary>
[RequireComponent(typeof(Tower))]
public class TowerClickHandler : MonoBehaviour
{
    [Header("Upgrade Arrow")]
    [Tooltip("Disabled child GameObject with the upgrade arrow sprite")]
    [SerializeField] private GameObject upgradeArrowButton;

    [Header("Upgrade Settings")]
    [Tooltip("Spark cost to upgrade this tower")]
    [SerializeField] private int upgradeCost = 25;

    [Tooltip("Preview image shown in the confirmation overlay")]
    [SerializeField] private Sprite upgradePreviewImage;

    [Tooltip("Description shown in the confirmation overlay")]
    [SerializeField] private string upgradeDescription = "";

    [Tooltip("New sprites for the upgraded tower (7 for Pencil animation, 6 for StickyNote damage states)")]
    [SerializeField] private Sprite[] upgradedSprites;

    private Tower tower;
    private bool isUpgraded = false;
    private bool arrowShowing = false;
    private Collider2D arrowCollider;
    private Collider2D towerCollider;

    private static TowerClickHandler activeHandler;

    void Awake()
    {
        tower = GetComponent<Tower>();
        towerCollider = GetComponent<Collider2D>();

        if (upgradeArrowButton != null)
        {
            arrowCollider = upgradeArrowButton.GetComponent<Collider2D>();
            upgradeArrowButton.SetActive(false);
        }
    }

    void OnMouseDown()
    {
        if (isUpgraded) return;
        if (!tower.IsPlaced()) return;
        if (Time.timeScale == 0f) return;

        // Hide previous arrow if another tower had one
        if (activeHandler != null && activeHandler != this)
        {
            activeHandler.HideArrow();
        }

        ShowArrow();
    }

    void Update()
    {
        if (!arrowShowing) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // Check if click hit the arrow
            if (arrowCollider != null && arrowCollider.OverlapPoint(mouseWorldPos))
            {
                OnUpgradeArrowClicked();
                return;
            }

            // Check if click hit this tower
            if (towerCollider != null && towerCollider.OverlapPoint(mouseWorldPos))
            {
                return; // OnMouseDown handles toggle
            }

            // Click was elsewhere — dismiss arrow
            HideArrow();
        }
    }

    private void ShowArrow()
    {
        if (upgradeArrowButton == null) return;
        upgradeArrowButton.SetActive(true);
        arrowShowing = true;
        activeHandler = this;
    }

    public void HideArrow()
    {
        if (upgradeArrowButton == null) return;
        upgradeArrowButton.SetActive(false);
        arrowShowing = false;
        if (activeHandler == this) activeHandler = null;
    }

    private void OnUpgradeArrowClicked()
    {
        HideArrow();
        if (UpgradeConfirmationUI.Instance != null)
        {
            UpgradeConfirmationUI.Instance.Show(this);
        }
    }

    public void ApplyUpgrade()
    {
        isUpgraded = true;

        TowerType type = tower.GetTowerType();

        if (type == TowerType.Pencil)
        {
            TowerAttack attack = GetComponent<TowerAttack>();
            if (attack != null) attack.SetUpgraded(true);

            PencilAnimation anim = GetComponent<PencilAnimation>();
            if (anim != null) anim.SetUpgradedSprites(upgradedSprites);
        }
        else if (type == TowerType.StickyNote)
        {
            TowerHealth health = GetComponent<TowerHealth>();
            if (health != null) health.ApplyUpgrade(6f);

            StickyNoteAnimation anim = GetComponent<StickyNoteAnimation>();
            if (anim != null) anim.SetUpgradedSprites(upgradedSprites);
        }
    }

    public bool IsUpgraded => isUpgraded;
    public int GetUpgradeCost() => upgradeCost;
    public Sprite GetUpgradePreviewImage() => upgradePreviewImage;
    public string GetUpgradeDescription() => upgradeDescription;

    void OnDestroy()
    {
        if (activeHandler == this) activeHandler = null;

        if (UpgradeConfirmationUI.Instance != null)
        {
            UpgradeConfirmationUI.Instance.ClearIfPending(this);
        }
    }
}
