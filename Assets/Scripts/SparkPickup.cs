using UnityEngine;
using System.Collections;

/// <summary>
/// Clickable spark pickup that grants bonus sparks when clicked.
/// Auto-destroys after a set lifetime if not collected.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SparkPickup : MonoBehaviour
{
    [SerializeField] private int sparkAmount = 2;
    [SerializeField] private float lifetime = 5f;

    public System.Action onCollected;

    private Collider2D myCollider;
    private Coroutine autoDestroyCoroutine;

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        autoDestroyCoroutine = StartCoroutine(AutoDestroy());
    }

    private IEnumerator AutoDestroy()
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }

    /// <summary>
    /// Prevents this pickup from auto-destroying. Used by tutorial to keep pickup alive.
    /// </summary>
    public void MakePersistent()
    {
        if (autoDestroyCoroutine != null)
        {
            StopCoroutine(autoDestroyCoroutine);
            autoDestroyCoroutine = null;
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;
        if (!Input.GetMouseButtonDown(0)) return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D[] hits = Physics2D.OverlapPointAll(mousePos);
        foreach (Collider2D hit in hits)
        {
            if (hit == myCollider)
            {
                if (SparkManager.Instance != null)
                    SparkManager.Instance.AddSparks(sparkAmount);

                onCollected?.Invoke();
                Destroy(gameObject);
                return;
            }
        }
    }
}
