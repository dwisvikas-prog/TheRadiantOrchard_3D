using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RadiantOrchard
{
    // Central progression singleton. Every other system (NPCs, fruit zones, UI,
    // and later the Flutter bridge) reads/writes through here instead of holding
    // its own copy of state, and reacts via these events instead of polling.
    public class GameState : MonoBehaviour
    {
        public static GameState Instance { get; private set; }

        [SerializeField] private int currentVibrancy;
        [SerializeField] private int maxVibrancy = 100;
        [SerializeField] private string currentLevelId;
        [SerializeField] private string defaultLevelId = "level_01";

        private readonly HashSet<string> completedLevels = new HashSet<string>();
        private readonly HashSet<string> completedVirtues = new HashSet<string>();
        private readonly Dictionary<string, EmotionState> npcEmotionStates = new Dictionary<string, EmotionState>();
        private readonly Dictionary<string, int> objectiveProgress = new Dictionary<string, int>();
        private readonly Dictionary<string, FruitGrowthStage> fruitZoneStates = new Dictionary<string, FruitGrowthStage>();
        private readonly List<DecorationEntry> unlockedDecorations = new List<DecorationEntry>();

        public event Action<int, int> VibrancyChanged;
        public event Action<string> LevelStarted;
        public event Action<string> LevelCompleted;
        public event Action<string> VirtueCompleted;
        public event Action<string, EmotionState> NpcEmotionChanged;
        public event Action<string, int> ObjectiveProgressChanged;
        public event Action<string, FruitGrowthStage> FruitZoneChanged;
        public event Action StickmanHealed;
        public event Action<int, Vector3> DecorationUnlocked;

        private float vibrancyMultiplier = 1f;
        private Coroutine vibrancyMultiplierRoutine;

        // Single source of truth for the fruit-zone key format — FruitHarvester
        // (writer) and VibrancyMeterUI (reader) both call this instead of each
        // building "fruitzone_" + type by hand, so the two sides can't drift.
        public static string FruitZoneId(FruitType type) => "fruitzone_" + type;

        public int CurrentVibrancy => currentVibrancy;
        public int MaxVibrancy => maxVibrancy;
        public string CurrentLevelId => currentLevelId;
        public IReadOnlyList<DecorationEntry> UnlockedDecorations => unlockedDecorations;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadGame();

            if (string.IsNullOrEmpty(currentLevelId))
                StartLevel(defaultLevelId);
        }

        // --- Vibrancy -----------------------------------------------------

        public void AddVibrancy(int amount)
        {
            int scaled = Mathf.RoundToInt(amount * vibrancyMultiplier);
            SetVibrancy(currentVibrancy + scaled);
        }

        public void SetVibrancy(int value)
        {
            int clamped = Mathf.Clamp(value, 0, maxVibrancy);
            if (clamped == currentVibrancy) return;
            currentVibrancy = clamped;
            VibrancyChanged?.Invoke(currentVibrancy, maxVibrancy);
            RequestSave();
        }

        // "Golden Harvest": every AddVibrancy call is scaled by multiplier
        // until duration elapses, then reverts to 1x. A new call while one is
        // already running replaces it rather than stacking.
        public void StartVibrancyMultiplier(float multiplier, float duration)
        {
            if (vibrancyMultiplierRoutine != null) StopCoroutine(vibrancyMultiplierRoutine);
            vibrancyMultiplierRoutine = StartCoroutine(VibrancyMultiplierRoutine(multiplier, duration));
        }

        private IEnumerator VibrancyMultiplierRoutine(float multiplier, float duration)
        {
            vibrancyMultiplier = multiplier;
            yield return new WaitForSeconds(duration);
            vibrancyMultiplier = 1f;
            vibrancyMultiplierRoutine = null;
        }

        // Fired whenever a Stickman is fully healed (ReceiveTonic) — QuizManager
        // and RewardManager both count these to decide when to trigger.
        public void NotifyStickmanHealed()
        {
            StickmanHealed?.Invoke();
        }

        // --- Island decorations --------------------------------------------------

        // RewardManager calls this to both persist an unlock and tell every
        // listener (including its own future Start() on reload) to build it.
        public void AddDecoration(int decorationIndex, Vector3 position)
        {
            unlockedDecorations.Add(new DecorationEntry { decorationIndex = decorationIndex, position = position });
            DecorationUnlocked?.Invoke(decorationIndex, position);
            SaveGame();
        }

        // Called by EditModeManager when the player finishes dragging
        // decorations around — overwrites saved positions in place (same
        // order RewardManager built them in) without touching which
        // decorations are unlocked.
        public void UpdateDecorationPositions(IReadOnlyList<Vector3> positions)
        {
            for (int i = 0; i < positions.Count && i < unlockedDecorations.Count; i++)
                unlockedDecorations[i].position = positions[i];
            RequestSave();
        }

        // --- Levels ---------------------------------------------------------

        public void StartLevel(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return;

            // Advancing into a *different* level restarts the meter from zero.
            // Without this the previous level's threshold stays pinned at max for
            // the rest of the run: every later AddVibrancy clamps to the value
            // already stored, SetVibrancy early-returns, and VibrancyChanged is
            // never raised again — so the HUD bar freezes and every following
            // level's requiredVibrancy gate is satisfied for free. Re-entering the
            // active level (same id) deliberately does not wipe mid-level progress.
            bool isNewLevel = !string.Equals(currentLevelId, levelId, StringComparison.Ordinal);
            currentLevelId = levelId;
            if (isNewLevel) SetVibrancy(0);

            LevelStarted?.Invoke(levelId);
            SaveGame();
        }

        public void CompleteLevel(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return;
            if (completedLevels.Add(levelId))
            {
                LevelCompleted?.Invoke(levelId);
                SaveGame();
            }
        }

        public bool IsLevelCompleted(string levelId) => !string.IsNullOrEmpty(levelId) && completedLevels.Contains(levelId);

        // --- Virtues ----------------------------------------------------------

        public void CompleteVirtue(string virtueId)
        {
            if (string.IsNullOrEmpty(virtueId)) return;
            if (completedVirtues.Add(virtueId))
            {
                VirtueCompleted?.Invoke(virtueId);
                SaveGame();
            }
        }

        public bool IsVirtueCompleted(string virtueId) => !string.IsNullOrEmpty(virtueId) && completedVirtues.Contains(virtueId);

        // --- NPC emotion state --------------------------------------------------

        public void SetNpcEmotion(string npcId, EmotionState emotion)
        {
            if (string.IsNullOrEmpty(npcId)) return;
            npcEmotionStates[npcId] = emotion;
            NpcEmotionChanged?.Invoke(npcId, emotion);
            RequestSave();
        }

        public EmotionState GetNpcEmotion(string npcId)
        {
            if (!string.IsNullOrEmpty(npcId) && npcEmotionStates.TryGetValue(npcId, out var emotion)) return emotion;
            return EmotionState.Neutral;
        }

        // --- Objective progress --------------------------------------------------

        public void SetObjectiveProgress(string objectiveId, int progress)
        {
            if (string.IsNullOrEmpty(objectiveId)) return;
            objectiveProgress[objectiveId] = progress;
            ObjectiveProgressChanged?.Invoke(objectiveId, progress);
            RequestSave();
        }

        public int GetObjectiveProgress(string objectiveId)
        {
            if (!string.IsNullOrEmpty(objectiveId) && objectiveProgress.TryGetValue(objectiveId, out var progress)) return progress;
            return 0;
        }

        // --- Fruit zone state --------------------------------------------------

        public void SetFruitZoneStage(string zoneId, FruitGrowthStage stage)
        {
            if (string.IsNullOrEmpty(zoneId)) return;
            fruitZoneStates[zoneId] = stage;
            FruitZoneChanged?.Invoke(zoneId, stage);
            RequestSave();
        }

        public FruitGrowthStage GetFruitZoneStage(string zoneId)
        {
            if (!string.IsNullOrEmpty(zoneId) && fruitZoneStates.TryGetValue(zoneId, out var stage)) return stage;
            return FruitGrowthStage.Seed;
        }

        // --- Save / Load --------------------------------------------------

        // SaveGame() used to be called synchronously from every mutation
        // (vibrancy tick, heal, zone stage, objective progress...), which during
        // a fast harvesting streak meant a full JSON serialize+disk write nearly
        // every frame that changed something. RequestSave() batches those into
        // at most one write per saveDebounceSeconds; call sites that used to call
        // SaveGame() directly now call RequestSave() instead, and OnDisable
        // flushes any pending save so nothing is lost on scene/app exit.
        [SerializeField] private float saveDebounceSeconds = 2f;
        private Coroutine pendingSaveRoutine;

        public void RequestSave()
        {
            if (pendingSaveRoutine == null)
                pendingSaveRoutine = StartCoroutine(DebouncedSave());
        }

        private void FlushPendingSave()
        {
            if (pendingSaveRoutine == null) return;
            StopCoroutine(pendingSaveRoutine);
            pendingSaveRoutine = null;
            SaveGame();
        }

        private void OnApplicationQuit() => FlushPendingSave();
        private void OnApplicationPause(bool paused) { if (paused) FlushPendingSave(); }
        private void OnDisable() => FlushPendingSave();

        private IEnumerator DebouncedSave()
        {
            yield return new WaitForSeconds(saveDebounceSeconds);
            pendingSaveRoutine = null;
            SaveGame();
        }

        public void SaveGame()
        {
            var data = new SaveData
            {
                schemaVersion = 1,
                currentVibrancy = currentVibrancy,
                maxVibrancy = maxVibrancy,
                currentLevelId = currentLevelId
            };

            data.completedLevels.AddRange(completedLevels);
            data.completedVirtues.AddRange(completedVirtues);

            foreach (var kvp in npcEmotionStates)
                data.npcEmotionStates.Add(new StringEntry { key = kvp.Key, value = kvp.Value.ToString() });

            foreach (var kvp in objectiveProgress)
                data.objectiveProgress.Add(new IntEntry { key = kvp.Key, value = kvp.Value });

            foreach (var kvp in fruitZoneStates)
                data.fruitZoneStates.Add(new StringEntry { key = kvp.Key, value = kvp.Value.ToString() });

            data.unlockedDecorations.AddRange(unlockedDecorations);

            SaveSystem.Save(data);
        }

        public void LoadGame()
        {
            var data = SaveSystem.Load();
            if (data == null) return; // no save yet — safe fresh start with inspector defaults

            currentVibrancy = data.currentVibrancy;
            maxVibrancy = data.maxVibrancy > 0 ? data.maxVibrancy : maxVibrancy;
            currentLevelId = data.currentLevelId;

            completedLevels.Clear();
            foreach (var id in data.completedLevels) completedLevels.Add(id);

            completedVirtues.Clear();
            foreach (var id in data.completedVirtues) completedVirtues.Add(id);

            npcEmotionStates.Clear();
            foreach (var entry in data.npcEmotionStates)
            {
                if (Enum.TryParse(entry.value, out EmotionState emotion))
                    npcEmotionStates[entry.key] = emotion;
            }

            objectiveProgress.Clear();
            foreach (var entry in data.objectiveProgress) objectiveProgress[entry.key] = entry.value;

            fruitZoneStates.Clear();
            foreach (var entry in data.fruitZoneStates)
            {
                if (Enum.TryParse(entry.value, out FruitGrowthStage stage))
                    fruitZoneStates[entry.key] = stage;
            }

            unlockedDecorations.Clear();
            if (data.unlockedDecorations != null) unlockedDecorations.AddRange(data.unlockedDecorations);
        }

        // Resets the *current attempt* without touching permanent progress
        // (completedLevels/completedVirtues/unlockedDecorations stay intact) —
        // this is what the in-game Restart button should call before reloading
        // the scene. GameState is a DontDestroyOnLoad singleton, so a plain
        // SceneManager.LoadScene(sameScene) does NOT reset it: the reloaded
        // scene's GameState.Awake() finds Instance already set and destroys
        // itself, leaving the old vibrancy/state exactly as it was — "Restart"
        // then visibly does nothing.
        public void RestartCurrentLevel()
        {
            SetVibrancy(0);
            npcEmotionStates.Clear();
            objectiveProgress.Clear();
            fruitZoneStates.Clear();
            SaveGame();
            LevelStarted?.Invoke(currentLevelId);
        }

        // Full reset back to the very start of the game (Level 1) — wipes
        // vibrancy, level/virtue completion, objective and NPC state, and the
        // save file, then starts defaultLevelId. This is what the in-game
        // Restart button calls: per-level RestartCurrentLevel() above would
        // otherwise leave a player who has already reached Level 2 right back
        // on Level 2 after "restarting", which is not what this game's
        // Restart button is meant to do.
        public void RestartGameFromBeginning()
        {
            ResetProgress();
            StartLevel(defaultLevelId);
        }

        // Development-only reset, called from the Tools menu — never exposed in a
        // release build UI.
        public void ResetProgress()
        {
            currentVibrancy = 0;
            currentLevelId = null;
            completedLevels.Clear();
            completedVirtues.Clear();
            npcEmotionStates.Clear();
            objectiveProgress.Clear();
            fruitZoneStates.Clear();
            unlockedDecorations.Clear();
            SaveSystem.DeleteSave();
            PlayerPrefs.DeleteKey(TutorialManager.SeenPrefKey);
            PlayerPrefs.DeleteKey(CoCGuideUI.SeenKey);
            VibrancyChanged?.Invoke(currentVibrancy, maxVibrancy);
        }
    }
}
