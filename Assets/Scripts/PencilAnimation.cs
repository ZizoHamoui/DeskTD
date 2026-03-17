using UnityEngine;
using System.Collections;

/// <summary>
/// Handles Pencil tower shoot animation.
/// Briefly flashes to a shoot sprite when the tower fires, then restores the default sprite.
/// </summary>
[RequireComponent(typeof(TowerAttack))]
public class PencilAnimation : MonoBehaviour
{
    [Header("Pencil Sprites")]
    [Tooltip("Default idle sprite for the Pencil tower")]
    [SerializeField] private Sprite defaultSprite;

    [Tooltip("Sprite shown when the Pencil fires a projectile")]
    [SerializeField] private Sprite shootSprite;

    [Tooltip("How long the shoot sprite is shown (seconds)")]
    [SerializeField] private float shootDuration = 0.15f;

    private SpriteRenderer spriteRenderer;
    private int cachedSortingOrder;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        if (spriteRenderer != null)
        {
            cachedSortingOrder = spriteRenderer.sortingOrder;
        }

        // Diagnostics
        if (defaultSprite == null) Debug.LogWarning($"[PencilAnimation] defaultSprite is NULL on {gameObject.name}. Assign it in Inspector.", this);
        if (shootSprite == null) Debug.LogWarning($"[PencilAnimation] shootSprite is NULL on {gameObject.name}. Assign it in Inspector.", this);
    }

    public void PlayShootFlash()
    {
        if (shootSprite == null || spriteRenderer == null) return;
        StartCoroutine(ShootFlashCoroutine());
    }

    private IEnumerator ShootFlashCoroutine()
    {
        SetSprite(shootSprite);
        yield return new WaitForSeconds(shootDuration);
        if (defaultSprite != null)
        {
            SetSprite(defaultSprite);
        }
    }

    private void SetSprite(Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = cachedSortingOrder;
    }
}
