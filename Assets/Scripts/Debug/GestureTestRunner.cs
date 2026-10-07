#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RadiantOrchard
{
    // Manual QA tool for the Block 7 test pass. No touchscreen/Unity Remote
    // available in this environment, so instead of polling Input this fires
    // GestureManager's events directly (via reflection on their private
    // backing delegates) at each fruit's screen position — exercising the
    // real downstream pipeline (FruitHarvester -> TonicFlight ->
    // StickmanController -> GameState -> QuizManager) faithfully. It does
    // NOT exercise GestureManager's own Input.touchCount recognition code —
    // that still needs a real device. Debug-only; safe to delete afterward.
    // Uses reflection to reach private members deliberately (a QA harness,
    // not shipped code) — editor/dev-build only, never in a release build.
    public class GestureTestRunner : MonoBehaviour
    {
        [SerializeField] private float postGestureWait = 1.5f;
        [SerializeField] private int simultaneousBatchSize = 3;

        private const string Tag = "[TestPass]";

        [ContextMenu("Run Test Pass")]
        public void RunFromMenu() => StartCoroutine(RunFullPass());

        private IEnumerator RunFullPass()
        {
            var gestureManager = FindFirstObjectByType<GestureManager>();
            if (gestureManager == null) { Debug.LogError($"{Tag} ABORT: no GestureManager in scene."); yield break; }

            var cam = Camera.main;
            if (cam == null) { Debug.LogError($"{Tag} ABORT: no main camera."); yield break; }

            // IsHarvestable, not !Harvested: a fruit whose Stickman already
            // celebrated or walked off can't be healed any more (the gesture would
            // spend the fruit on a heal that can't land), so testing one would
            // report a false FAIL. NPCSpawner clears those within the frame.
            var harvesters = FindObjectsByType<FruitHarvester>(FindObjectsSortMode.None)
                .Where(h => h != null && h.IsHarvestable)
                .ToList();

            if (harvesters.Count == 0)
            {
                Debug.LogError($"{Tag} ABORT: no harvestable fruit in the scene. Stickmen arrive on a timer " +
                               $"(NPCSpawner.spawnInterval / the active level's pacing), so a pass started before " +
                               $"the first spawn has nothing to test — wait for one, or check the NPCSpawner " +
                               $"warnings (unwired spawn points / empty fruit pool mean nothing ever spawns).");
                yield break;
            }

            Debug.LogWarning($"{Tag} === START === {harvesters.Count} harvestable fruits found.");

            int pass = 0, fail = 0, healsSoFar = 0;

            // --- Multiple-simultaneous-Stickmen sub-test: fire the first N
            // gestures back-to-back with no wait between, then check them all. ---
            var batch = harvesters.Take(simultaneousBatchSize).ToList();
            harvesters = harvesters.Skip(simultaneousBatchSize).ToList();

            Debug.LogWarning($"{Tag} --- Simultaneous batch: firing {batch.Count} gestures back-to-back ---");
            int batchVibrancyBefore = CurrentVibrancy();
            foreach (var h in batch)
                FireGesture(gestureManager, h.FruitData.gestureType, cam.WorldToScreenPoint(h.transform.position));

            yield return new WaitForSeconds(postGestureWait + 0.5f);

            foreach (var h in batch)
            {
                bool ok = LogFruitResult(h, "[simultaneous]", batchVibrancyBefore);
                if (ok) pass++; else fail++;
                healsSoFar++;
                if (healsSoFar == 5) yield return CheckQuizTriggered();
            }

            // --- Remaining fruits, one gesture at a time ---
            foreach (var h in harvesters)
            {
                var screenPos = cam.WorldToScreenPoint(h.transform.position);
                int vibrancyBefore = CurrentVibrancy();
                FireGesture(gestureManager, h.FruitData.gestureType, screenPos);

                yield return new WaitForSeconds(postGestureWait);

                bool ok = LogFruitResult(h, "[sequential]", vibrancyBefore);
                if (ok) pass++; else fail++;
                healsSoFar++;
                if (healsSoFar == 5) yield return CheckQuizTriggered();
            }

            Debug.LogWarning($"{Tag} === DONE === {pass}/{pass + fail} fruits resolved correctly. Total heals counted: {healsSoFar}.");
        }

        // A heal is only a pass if everything the player sees actually happened:
        // the Stickman left Idle_Sad for Celebrate (visual restoration), the
        // Vibrancy Meter received the gain, and the TonicFlight consumed the
        // fruit. The meter leg is checked explicitly because it used to freeze
        // silently when GameState wasn't ready at the HUD's Start.
        private bool LogFruitResult(FruitHarvester h, string label, int vibrancyBefore)
        {
            var stickman = h.TargetStickman;
            bool healed = stickman != null && stickman.CurrentState == StickmanState.Celebrate;
            string who = stickman != null ? stickman.name : "(stickman gone)";

            int vibrancy = CurrentVibrancy();
            // Clamped-at-max counts as moved: SetVibrancy early-returns when the
            // value doesn't change, so a full bar is not a failed heal.
            int maxVibrancy = GameState.Instance != null ? GameState.Instance.MaxVibrancy : 0;
            bool meterMoved = vibrancy > vibrancyBefore || (maxVibrancy > 0 && vibrancy >= maxVibrancy);
            float meterFill = MeterTargetFill();
            bool meterFillMatches = meterFill < 0f || Mathf.Approximately(meterFill, MeterExpectedFill());

            bool ok = healed && meterMoved && h.Harvested && meterFillMatches;
            string details =
                $"healed={healed} state={(stickman != null ? stickman.CurrentState.ToString() : "n/a")} " +
                $"harvested={h.Harvested} vibrancy {vibrancyBefore}->{vibrancy}" +
                (meterFill < 0f ? " meter=absent" : $" meterFill={meterFill:F2} (expected {MeterExpectedFill():F2})");

            Debug.LogWarning($"{Tag} {(ok ? "PASS" : "FAIL")} {label} {h.FruitData.displayName} " +
                             $"({h.FruitData.gestureType}) -> {who} | {details}");
            return ok;
        }

        private static int CurrentVibrancy() => GameState.Instance != null ? GameState.Instance.CurrentVibrancy : -1;

        private static float MeterExpectedFill()
        {
            var state = GameState.Instance;
            return state != null && state.MaxVibrancy > 0 ? state.CurrentVibrancy / (float)state.MaxVibrancy : 0f;
        }

        // Reads VibrancyMeterUI's target (the value the bar is easing toward) so a
        // broken subscription shows up as a FAIL rather than a passing Stickman
        // with a frozen bar. -1 = no meter in the scene.
        private static float MeterTargetFill()
        {
            var meter = FindFirstObjectByType<VibrancyMeterUI>();
            return meter != null ? GetPrivateField<float>(meter, "targetFill") : -1f;
        }

        private IEnumerator CheckQuizTriggered()
        {
            yield return null; // let this frame's event handlers run first

            // GameObject.Find skips inactive objects, and the quiz panel starts
            // inactive — search by type (inactive included) instead.
            var panelUI = FindFirstObjectByType<QuizPanelUI>(FindObjectsInactive.Include);
            if (panelUI == null) { Debug.LogError($"{Tag} FAIL Quiz: no QuizPanelUI found in scene."); yield break; }

            bool active = panelUI.gameObject.activeSelf;
            Debug.LogWarning($"{Tag} {(active ? "PASS" : "FAIL")} Quiz panel active after 5th heal: {active}");
            if (!active) yield break;
            var question = GetPrivateField<QuizQuestion>(panelUI, "currentQuestion");
            var buttons = GetPrivateField<Button[]>(panelUI, "optionButtons");

            if (question == null || buttons == null || question.correctIndex < 0 || question.correctIndex >= buttons.Length)
            {
                Debug.LogError($"{Tag} FAIL Quiz: could not read current question/buttons via reflection.");
                yield break;
            }

            Debug.LogWarning($"{Tag} Quiz question: \"{question.questionText}\" — answering correctly (option {question.correctIndex}).");
            buttons[question.correctIndex].onClick.Invoke();

            yield return new WaitForSeconds(0.2f);

            float multiplier = GetPrivateField<float>(GameState.Instance, "vibrancyMultiplier");
            Debug.LogWarning($"{Tag} {(Mathf.Approximately(multiplier, 2f) ? "PASS" : "FAIL")} Golden Harvest multiplier active after correct answer: {multiplier}");
        }

        private void FireGesture(GestureManager gm, GestureType type, Vector2 screenPos)
        {
            string fieldName = type switch
            {
                GestureType.DoubleTap => "OnDoubleTap",
                GestureType.LongPress => "OnLongPress",
                GestureType.FastSwipe => "OnFastSwipe",
                _ => null
            };
            if (fieldName == null) return;

            var field = typeof(GestureManager).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            var del = field?.GetValue(gm) as MulticastDelegate;
            if (del == null)
            {
                Debug.LogWarning($"{Tag} No subscribers on GestureManager.{fieldName} (nothing was listening at that position).");
                return;
            }

            if (type == GestureType.FastSwipe)
                del.DynamicInvoke(screenPos, screenPos + Vector2.right * 150f);
            else
                del.DynamicInvoke(screenPos);
        }

        private static T GetPrivateField<T>(object obj, string fieldName)
        {
            if (obj == null) return default;
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return field != null ? (T)field.GetValue(obj) : default;
        }

        // Drives EditModeManager's private drag methods directly with fake
        // screen positions, since real mouse/touch input can't be simulated
        // from an editor script. Returns a report string.
        public static string TestEditModeDrag(string decorationName, Vector2 fromScreenPos, Vector2 toScreenPos)
        {
            var editGO = GameObject.Find("EditModeManager");
            if (editGO == null) return "FAIL: EditModeManager not found.";
            var em = editGO.GetComponent<EditModeManager>();

            var decoGO = GameObject.Find(decorationName);
            if (decoGO == null) return "FAIL: decoration '" + decorationName + "' not found.";

            var type = typeof(EditModeManager);
            var tryStartDrag = type.GetMethod("TryStartDrag", BindingFlags.NonPublic | BindingFlags.Instance);
            var dragTo = type.GetMethod("DragTo", BindingFlags.NonPublic | BindingFlags.Instance);
            var draggingField = type.GetField("dragging", BindingFlags.NonPublic | BindingFlags.Instance);

            var posBefore = decoGO.transform.position;

            tryStartDrag.Invoke(em, new object[] { fromScreenPos });
            var picked = draggingField.GetValue(em) as Transform;

            dragTo.Invoke(em, new object[] { toScreenPos });

            var posAfter = decoGO.transform.position;

            return $"picked={(picked != null ? picked.name : "NONE")} posBefore={posBefore} posAfter={posAfter}";
        }

        // Clears EditModeManager's private "dragging" field, mimicking the
        // release step (TouchPhase.Ended / GetMouseButtonUp) that the real
        // input path does automatically but a one-shot reflection-driven
        // test skips.
        public static void ClearEditModeDrag()
        {
            var editGO = GameObject.Find("EditModeManager");
            if (editGO == null) return;
            var em = editGO.GetComponent<EditModeManager>();
            var draggingField = typeof(EditModeManager).GetField("dragging", BindingFlags.NonPublic | BindingFlags.Instance);
            draggingField.SetValue(em, null);
        }

        // One-off diagnostic used to debug why the quiz didn't trigger; call
        // via the Editor scripting bridge (Reflection isn't allowed there directly).
        public static string DumpQuizState()
        {
            var canvasGO = GameObject.Find("GameplayUI");
            if (canvasGO == null) return "GameplayUI not found.";

            var qm = canvasGO.GetComponent<QuizManager>();
            if (qm == null) return "QuizManager not found on GameplayUI.";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("healCount=" + GetPrivateField<int>(qm, "healCount"));
            sb.AppendLine("healsPerQuiz=" + GetPrivateField<int>(qm, "healsPerQuiz"));

            var panelUI = GetPrivateField<QuizPanelUI>(qm, "panelUI");
            sb.AppendLine("panelUI assigned=" + (panelUI != null));
            if (panelUI != null)
                sb.AppendLine("panelUI.gameObject.activeSelf=" + panelUI.gameObject.activeSelf + " name=" + panelUI.gameObject.name);

            var questions = GetPrivateField<QuizQuestion[]>(qm, "questions");
            sb.AppendLine("questions.Length=" + (questions != null ? questions.Length : -1));

            var gs = GameState.Instance;
            var del = GetPrivateField<MulticastDelegate>(gs, "StickmanHealed");
            sb.AppendLine("StickmanHealed subscriber count=" + (del != null ? del.GetInvocationList().Length : 0));

            return sb.ToString();
        }

        // Isolates whether QuizManager's trigger logic or QuizPanelUI.Show()
        // itself is the problem, by calling Show() directly and reading back
        // every field it touches.
        public static string TestShowDirectly()
        {
            var canvasGO = GameObject.Find("GameplayUI");
            var qm = canvasGO.GetComponent<QuizManager>();
            var panelUI = GetPrivateField<QuizPanelUI>(qm, "panelUI");

            var panelField = GetPrivateField<GameObject>(panelUI, "panel");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("panel field assigned=" + (panelField != null) + " name=" + (panelField != null ? panelField.name : "null"));
            sb.AppendLine("panel.activeSelf BEFORE Show=" + (panelField != null ? panelField.activeSelf.ToString() : "n/a"));

            var q = new QuizQuestion { questionText = "Manual test question", options = new[] { "A", "B", "C" }, correctIndex = 0 };
            panelUI.Show(q, (i, question) => Debug.LogWarning($"{Tag} Manual Show() callback fired with index {i}"));

            sb.AppendLine("panel.activeSelf AFTER Show=" + (panelField != null ? panelField.activeSelf.ToString() : "n/a"));
            sb.AppendLine("panelUI.gameObject.activeSelf AFTER Show=" + panelUI.gameObject.activeSelf);

            return sb.ToString();
        }
    }
}
#endif
