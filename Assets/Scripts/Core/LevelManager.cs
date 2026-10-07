using UnityEngine;

namespace RadiantOrchard
{
    // Watches objective progress AND vibrancy for the active level and, once
    // every objective is done and the level's requiredVibrancy is reached,
    // completes the level + its virtue and (if set) starts the next level —
    // closing the loop from "tap NPC/fruit zone or heal a Stickman" to
    // "level actually finishes" that GameState alone doesn't do.
    //
    // Vibrancy is the primary completion gate (PRD: reaching the threshold
    // completes the level): requiredVibrancy > 0 blocks completion until
    // GameState.CurrentVibrancy reaches it. Completion is re-checked on
    // ObjectiveProgressChanged, VibrancyChanged and StickmanHealed so the
    // heal loop (AddVibrancy per tonic) drives it without any direct
    // coupling to StickmanController.
    //
    // Save migration: levels built before VibrancyObjectiveDriver existed
    // never had their heal objectives reported — a save reloaded past the
    // threshold would sit at 0 objective progress forever. Once the vibrancy
    // gate passes, heal-type objectives (HelpNPC / InteractWithNPC) are
    // backfilled: the vibrancy that got here can only have come from those
    // heals. VibrancyObjectiveDriver keeps reporting them normally for fresh
    // playthroughs.
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private LevelDefinition[] allLevels;

        public static LevelManager Instance { get; private set; }

        private LevelDefinition currentLevel;
        public LevelDefinition CurrentLevel => currentLevel;

        // NPCSpawner (and anything else that paces itself per level) subscribes
        // to this instead of polling GameState.CurrentLevelId + re-resolving it.
        public event System.Action<LevelDefinition> LevelChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            if (GameState.Instance == null) return;

            GameState.Instance.LevelStarted += OnLevelStarted;
            GameState.Instance.ObjectiveProgressChanged += OnObjectiveProgressChanged;
            GameState.Instance.VibrancyChanged += OnVibrancyChanged;
            GameState.Instance.StickmanHealed += OnStickmanHealed;

            OnLevelStarted(GameState.Instance.CurrentLevelId);
        }

        private void OnDestroy()
        {
            if (GameState.Instance == null) return;

            GameState.Instance.LevelStarted -= OnLevelStarted;
            GameState.Instance.ObjectiveProgressChanged -= OnObjectiveProgressChanged;
            GameState.Instance.VibrancyChanged -= OnVibrancyChanged;
            GameState.Instance.StickmanHealed -= OnStickmanHealed;
        }

        private void OnLevelStarted(string levelId)
        {
            currentLevel = FindLevel(levelId);
            LevelChanged?.Invoke(currentLevel);

            // Save migration: a run whose active level is already recorded as
            // completed (level finished with no nextLevel set, or a save written
            // before GameState.StartLevel cleared the meter) would otherwise sit
            // pinned at max — no further VibrancyChanged ever fires, so the HUD bar
            // never moves and this level's gate passes for free. Clear it so the
            // meter is usable again. Safe no-op on a fresh, unfinished level.
            if (currentLevel != null &&
                GameState.Instance != null &&
                GameState.Instance.IsLevelCompleted(currentLevel.levelId))
            {
                GameState.Instance.SetVibrancy(0);
            }

            CheckLevelCompletion(); // in case objectives/threshold were already satisfied (e.g. save reload)
        }

        private LevelDefinition FindLevel(string levelId)
        {
            if (allLevels == null || string.IsNullOrEmpty(levelId)) return null;
            foreach (var level in allLevels)
                if (level != null && level.levelId == levelId) return level;
            return null;
        }

        private void OnObjectiveProgressChanged(string objectiveId, int progress) => CheckLevelCompletion();
        private void OnVibrancyChanged(int current, int max) => CheckLevelCompletion();
        private void OnStickmanHealed() => CheckLevelCompletion();

        private void CheckLevelCompletion()
        {
            if (currentLevel == null || GameState.Instance == null) return;
            if (GameState.Instance.IsLevelCompleted(currentLevel.levelId)) return;

            // Vibrancy gate first: the level's threshold is the main
            // completion condition — no amount of tapped objectives
            // completes a level whose vibrancy never got there.
            if (currentLevel.requiredVibrancy > 0 &&
                GameState.Instance.CurrentVibrancy < currentLevel.requiredVibrancy)
                return;

            BackfillHealObjectivesAtThreshold();

            foreach (var objective in currentLevel.objectives)
            {
                if (GameState.Instance.GetObjectiveProgress(objective.objectiveId) < objective.requiredCount)
                    return; // at least one objective still incomplete
            }

            GameState.Instance.CompleteLevel(currentLevel.levelId);
            SfxPlayer.Instance?.PlayLevelComplete();

            if (currentLevel.virtue != null)
                GameState.Instance.CompleteVirtue(currentLevel.virtue.virtueId);

            if (currentLevel.nextLevel != null)
                GameState.Instance.StartLevel(currentLevel.nextLevel.levelId);
        }

        // Runs only once the vibrancy gate above has passed AND the level
        // actually declares one (requiredVibrancy > 0 — otherwise this would
        // gift objectives to a level with no gate at all). Heals are the sole
        // source of vibrancy in the current loop, so a heal-type objective
        // still sitting at 0 on a save that's already at/over the threshold
        // was simply never reported — count it as done rather than stranding
        // the save one objective short of a level it earned.
        private void BackfillHealObjectivesAtThreshold()
        {
            if (currentLevel.requiredVibrancy <= 0) return;
            if (GameState.Instance.CurrentVibrancy < currentLevel.requiredVibrancy) return;

            foreach (var objective in currentLevel.objectives)
            {
                if (objective.type != ObjectiveType.HelpNPC &&
                    objective.type != ObjectiveType.InteractWithNPC) continue;
                if (objective.requiredCount <= 0) continue;
                if (GameState.Instance.GetObjectiveProgress(objective.objectiveId) >= objective.requiredCount) continue;

                GameState.Instance.SetObjectiveProgress(objective.objectiveId, objective.requiredCount);
            }
        }
    }
}
