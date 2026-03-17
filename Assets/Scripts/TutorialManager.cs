using UnityEngine;

/// <summary>
/// Orchestrates the Level 1 tutorial flow through scripted narrative beats
/// that teach core mechanics: Spark Towers, Pencil Towers, and spark pickups.
/// Only active on the tutorial level — attach to a GameObject in the Level 1 scene.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    private enum TutorialState
    {
        WaitingToStart,
        WaitingForSparks,
        WaitingForFirstKill,
        WaitingForPickupCollect,
        TutorialComplete
    }

    [Header("Settings")]
    [Tooltip("Spark count that triggers the first enemy wave")]
    [SerializeField] private int sparksToTriggerWave = 10;

    [Tooltip("Delay before showing the first narrative (seconds)")]
    [SerializeField] private float startDelay = 1f;

    private TutorialState state = TutorialState.WaitingToStart;

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

        // Listen for the first ink trail so we can teach the player about it
        GridTile.onFirstInkCreated += OnFirstInkCreated;
    }

    void OnDestroy()
    {
        GridTile.onFirstInkCreated -= OnFirstInkCreated;
    }

    // === TUTORIAL FLOW ===

    /// <summary>
    /// Beat 1: Welcome message.
    /// </summary>
    private void BeginTutorial()
    {
        NarrativeEventUI.Show(
            "Welcome, boss. This scroll holds ancient secrets \u2014 and the Ink Blots want to destroy it. Let\u2019s make sure that doesn\u2019t happen.",
            OnWelcomeDismissed
        );
    }

    /// <summary>
    /// Beat 2: Teach Spark Tower placement.
    /// </summary>
    private void OnWelcomeDismissed()
    {
        NarrativeEventUI.Show(
            "Start by placing a Spark Tower on the grid \u2014 you\u2019ll need sparks to deploy your defenses.",
            OnSparkInstructionDismissed
        );
    }

    /// <summary>
    /// After player dismisses spark instruction, monitor spark count.
    /// </summary>
    private void OnSparkInstructionDismissed()
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

        // Force first kill to drop a spark pickup
        if (WaveManager.Instance != null)
            WaveManager.Instance.forceNextDrop = true;

        // Beat 3: Enemy incoming + teach Pencil Tower
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
    /// Fires when the first ink trail appears on a tile. Teaches player about ink shields and erasers.
    /// </summary>
    private void OnFirstInkCreated()
    {
        GridTile.onFirstInkCreated -= OnFirstInkCreated;

        NarrativeEventUI.Show(
            "Watch out \u2014 Ink Blots leave ink trails on the grid! Ink gives enemies extra shields and blocks tower placement. Use an Eraser to clean it up."
        );
    }

    /// <summary>
    /// Called by WaveManager when a spark pickup is instantiated during tutorial.
    /// </summary>
    public void OnSparkPickupSpawned(GameObject pickupObj)
    {
        if (state != TutorialState.WaitingForFirstKill) return;

        state = TutorialState.WaitingForPickupCollect;

        SparkPickup pickup = pickupObj.GetComponent<SparkPickup>();
        if (pickup != null)
        {
            // Keep pickup alive until player collects it
            pickup.MakePersistent();
            pickup.onCollected += OnPickupCollected;
        }

        // Beat 4: Teach spark pickup collection
        NarrativeEventUI.Show(
            "Nice shot! See that spark on the ground? Click on it to collect bonus sparks from fallen enemies."
        );
    }

    /// <summary>
    /// Beat 5: Player collected the pickup — tutorial complete.
    /// </summary>
    private void OnPickupCollected()
    {
        if (state != TutorialState.WaitingForPickupCollect) return;

        state = TutorialState.TutorialComplete;

        NarrativeEventUI.Show(
            "You\u2019ve got the basics down. More Ink Blots are on the way \u2014 defend the scroll!",
            () =>
            {
                if (WaveManager.Instance != null)
                    WaveManager.Instance.ResumeNormalFlow();
            }
        );
    }
}
