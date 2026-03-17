using UnityEngine;

/// <summary>
/// Singleton that manages the spark currency economy.
/// Each Spark Tower generates sparks independently on its own timer.
/// </summary>
public class SparkManager : MonoBehaviour
{
    public static SparkManager Instance { get; private set; }

    [Header("Economy Settings")]
    [SerializeField] private int startingSparks = 5;

    [Header("Runtime State (Read-Only)")]
    [SerializeField] private int currentSparks;

    public event System.Action<int> onSparksChanged;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        currentSparks = startingSparks;
    }

    /// Adds sparks to the player's total.
    public void AddSparks(int amount)
    {
        currentSparks += amount;
        onSparksChanged?.Invoke(currentSparks);
    }

    public bool TrySpendSparks(int amount)
    {
        if (currentSparks >= amount)
        {
            currentSparks -= amount;
            onSparksChanged?.Invoke(currentSparks);
            return true;
        }
        else
        {
            return false;
        }
    }

    /// Returns the current spark count.
    public int GetCurrentSparks()
    {
        return currentSparks;
    }
}
