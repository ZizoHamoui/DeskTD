using UnityEngine;

/// <summary>
/// Component for Spark Towers that generate spark currency.
/// Each tower independently generates 5 sparks every 10 seconds.
/// </summary>
[RequireComponent(typeof(Tower))]
public class SparkTower : MonoBehaviour
{
    [Header("Generation Settings")]
    [Tooltip("Sparks generated per cycle")]
    [SerializeField] private int sparksPerCycle = 5;

    [Tooltip("Time in seconds for a full generation cycle")]
    [SerializeField] private float cycleInterval = 3.5f;

    public static event System.Action onCycleCompleted;

    [HideInInspector] public bool isFrozenDisabled = false;

    private bool isPlaced = false;
    private float cycleTimer = 0f;

    /// <summary>
    /// Returns how far through the current cycle we are (0 to cycleInterval).
    /// Used by SparkTowerAnimation to drive sprite changes.
    /// </summary>
    public float CycleTimer => cycleTimer;

    /// <summary>
    /// Returns the full cycle interval in seconds.
    /// </summary>
    public float CycleInterval => cycleInterval;

    public void OnPlaced()
    {
        if (isPlaced) return;
        isPlaced = true;
        cycleTimer = 0f;
    }

    void Update()
    {
        if (!isPlaced) return;
        if (isFrozenDisabled) return;

        cycleTimer += Time.deltaTime;

        if (cycleTimer >= cycleInterval)
        {
            if (SparkManager.Instance != null)
            {
                SparkManager.Instance.AddSparks(sparksPerCycle);
                onCycleCompleted?.Invoke();
            }
            cycleTimer = 0f;
        }
    }
}
