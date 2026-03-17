using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Displays a floating quip above a tower when it kills the last enemy of a wave.
/// Singleton — add to an empty GameObject in the scene and assign the quipPrefab.
/// </summary>
public class KillQuipDisplay : MonoBehaviour
{
    public static KillQuipDisplay Instance { get; private set; }

    [Header("Quip Display Settings")]
    [Tooltip("World-space TextMeshPro prefab for floating quips")]
    [SerializeField] private GameObject quipPrefab;

    [Tooltip("Vertical offset above tower for quip text")]
    [SerializeField] private float yOffset = 1.2f;

    [Tooltip("How long the quip stays visible (seconds)")]
    [SerializeField] private float displayDuration = 7f;

    [Tooltip("How long the fade-out takes (seconds), included in displayDuration")]
    [SerializeField] private float fadeDuration = 1f;

    private static readonly Dictionary<TowerType, string[]> quips = new Dictionary<TowerType, string[]>
    {
        { TowerType.Pencil, new string[] {
            "Write that down!",
            "Period.",
            "That's the point!",
            "Drawn to conclusions.",
            "Sharp work!"
        }},
        { TowerType.Compass, new string[] {
            "Full circle!",
            "You've been rounded up!",
            "Stay in your arc!",
            "Get centered!",
            "That's how we roll!"
        }},
        { TowerType.StickyNote, new string[] {
            "Thou shall not pass!",
            "Is that all you've got?",
            "Barely a scratch!",
            "I'm stuck here all day!",
            "Read the fine print!"
        }},
        { TowerType.SparkTower, new string[] {
            "Sparked out!",
            "Electrifying finish!",
            "Lights out!",
            "Shocking, isn't it?",
            "Current events!"
        }}
    };

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Static convenience method called from WaveManager.
    /// </summary>
    public static void ShowQuip(Tower tower)
    {
        Debug.Log($"KillQuipDisplay.ShowQuip called. Instance={Instance != null}, Tower={tower != null}");
        if (Instance != null)
        {
            Instance.DisplayQuip(tower);
        }
        else
        {
            Debug.LogWarning("KillQuipDisplay: Instance is null! Is the GameObject in the scene?");
        }
    }

    /// <summary>
    /// Static convenience method for defensive towers (StickyNote) hit-triggered quips.
    /// </summary>
    public static void ShowDefensiveQuip(Tower tower)
    {
        if (Instance != null)
        {
            Instance.DisplayQuip(tower);
        }
    }

    private void DisplayQuip(Tower tower)
    {
        Debug.Log($"KillQuipDisplay.DisplayQuip called for tower: {tower.GetTowerType()}");

        if (quipPrefab == null)
        {
            Debug.LogError("KillQuipDisplay: quipPrefab not assigned!");
            return;
        }

        TowerType type = tower.GetTowerType();

        if (!quips.ContainsKey(type))
        {
            Debug.LogWarning($"KillQuipDisplay: No quips for tower type {type}");
            return;
        }

        string[] pool = quips[type];
        string quip = pool[Random.Range(0, pool.Length)];
        Debug.Log($"KillQuipDisplay: Showing quip \"{quip}\" above {type}");

        Vector3 spawnPos = tower.transform.position + Vector3.up * yOffset;
        spawnPos.z = -5f; // Ensure quip renders in front of all sprites
        GameObject quipObj = Instantiate(quipPrefab, spawnPos, Quaternion.identity);
        TextMeshPro tmp = quipObj.GetComponent<TextMeshPro>();

        if (tmp != null)
        {
            tmp.text = quip;
            tmp.sortingOrder = 100; // Render above everything
            Debug.Log($"KillQuipDisplay: Quip object spawned at {spawnPos}");
            StartCoroutine(FadeAndDestroy(tmp, quipObj));
        }
        else
        {
            Debug.LogError("KillQuipDisplay: quipPrefab missing TextMeshPro component!");
            Destroy(quipObj);
        }
    }

    private IEnumerator FadeAndDestroy(TextMeshPro tmp, GameObject quipObj)
    {
        float holdTime = Mathf.Max(0f, displayDuration - fadeDuration);
        yield return new WaitForSeconds(holdTime);

        Color startColor = tmp.color;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            tmp.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        Destroy(quipObj);
    }
}
