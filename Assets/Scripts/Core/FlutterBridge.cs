using UnityEngine;

namespace RadiantOrchard
{
    // The Unity <-> Flutter contract, decided and half-wired here because
    // nobody had picked one yet (Issue Register: Flutter bridge doesn't
    // exist). This does NOT make a working Flutter app exist — there is no
    // Flutter project anywhere near this Unity project, and building one is
    // its own separate task. What this DOES do: decide who owns what, and
    // build the Unity-side half of the seam so a Flutter host has something
    // concrete to call into and listen to once it exists.
    //
    // THE DECISION:
    //  - Flutter owns: the app shell, level-select menu, settings, and any
    //    IAP/store screens. Unity is embedded as a single "island view".
    //  - Unity owns: the island scene, all gameplay, and local save data
    //    (SaveSystem/GameState already do this — unchanged by this bridge).
    //  - Flutter -> Unity: via Unity's own UnitySendMessage("FlutterBridge",
    //    methodName, stringArg) — this is Unity's real, documented mechanism
    //    for a native host to call into an embedded Unity player, independent
    //    of whichever Flutter-Unity plugin ends up used. The public methods
    //    below (StartLevel, SetPaused, ResetProgress) are the entry points.
    //  - Unity -> Flutter: via SendToFlutter(eventName, jsonPayload) below.
    //    That method is a placeholder that logs instead of actually crossing
    //    a bridge — there is nothing on the other side yet. Replace its body
    //    with the chosen plugin's send call (e.g. flutter_unity_widget's
    //    UnityMessageManager.instance.SendMessageToFlutter(...)) and nothing
    //    else in this file needs to change, since every outbound event
    //    already funnels through this one method.
    public class FlutterBridge : MonoBehaviour
    {
        public static FlutterBridge Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            if (GameState.Instance == null) return;
            GameState.Instance.LevelCompleted += OnLevelCompleted;
            GameState.Instance.VibrancyChanged += OnVibrancyChanged;
        }

        private void OnDisable()
        {
            if (GameState.Instance == null) return;
            GameState.Instance.LevelCompleted -= OnLevelCompleted;
            GameState.Instance.VibrancyChanged -= OnVibrancyChanged;
        }

        // --- Flutter -> Unity ------------------------------------------------
        // Call via UnitySendMessage("FlutterBridge", "<MethodName>", arg) from
        // the native/Dart side. GameObject in the scene must be named
        // "FlutterBridge" to match — see AttachToScene() below.

        public void StartLevel(string levelId)
        {
            if (GameState.Instance != null) GameState.Instance.StartLevel(levelId);
        }

        public void SetPaused(string trueOrFalse)
        {
            Time.timeScale = trueOrFalse == "true" ? 0f : 1f;
        }

        public void ResetProgress()
        {
            if (GameState.Instance != null) GameState.Instance.ResetProgress();
        }

        // --- Unity -> Flutter ------------------------------------------------

        private void OnLevelCompleted(string levelId) =>
            SendToFlutter("levelCompleted", "{\"levelId\":\"" + Escape(levelId) + "\"}");

        private void OnVibrancyChanged(int current, int max) =>
            SendToFlutter("vibrancyChanged", "{\"current\":" + current + ",\"max\":" + max + "}");

        // The one place every outbound message funnels through. Currently a
        // logged no-op — there is no Flutter host to receive this yet. Swap
        // the body for the chosen plugin's send call once one is added.
        private void SendToFlutter(string eventName, string jsonPayload)
        {
            Debug.Log($"[FlutterBridge] (no host attached) {eventName}: {jsonPayload}");
        }

        private static string Escape(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("\"", "\\\"");
    }
}
