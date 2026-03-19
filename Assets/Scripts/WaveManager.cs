using UnityEngine;
using System.Collections;

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

    [Header("Tutorial")]
    [Tooltip("When true, waves do not auto-start and tutorial controls flow")]
    [SerializeField] private bool isTutorialLevel = false;

    [HideInInspector] public bool forceNextDrop = false;

    public event System.Action<int> onWaveComplete;

    [Header("Runtime State (Read-Only)")]
    [SerializeField] private GameState gameState = GameState.Idle;
    [SerializeField] private int currentWaveIndex = 0;
    [SerializeField] private int activeEnemyCount = 0;
    [SerializeField] private int totalEnemiesSpawned = 0;

    private bool deferredVictory = false;
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
    /// Starts the game after initial delay.
    /// </summary>
    private IEnumerator StartGameAfterDelay()
    {
        gameState = GameState.Idle;
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

        if (!isTutorialLevel && currentWaveIndex == 2)
            NarrativeEventUI.Show("Final wave — defend at all costs!");

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

            // Spawn enemy
            enemySpawner.SpawnEnemy(lane);
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
                // Tutorial level: defer victory until narrative popup is dismissed
                if (isTutorialLevel)
                    deferredVictory = true;
                else
                    TriggerVictory();
            }
            else
            {
                // During tutorial, don't auto-advance waves until tutorial is complete
                if (isTutorialLevel && TutorialManager.Instance != null && !TutorialManager.Instance.IsTutorialComplete)
                    return;

                if (!isTutorialLevel && currentWaveIndex == 2)
                    NarrativeEventUI.Show("They're regrouping... reinforce your defenses!");

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
