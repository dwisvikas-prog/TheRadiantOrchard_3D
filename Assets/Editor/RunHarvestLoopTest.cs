#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using RadiantOrchard;

// One-click "play through the harvest loop" harness.
//
// Enters Play mode, waits for the first fruit to actually exist, then runs
// GestureTestRunner's pass — which fires real GestureManager events at each
// fruit's screen position, so the genuine pipeline runs end to end:
//   FruitHarvester -> TonicFlight -> StickmanController -> GameState -> QuizManager
// and each heal is asserted on all three things the player sees: the Stickman
// turns colored, the Vibrancy Meter receives the gain, and the fruit is consumed.
//
// Two reasons this can't just be "press Play and walk to a fruit":
//   • NPCSpawner only spawns on a timer (spawnInterval, ~12-15s at level 1), so a
//     pass started immediately after Play finds zero fruits and tests nothing —
//     hence the wait-for-a-fruit step below.
//   • The runner is created in Play mode on purpose: nothing is added to the
//     saved scene, and it disappears when Play ends.
//
// Results land in the Console, tagged [TestPass] (PASS/FAIL per fruit) plus a
// summary line. Editor-only; the runtime side lives in
// Scripts/Debug/GestureTestRunner.cs.
public static class RunHarvestLoopTest
{
    private const string RunnerName = "HarvestLoopTestRunner";
    private const double FruitWaitTimeoutSeconds = 60.0;
    private const double CheckIntervalSeconds = 0.25;

    private static double waitStartedAt = -1.0;
    private static double nextCheckAt;

    [MenuItem("Tools/Radiant Orchard/Run Harvest Loop Test Pass")]
    public static void RunHarvestLoopTestPass()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[HarvestLoop] Already playing. Run it from the HarvestLoopTestRunner component's " +
                             "\"Run Test Pass\" context menu, or exit Play mode first.");
            return;
        }

        waitStartedAt = -1.0;
        nextCheckAt = 0.0;

        EditorApplication.update -= Step;
        EditorApplication.update += Step;
        EditorApplication.EnterPlaymode();

        Debug.Log("[HarvestLoop] Entering Play mode; waiting for the first fruit to spawn (this is the real " +
                  "spawn timer, so it takes ~12-15s)...");
    }

    private static void Step()
    {
        // Not in Play mode yet — either still transitioning, or the user
        // cancelled before it started.
        if (!EditorApplication.isPlaying)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Stop("Play mode was never entered (cancelled?).");
            return;
        }

        if (waitStartedAt < 0.0) waitStartedAt = EditorApplication.timeSinceStartup;
        if (EditorApplication.timeSinceStartup < nextCheckAt) return;
        nextCheckAt = EditorApplication.timeSinceStartup + CheckIntervalSeconds;

        int fruits = Object.FindObjectsByType<FruitHarvester>(FindObjectsSortMode.None).Length;
        if (fruits == 0)
        {
            if (EditorApplication.timeSinceStartup - waitStartedAt > FruitWaitTimeoutSeconds)
                Stop($"no fruit spawned within {FruitWaitTimeoutSeconds:F0}s. Check the console for NPCSpawner warnings — " +
                     "unwired spawn points or an empty fruit pool mean nothing will ever spawn.");
            return;
        }

        var runner = Object.FindFirstObjectByType<GestureTestRunner>();
        if (runner == null)
        {
            var go = new GameObject(RunnerName);
            runner = go.AddComponent<GestureTestRunner>();
        }

        Stop(null);
        Debug.Log($"[HarvestLoop] {fruits} fruit(s) live — driving the test pass now.");
        runner.RunFromMenu();
    }

    private static void Stop(string problem)
    {
        EditorApplication.update -= Step;
        if (problem != null) Debug.LogError("[HarvestLoop] " + problem);
    }
}
#endif
