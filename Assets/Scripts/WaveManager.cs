using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Singleton manager that orchestrates wave spawning, game flow, and win/lose conditions.
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave Configuration")]
    [Tooltip("Array of wave configurations")]
    [SerializeField]
    private WaveConfig[] waves = new WaveConfig[]
    {
        new WaveConfig { enemyCount = 5, spawnInterval = 2f, randomizeLanes = true },
        new WaveConfig { enemyCount = 10, spawnInterval = 1.5f, randomizeLanes = true },
        new WaveConfig { enemyCount = 10, spawnInterval = 1.2f, randomizeLanes = true }
    };

    [Header("Spawning Settings")]
    [Tooltip("Delay before starting first wave (seconds)")]
    [SerializeField] private float initialDelay = 3f;

    [Tooltip("Delay between waves (seconds)")]
    [SerializeField] private float waveDelay = 5f;

    [Header("Spark Pickup")]
    [Tooltip("Prefab for the clickable spark pickup")]
    [SerializeField] private GameObject sparkPickupPrefab;
    [Tooltip("Minimum kills before a spark pickup spawns")]
    [SerializeField] private int minKillsForDrop = 3;
    [Tooltip("Maximum kills before a spark pickup spawns")]
    [SerializeField] private int maxKillsForDrop = 7;
    private int killsUntilNextDrop;

    [Header("Special Enemies")]
    [Tooltip("Prefab for the Freeze Enemy (spawns as last enemy of final wave)")]
    [SerializeField] private GameObject freezeEnemyPrefab;

    [Tooltip("Narrative message shown when the Freeze Enemy first appears")]
    [TextArea]
    [SerializeField] private string freezeEnemyNarrativeMessage;

    [Header("Tutorial")]
    [Tooltip("When true, waves do not auto-start and tutorial controls flow")]
    [SerializeField] private bool isTutorialLevel = false;

    [HideInInspector] public bool forceNextDrop = false;

    public event System.Action<int> onWaveComplete;
    public event System.Action<int> onWaveStarting;

    [Header("Runtime State (Read-Only)")]
    [SerializeField] private GameState gameState = GameState.Idle;
    [SerializeField] private int currentWaveIndex = 0;
    [SerializeField] private int activeEnemyCount = 0;
    [SerializeField] private int totalEnemiesSpawned = 0;

    private bool deferredVictory = false;
    private bool deferVictoryRequested = false;
    private bool autoStartPaused = false;
    private EnemySpawner enemySpawner;

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

        // Get or add EnemySpawner component
        enemySpawner = GetComponent<EnemySpawner>();
        if (enemySpawner == null)
        {
            enemySpawner = gameObject.AddComponent<EnemySpawner>();
        }
    }

    void Start()
    {

        if (enemySpawner == null)
        {
            return;
        }

        // Validate spawner configuration
        if (!enemySpawner.IsConfigured())
        {
            return;
        }

        killsUntilNextDrop = Random.Range(minKillsForDrop, maxKillsForDrop + 1);

        if (!isTutorialLevel)
            StartCoroutine(StartGameAfterDelay());
    }

    /// <summary>
    /// Prevents auto-start so a narrative can play first.
    /// Call before Start or during Awake/Start of another script.
    /// </summary>
    public void PauseAutoStart()
    {
        autoStartPaused = true;
    }

    /// <summary>
    /// Resumes auto-start after a narrative is dismissed.
    /// </summary>
    public void ResumeAutoStart()
    {
        autoStartPaused = false;
    }

    /// <summary>
    /// Starts the game after initial delay, waiting if auto-start is paused.
    /// </summary>
    private IEnumerator StartGameAfterDelay()
    {
        gameState = GameState.Idle;

        // Wait for any level-start narrative to be dismissed
        while (autoStartPaused)
            yield return null;

        yield return new WaitForSeconds(initialDelay);
        StartNextWave();
    }

    /// <summary>
    /// Starts the next wave in the sequence.
    /// </summary>
    private void StartNextWave()
    {
        if (currentWaveIndex >= waves.Length)
        {
            return;
        }

        onWaveStarting?.Invoke(currentWaveIndex);

        gameState = GameState.SpawningWave;
        WaveConfig wave = waves[currentWaveIndex];


        AudioManager.PlayRoar();

        StartCoroutine(SpawnWave(wave));
    }

    private IEnumerator SpawnWave(WaveConfig wave)
    {
        totalEnemiesSpawned = 0;
        int laneCount = enemySpawner.GetLaneCount();

        // For wave 1, track used lanes to prevent duplicates
        bool uniqueLanes = (currentWaveIndex == 0);
        bool[] usedLanes = new bool[laneCount];

        for (int i = 0; i < wave.enemyCount; i++)
        {
            // Get lane for this enemy
            int lane;
            if (wave.randomizeLanes)
            {
                if (uniqueLanes)
                {
                    // Pick a random lane that hasn't been used yet
                    int availableCount = 0;
                    for (int l = 0; l < laneCount; l++)
                        if (!usedLanes[l]) availableCount++;

                    // If all lanes used, stop spawning early
                    if (availableCount == 0) break;

                    int pick = Random.Range(0, availableCount);
                    lane = 0;
                    for (int l = 0; l < laneCount; l++)
                    {
                        if (!usedLanes[l])
                        {
                            if (pick == 0) { lane = l; break; }
                            pick--;
                        }
                    }
                    usedLanes[lane] = true;
                }
                else
                {
                    lane = Random.Range(0, laneCount);
                }
            }
            else
            {
                if (wave.lanes != null && wave.lanes.Length > 0)
                    lane = wave.lanes[i % wave.lanes.Length];
                else
                    lane = 0;
            }

            // Clamp lane to valid range in case scene data is out of bounds
            lane = Mathf.Clamp(lane, 0, Mathf.Max(0, laneCount - 1));

            // Check if this is the 3rd-quarter enemy of the last wave (Freeze Enemy slot)
            int freezeEnemyIndex = Mathf.RoundToInt(wave.enemyCount * 0.75f) - 1;
            bool isFreezeEnemySlot = freezeEnemyPrefab != null
                && currentWaveIndex == waves.Length - 1
                && i == freezeEnemyIndex;

            if (isFreezeEnemySlot)
            {
                // Override lane to one that already has an enemy
                lane = FindOccupiedLane(laneCount);

                enemySpawner.SpawnEnemy(lane, freezeEnemyPrefab);

                if (!string.IsNullOrEmpty(freezeEnemyNarrativeMessage))
                {
                    bool narrativeDismissed = false;
                    NarrativeEventUI.Show(freezeEnemyNarrativeMessage, () => { narrativeDismissed = true; });

                    // The coroutine will pause here until the player dismisses the narrative
                    yield return new WaitUntil(() => narrativeDismissed);
                }
            }
            else
            {
                enemySpawner.SpawnEnemy(lane);
            }

            totalEnemiesSpawned++;
            activeEnemyCount++;

            // Wait before spawning next enemy
            if (i < wave.enemyCount - 1)
            {
                yield return new WaitForSeconds(wave.spawnInterval);
            }
        }

        // All enemies spawned, wave is now in progress
        gameState = GameState.WaveInProgress;
    }

    /// <summary>
    /// Finds a lane that currently has at least one active enemy.
    /// Used to spawn the Freeze Enemy in an occupied lane.
    /// </summary>
    private int FindOccupiedLane(int laneCount)
    {
        Enemy[] activeEnemies = FindObjectsOfType<Enemy>();
        List<int> occupiedLanes = new List<int>();

        foreach (Enemy e in activeEnemies)
        {
            if (e.IsAlive())
            {
                int l = e.GetLane();
                if (!occupiedLanes.Contains(l))
                    occupiedLanes.Add(l);
            }
        }

        if (occupiedLanes.Count == 0)
            return Random.Range(0, laneCount);

        return occupiedLanes[Random.Range(0, occupiedLanes.Count)];
    }

    public void OnEnemyDeath(Tower killingTower = null, Vector3 deathPosition = default)
    {
        activeEnemyCount--;

        // Notify tutorial before drop logic so it can set forceNextDrop in time
        if (isTutorialLevel && TutorialManager.Instance != null)
            TutorialManager.Instance.OnTutorialEnemyKilled();

        bool shouldDrop = false;
        if (forceNextDrop)
        {
            shouldDrop = true;
            forceNextDrop = false;
        }
        else
        {
            killsUntilNextDrop--;
            if (killsUntilNextDrop <= 0)
            {
                shouldDrop = true;
                killsUntilNextDrop = Random.Range(minKillsForDrop, maxKillsForDrop + 1);
            }
        }

        if (shouldDrop && sparkPickupPrefab != null)
        {
            GameObject pickup = Instantiate(sparkPickupPrefab, deathPosition, Quaternion.identity);
            if (isTutorialLevel && TutorialManager.Instance != null)
                TutorialManager.Instance.OnSparkPickupSpawned(pickup);
        }

        CheckWaveComplete(killingTower);
    }

    public void OnEnemyReachedEnd()
    {
        activeEnemyCount--;

        // In tutorial mode on waves 1 and 2, hand off to TutorialManager — no scroll damage
        if (isTutorialLevel && TutorialManager.Instance != null && currentWaveIndex < 2)
        {
            TutorialManager.Instance.OnTutorialEnemyReachedEnd();
            return;
        }

        // Damage the scroll health
        bool isGameOver = false;
        if (ScrollHealth.Instance != null)
        {
            isGameOver = ScrollHealth.Instance.TakeDamage();
        }
        else
        {
            // No scroll health system - instant game over (fallback)
            isGameOver = true;
        }

        if (isGameOver)
        {
            gameState = GameState.GameOver;


            // Stop all coroutines to prevent further spawning
            StopAllCoroutines();

            // Show game over overlay
            if (GameOverlay.Instance != null)
            {
                GameOverlay.Instance.ShowGameOver();
            }
        }
        else
        {
            // Check if wave is complete (enemy died but scroll survived)
            CheckWaveComplete();
        }
    }

    /// <summary>
    /// Destroys all active enemies and restarts the current wave from scratch.
    /// Used by TutorialManager when an enemy slips through.
    /// </summary>
    public void RestartCurrentWave()
    {
        StopAllCoroutines();

        // Destroy all enemies still in the scene
        foreach (Enemy e in FindObjectsOfType<Enemy>())
            Destroy(e.gameObject);

        // Clean ink from all tiles
        foreach (GridTile tile in FindObjectsOfType<GridTile>())
            tile.CleanInk();

        activeEnemyCount = 0;
        totalEnemiesSpawned = 0;

        // Re-run the same wave index
        StartNextWave();
    }

    private void CheckWaveComplete(Tower killingTower = null)
    {
        Debug.Log($"CheckWaveComplete: state={gameState}, activeEnemies={activeEnemyCount}, killingTower={killingTower != null}");

        // Only check if we're in WaveInProgress state (all enemies spawned)
        if (gameState != GameState.WaveInProgress) return;

        // Wave complete if all enemies are dead
        if (activeEnemyCount <= 0)
        {
            gameState = GameState.WaveComplete;

            // Show quip on the tower that killed the last enemy
            if (killingTower != null)
            {
                KillQuipDisplay.ShowQuip(killingTower);
            }
            else
            {
                Debug.LogWarning("CheckWaveComplete: killingTower is null, no quip displayed");
            }

            currentWaveIndex++;
            onWaveComplete?.Invoke(currentWaveIndex - 1);

            // Check if more waves exist
            if (currentWaveIndex >= waves.Length)
            {
                // Defer victory when a narrative needs to play first
                if (isTutorialLevel || deferVictoryRequested)
                {
                    deferredVictory = true;
                    deferVictoryRequested = false;
                }
                else
                {
                    TriggerVictory();
                }
            }
            else
            {
                // During tutorial, don't auto-advance waves until tutorial is complete
                if (isTutorialLevel && TutorialManager.Instance != null && !TutorialManager.Instance.IsTutorialComplete)
                    return;

                // Start next wave after delay
                StartCoroutine(StartNextWaveAfterDelay());
            }
        }
    }

    private IEnumerator StartNextWaveAfterDelay()
    {
        yield return new WaitForSeconds(waveDelay);
        StartNextWave();
    }

    private void TriggerVictory()
    {
        gameState = GameState.Victory;


        // Show victory overlay
        if (GameOverlay.Instance != null)
        {
            GameOverlay.Instance.ShowVictory();
        }
    }

    /// <summary>
    /// Request that victory be deferred until CompleteDeferredVictory is called.
    /// Must be called before the last wave completes (e.g. in Start).
    /// </summary>
    public void RequestDeferVictory()
    {
        deferVictoryRequested = true;
    }

    /// <summary>
    /// Called after the final narrative is dismissed to show the victory screen.
    /// </summary>
    public void CompleteDeferredVictory()
    {
        if (deferredVictory)
        {
            deferredVictory = false;
            TriggerVictory();
        }
    }

    /// <summary>
    /// Externally trigger a specific wave (used by TutorialManager).
    /// </summary>
    public void StartWaveExternal(int waveIndex)
    {
        currentWaveIndex = waveIndex;
        StartNextWave();
    }

    /// <summary>
    /// Resume normal wave flow after tutorial completes.
    /// </summary>
    public void ResumeNormalFlow()
    {
        StartCoroutine(StartNextWaveAfterDelay());
    }

    public GameState GetGameState()
    {
        return gameState;
    }

    public int GetCurrentWaveIndex()
    {
        return currentWaveIndex;
    }

    public int GetActiveEnemyCount()
    {
        return activeEnemyCount;
    }

    public int GetTotalWaveCount()
    {
        return waves.Length;
    }
}

public enum GameState
{
    Idle,               // Waiting to start first wave
    SpawningWave,       // Currently spawning enemies
    WaveInProgress,     // All enemies spawned, wave ongoing
    WaveComplete,       // Wave finished, preparing next wave
    Victory,            // All waves complete
    GameOver            // Enemy reached end
}

[System.Serializable]
public class WaveConfig
{
    [Tooltip("Number of enemies in this wave")]
    public int enemyCount;

    [Tooltip("Time between each enemy spawn (seconds)")]
    public float spawnInterval;

    [Tooltip("Lane for each enemy (0=bottom, 1=middle, 2=top). Ignored if randomizeLanes is true.")]
    public int[] lanes;

    [Tooltip("If true, lanes are randomly assigned instead of using the lanes array")]
    public bool randomizeLanes = false;
}
