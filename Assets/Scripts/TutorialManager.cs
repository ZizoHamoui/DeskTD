using UnityEngine;

/// <summary>
/// Orchestrates the Level 1 tutorial flow through scripted narrative beats
/// that teach core mechanics: Spark Towers, Pencil Towers, erasers, and spark pickups.
/// Only active on the tutorial level — attach to a GameObject in the Level 1 scene.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    private enum TutorialState
    {
        WaitingToStart,
        WaitingForFirstGeneration,
        WaitingForSparks,
        WaitingForFirstKill,
        WaitingForWave2SecondKill,
        WaitingForPickupCollect,
        TutorialComplete
    }

    [Header("Settings")]
    [Tooltip("Spark count that triggers the first enemy wave")]
    [SerializeField] private int sparksToTriggerWave = 10;

    [Tooltip("Delay before showing the first narrative (seconds)")]
    [SerializeField] private float startDelay = 1f;

    private TutorialState state = TutorialState.WaitingToStart;
    private int wave2KillCount = 0;

    public bool IsTutorialComplete => state == TutorialState.TutorialComplete;

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
    }

    void Start()
    {
        Invoke(nameof(BeginTutorial), startDelay);
    }

    void OnDestroy()
    {
        // Defensive unsubscribes
        SparkTower.onCycleCompleted -= OnFirstSparkCycleCompleted;
        if (SparkManager.Instance != null)
            SparkManager.Instance.onSparksChanged -= OnSparksChanged;
        if (WaveManager.Instance != null)
            WaveManager.Instance.onWaveComplete -= OnWaveCompleted;
    }

    // === TUTORIAL FLOW ===

    /// <summary>
    /// Beat 1: Welcome message.
    /// </summary>
    private void BeginTutorial()
    {
        NarrativeEventUI.Show(
            "Hey there. This scroll holds the to a full digital world, and the Ink Blots want to destroy it. Let\u2019s make sure that doesn\u2019t happen.",
            OnWelcomeDismissed
        );
    }

    /// <summary>
    /// Beat 2: Teach Spark Tower placement.
    /// </summary>
    private void OnWelcomeDismissed()
    {
        NarrativeEventUI.Show(
            "Start by placing a Spark Tower on the grid, you\u2019ll need sparks to deploy your defenses.",
            OnSparkInstructionDismissed
        );
    }

    /// <summary>
    /// After player dismisses spark instruction, wait for first spark generation cycle.
    /// </summary>
    private void OnSparkInstructionDismissed()
    {
        state = TutorialState.WaitingForFirstGeneration;
        SparkTower.onCycleCompleted += OnFirstSparkCycleCompleted;
    }

    /// <summary>
    /// Beat 3 (NEW): First spark generation cycle completed — teach generator mechanic.
    /// </summary>
    private void OnFirstSparkCycleCompleted()
    {
        if (state != TutorialState.WaitingForFirstGeneration) return;
        SparkTower.onCycleCompleted -= OnFirstSparkCycleCompleted;

        NarrativeEventUI.Show(
            "When the generator is full, you get 5 sparks. These will be needed to deploy more towers. Lets wait for 10 to deploy a pencil",
            OnGeneratorNarrativeDismissed
        );
    }

    /// <summary>
    /// After generator narrative, monitor spark count for wave trigger.
    /// </summary>
    private void OnGeneratorNarrativeDismissed()
    {
        state = TutorialState.WaitingForSparks;

        if (SparkManager.Instance != null)
            SparkManager.Instance.onSparksChanged += OnSparksChanged;
    }

    /// <summary>
    /// When sparks reach threshold, show enemy warning and spawn wave 1.
    /// </summary>
    private void OnSparksChanged(int currentSparks)
    {
        if (state != TutorialState.WaitingForSparks) return;
        if (currentSparks < sparksToTriggerWave) return;

        // Unsubscribe
        if (SparkManager.Instance != null)
            SparkManager.Instance.onSparksChanged -= OnSparksChanged;

        state = TutorialState.WaitingForFirstKill;

        // Beat 4: Enemy incoming + teach Pencil Tower (no forceNextDrop)
        NarrativeEventUI.Show(
            "Ink Blots incoming! Place a Pencil Tower in the lane to shoot them down before they reach the scroll!",
            () =>
            {
                if (WaveManager.Instance != null)
                    WaveManager.Instance.StartWaveExternal(0);
            }
        );
    }

    /// <summary>
    /// Called by WaveManager on every enemy death during tutorial (before drop logic).
    /// Handles first-kill eraser narrative and wave 2 drop arming.
    /// </summary>
    public void OnTutorialEnemyKilled()
    {
        if (state == TutorialState.WaitingForFirstKill)
        {
            // First kill — show eraser narrative, transition to wave 2 tracking
            state = TutorialState.WaitingForWave2SecondKill;

            // Subscribe to wave completion to know when wave 1 ends
            if (WaveManager.Instance != null)
                WaveManager.Instance.onWaveComplete += OnWaveCompleted;

            NarrativeEventUI.Show(
                "Ink adds a shield to incoming enemies, Lets clean those tiles using the eraser before the next wave."
            );
        }
        else if (state == TutorialState.WaitingForWave2SecondKill)
        {
            wave2KillCount++;

            if (wave2KillCount == 1)
            {
                // After 1st wave-2 kill, arm the drop so the 2nd kill triggers it
                if (WaveManager.Instance != null)
                    WaveManager.Instance.forceNextDrop = true;
            }
            // The 2nd kill will trigger the drop via forceNextDrop,
            // and OnSparkPickupSpawned will handle the narrative
        }
    }

    /// <summary>
    /// When wave 1 completes, start wave 2 and reset kill counter.
    /// </summary>
    private void OnWaveCompleted(int waveIndex)
    {
        if (waveIndex == 0 && state == TutorialState.WaitingForWave2SecondKill)
        {
            wave2KillCount = 0;

            StartCoroutine(StartWave2AfterDelay());
        }
        else if (waveIndex == 2)
        {
            // Last wave complete — show closing narrative
            if (WaveManager.Instance != null)
                WaveManager.Instance.onWaveComplete -= OnWaveCompleted;

            NarrativeEventUI.Show(
                "Phew, close one. It only gets more challenging from here, sharpen your pencils!.",
                () =>
                {
                    if (WaveManager.Instance != null)
                        WaveManager.Instance.CompleteDeferredVictory();
                }
            );
        }
    }

    private System.Collections.IEnumerator StartWave2AfterDelay()
    {
        yield return new WaitForSeconds(5f);
        if (WaveManager.Instance != null)
            WaveManager.Instance.StartWaveExternal(1);
    }

    /// <summary>
    /// Called by WaveManager when an enemy reaches the end during the tutorial.
    /// Shows a forgiving narrative and restarts the current wave.
    /// </summary>
    public void OnTutorialEnemyReachedEnd()
    {
        NarrativeEventUI.Show(
            "Oh no, an Ink Blot got through! Let\u2019s restart this wave \u2014 you\u2019ve got this!",
            () =>
            {
                if (WaveManager.Instance != null)
                    WaveManager.Instance.RestartCurrentWave();
            }
        );
    }

    /// <summary>
    /// Called by WaveManager when a spark pickup is instantiated during tutorial.
    /// </summary>
    public void OnSparkPickupSpawned(GameObject pickupObj)
    {
        if (state != TutorialState.WaitingForWave2SecondKill) return;

        state = TutorialState.WaitingForPickupCollect;

        SparkPickup pickup = pickupObj.GetComponent<SparkPickup>();
        if (pickup != null)
        {
            // Keep pickup alive until player collects it
            pickup.MakePersistent();
            pickup.onCollected += OnPickupCollected;
        }

        // Beat: Teach spark pickup collection
        NarrativeEventUI.Show(
            "See that spark on the ground? Click on it to collect bonus sparks from fallen enemies."
        );
    }

    /// <summary>
    /// Player collected the pickup — tutorial complete.
    /// </summary>
    private void OnPickupCollected()
    {
        if (state != TutorialState.WaitingForPickupCollect) return;

        state = TutorialState.TutorialComplete;

        NarrativeEventUI.Show(
            "You\u2019re ready. More Ink Blots are on the way, defend the scroll!",
            () =>
            {
                if (WaveManager.Instance != null)
                    WaveManager.Instance.ResumeNormalFlow();
            }
        );
    }
}
