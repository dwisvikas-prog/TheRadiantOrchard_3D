using UnityEngine;

namespace RadiantOrchard
{
    // Bridges the heal loop into level objectives. Every healed Stickman
    // bumps every heal-type objective (HelpNPC / InteractWithNPC) of the
    // active level by one, so "help N visitors" objectives complete through
    // normal tonic deliveries instead of only through the separate
    // NPCInteractable tap path (which was the sole progress writer before —
    // meaning a level whose objectives nobody tapped could never finish no
    // matter how many Stickmen were healed).
    //
    // Vibrancy itself needs no bridging: StickmanController already calls
    // GameState.AddVibrancy per heal, and LevelManager now re-checks
    // completion on VibrancyChanged.
    //
    // Scene wiring: attach to the same object as LevelManager (the
    // WireCfsaccaManagers editor tool does this). If no LevelManager exists
    // (e.g. playing an old scene before wiring, or a save reloaded past the
    // threshold where LevelManager stopped reporting), this falls back to
    // driving GameState directly so the loop can still run start-to-finish.
    public class VibrancyObjectiveDriver : MonoBehaviour
    {
        private bool subscribed;

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void Start()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            if (!subscribed || GameState.Instance == null) return;
            GameState.Instance.StickmanHealed -= OnStickmanHealed;
            subscribed = false;
        }

        private void TrySubscribe()
        {
            if (subscribed || GameState.Instance == null) return;
            GameState.Instance.StickmanHealed += OnStickmanHealed;
            subscribed = true;
        }

        private void OnStickmanHealed()
        {
            var level = LevelManager.Instance != null ? LevelManager.Instance.CurrentLevel : null;

            if (level != null)
            {
                ReportHealObjectives(level);
                return;
            }

            // Fallback (no LevelManager in this scene): replicate the
            // objective side of level completion directly. If the vibrancy
            // threshold is already met and every heal objective is maxed,
            // complete the level + virtue and advance, so a save/scene
            // missing LevelManager still finishes Level 1 instead of
            // silently stalling. LevelManager's own backfill handles the
            // case where this component wasn't present for earlier heals.
            var state = GameState.Instance;
            var levelId = state.CurrentLevelId;
            if (string.IsNullOrEmpty(levelId) || state.IsLevelCompleted(levelId)) return;

            var levelDef = FindLoadedLevel(levelId);
            if (levelDef == null) return;

            ReportHealObjectives(levelDef);

            if (levelDef.requiredVibrancy > 0 && state.CurrentVibrancy < levelDef.requiredVibrancy) return;

            foreach (var objective in levelDef.objectives)
            {
                if (state.GetObjectiveProgress(objective.objectiveId) < objective.requiredCount) return;
            }

            state.CompleteLevel(levelId);
            SfxPlayer.Instance?.PlayLevelComplete();

            if (levelDef.virtue != null)
                state.CompleteVirtue(levelDef.virtue.virtueId);

            if (levelDef.nextLevel != null)
                state.StartLevel(levelDef.nextLevel.levelId);
        }

        private void ReportHealObjectives(LevelDefinition level)
        {
            if (level == null || GameState.Instance == null) return;

            foreach (var objective in level.objectives)
            {
                if (objective.type != ObjectiveType.HelpNPC &&
                    objective.type != ObjectiveType.InteractWithNPC) continue;

                int progress = GameState.Instance.GetObjectiveProgress(objective.objectiveId);
                if (progress >= objective.requiredCount) continue; // already done — don't inflate saved progress

                GameState.Instance.SetObjectiveProgress(objective.objectiveId, progress + 1);
            }
        }

        // Same lookup LevelManager does, minus the serialized allLevels list —
        // only used on the no-LevelManager fallback path. Resources
        // deliberately: GameData assets live under Assets/GameData, which is
        // not a Resources folder, so this stays inert in normal scenes.
        private static LevelDefinition FindLoadedLevel(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return null;
            foreach (var obj in Resources.LoadAll<LevelDefinition>(string.Empty))
            {
                if (obj != null && obj.levelId == levelId) return obj;
            }
            return null;
        }
    }
}
