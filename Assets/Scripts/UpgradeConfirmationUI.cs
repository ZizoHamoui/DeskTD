using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Singleton confirmation overlay for tower upgrades.
/// Shows upgrade preview, description, cost, and confirm/cancel buttons.
/// Pauses the game while visible.
/// </summary>
public class UpgradeConfirmationUI : MonoBehaviour
{
    public static UpgradeConfirmationUI Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The overlay panel root")]
    [SerializeField] private GameObject overlayPanel;

    [Tooltip("Image component showing the upgraded tower preview")]
    [SerializeField] private Image upgradePreviewImage;

    [Tooltip("Text showing what the upgrade does")]
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Tooltip("Text showing the spark cost")]
    [SerializeField] private TextMeshProUGUI costText;

    [Tooltip("Confirm upgrade button")]
    [SerializeField] private Button confirmButton;

    [Tooltip("Cancel button")]
    [SerializeField] private Button cancelButton;

    [Header("Colors")]
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color unaffordableColor = Color.red;

    private TowerClickHandler pendingUpgrade;
    private bool isShowing = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (overlayPanel != null)
            overlayPanel.SetActive(false);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancel);
    }

    public void Show(TowerClickHandler handler)
    {
        if (handler == null || isShowing) return;

        pendingUpgrade = handler;

        // Populate UI
        if (upgradePreviewImage != null)
        {
            Sprite preview = handler.GetUpgradePreviewImage();
            if (preview != null)
            {
                upgradePreviewImage.sprite = preview;
                upgradePreviewImage.enabled = true;
            }
            else
            {
                upgradePreviewImage.enabled = false;
            }
        }

        if (descriptionText != null)
            descriptionText.text = handler.GetUpgradeDescription();

        int cost = handler.GetUpgradeCost();
        bool canAfford = SparkManager.Instance != null &&
                         SparkManager.Instance.GetCurrentSparks() >= cost;

        if (costText != null)
        {
            costText.text = $"Cost: {cost} Sparks";
            costText.color = canAfford ? affordableColor : unaffordableColor;
        }

        if (confirmButton != null)
            confirmButton.interactable = canAfford;

        if (overlayPanel != null)
            overlayPanel.SetActive(true);

        Time.timeScale = 0f;
        isShowing = true;
    }

    private void OnConfirm()
    {
        if (pendingUpgrade == null)
        {
            Close();
            return;
        }

        int cost = pendingUpgrade.GetUpgradeCost();
        if (SparkManager.Instance == null || !SparkManager.Instance.TrySpendSparks(cost))
        {
            ErrorMessageUI.Show("Not enough sparks to upgrade!");
            Close();
            return;
        }

        // Store reference before closing
        TowerClickHandler handler = pendingUpgrade;

        // Close overlay and unpause FIRST
        Close();

        // THEN apply upgrade (reveal after overlay closes)
        handler.ApplyUpgrade();
    }

    private void OnCancel()
    {
        Close();
    }

    private void Close()
    {
        if (overlayPanel != null)
            overlayPanel.SetActive(false);

        Time.timeScale = 1f;
        isShowing = false;
        pendingUpgrade = null;
    }

    public void ClearIfPending(TowerClickHandler handler)
    {
        if (pendingUpgrade == handler)
        {
            pendingUpgrade = null;
            if (isShowing) Close();
        }
    }
}
