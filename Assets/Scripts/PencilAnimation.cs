using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Handles Pencil tower shoot animation.
/// Cycles through 7 sprites when the tower fires, releasing the projectile on sprite 4 (index 3).
/// </summary>
[RequireComponent(typeof(TowerAttack))]
public class PencilAnimation : MonoBehaviour
{
    [Header("Pencil Sprites")]
    [Tooltip("7 sprites for the shoot animation (projectile fires on sprite 4)")]
    [SerializeField] private Sprite[] sprites;

    [Tooltip("Duration of each animation frame (seconds)")]
    [SerializeField] private float frameDuration = 0.1f;

    private SpriteRenderer spriteRenderer;
    private bool isAnimating = false;
    private int cachedSortingOrder;

    public bool IsAnimating => isAnimating;

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

        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning($"[PencilAnimation] sprites array is empty on {gameObject.name}. Assign 7 sprites in Inspector.", this);
        }
        else
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) Debug.LogWarning($"[PencilAnimation] sprites[{i}] is NULL on {gameObject.name}.", this);
            }
        }
    }

    public void PlayShootAnimation(Action onFrame4)
    {
        if (sprites == null || sprites.Length != 7 || spriteRenderer == null) return;
        if (isAnimating) return;
        StartCoroutine(ShootAnimationCoroutine(onFrame4));
    }

    private IEnumerator ShootAnimationCoroutine(Action onFrame4)
    {
        isAnimating = true;

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
            {
                SetSprite(sprites[i]);
            }

            // Fire projectile on sprite 4 (index 3)
            if (i == 3)
            {
                onFrame4?.Invoke();
            }

            yield return new WaitForSeconds(frameDuration);
        }

        // Reset to idle sprite (first frame)
        if (sprites[0] != null)
        {
            SetSprite(sprites[0]);
        }

        isAnimating = false;
    }

    private void SetSprite(Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = cachedSortingOrder;
    }
}
