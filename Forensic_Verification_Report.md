# Forensic Verification Report — CoC_BlankGround.unity

Scope: read-only investigation of the active scene `Assets/CoC_BlankGround.unity` and the
code path that drives it (`EmptyIslandPhaseRunner` → Pad/Landmarks/PlantWishTree/WellWish/
FirstVisitor/FirstPlant/CoreLoop). Per the prior audit's confirmed facts (9 fruits exist,
Vibrancy is the real currency, "Radiance"/persistent coins do not exist, `FruitGrowthStage`
is stubbed, Shop previews are static icons), this report does not re-derive those and
focuses on sections C, D, E as requested.

---

## C. Existing-system gaps

### C.1 Genuinely missing systems (searched, not found anywhere in code)

- **"Radiance" currency / persistent coin economy.** Grepped the whole `Assets/Scripts`
  tree for `Radiance`, `CoinEconomy`, `class Radiance` — zero hits. Confirms prior audit:
  the only persistent progress values are `GameState.CurrentVibrancy`
  (`Assets/Scripts/Core/GameState.cs`), `PlayerPrefs`-backed flags
  (`EmptyIslandProgress`, `FruitTeachProgress`, `EmptyIslandLevelProgress`), and the
  well-wish string (`WellWishController.PrefsKey`).
- **Live/rotating 3D shop previews.** Confirmed by inspecting `Assets/Scripts/UI/ShopIconCapture.cs`
  — it renders a static icon via an offscreen camera (`class Runner : MonoBehaviour` at
  line 202 is a throwaway render-helper, not a live preview object). No rotating-preview
  component exists anywhere in `Assets/Scripts/UI` or `Assets/Scripts/Core`.
- **Any growth-stage-driven visuals for fruit trees.** `FruitGrowthStage` (Seed/Growing/
  Healthy/Vibrant/Harvested) is only ever *written* with `.Harvested`
  (`Assets/Scripts/Interaction/FruitHarvester.cs:197`) and *read* generically in
  `GameState.cs:218-221` (default-returns `.Seed` for unknown zones) and in
  `Assets/Scripts/UI/VibrancyMeterUI.cs:137-155` (only branches on `== Harvested` to bump
  an icon's scale). No tree/plant mesh, shader, or scale ever changes based on Seed/
  Growing/Healthy/Vibrant — those three stages are declared but never produced or acted on
  visually anywhere in the codebase.
- **A second camera rig coexisting with `DioramaController`.** `EmptyIslandBootstrap.
  ApplyCloseUpCamera()` (`Assets/Scripts/Core/EmptyIslandBootstrap.cs:43-67`) explicitly
  disables `CameraOrbitController` so only one camera system runs — orbit camera control
  is therefore present in code but is never active in this scene.

### C.2 Partially implemented systems (scaffolding exists, not fully wired/visually complete)

- **`FruitGrowthStage` progression** (see C.1) — enum and storage dictionary
  (`GameState.fruitZoneStates`, `GameState.cs:24`) exist and fire a `FruitZoneChanged`
  event (`GameState.cs:33`), but only one of five enum values is ever produced
  (`.Harvested`), so the "growth" concept is scaffolding without content.
- **`EmptyIslandLevelProgress` Level 2 decoration goal** — `RegisterDecorationPlaced()`,
  `Level2DecorationGoalDone`, and `OnLevel2DecorationGoalComplete` (`Assets/Scripts/Core/
  EmptyIslandLevelProgress.cs:64-76`) are fully coded, but `EmptyIslandPhaseRunner`'s
  phase machine (CoC_BlankGround's actual driver) never progresses past `CoreLoop` and
  never calls `AdvanceToLevel2()` or `RegisterDecorationPlaced()` from anywhere reachable
  in the CoreLoop phase — `Level2Intro.cs`/`EmptyIslandShopUI.cs` are Level-2 UI wired via
  `Level2Intro.Wire()` in `EmptyIslandBootstrap.cs:30`, so the Level-2 decoration goal only
  activates after `CurrentLevel` is advanced by whatever calls `AdvanceToLevel2()` — UNKNOWN,
  not located inside any CoC_BlankGround-reachable file in this pass; worth a follow-up
  grep across `Level2Intro.cs`/`EmptyIslandDecorationReward.cs` specifically.
- **`FirstPlantTutorial` step UI** — `BuildUI()` (`FirstPlantTutorial.cs:149-158`) sets
  `canvas`/`panel`/`titleText`/etc. all to `null` with the comment "Girl alone guides First
  Plant"; the five-step `Steps[]` array (title/body/ok strings, lines 28-46) is fully
  authored but its `title`/`ok` fields are consumed only partially (`ok` text is unused —
  `ShowStep()` never reads `Steps[step].ok`, driving CTA text ad hoc per-case instead) —
  the struct's `ok` field is dead data for 4 of 5 steps.
- **Shop/Edit gating via `FruitPlantLessonFlow.IsBusy`** — functions (confirmed wired into
  `EmptyIslandMoreVisitors.Loop()` line 40 and presumably Shop/Edit per prior audit), but
  depends on exactly one cleanup path (`CancelFor`) to ever clear if a lesson is abandoned
  mid-flow; see D.2 below — it is "wired" but fragile, not robustly complete.

### C.3 Systems that exist but are not visibly used / called with only one constant value

- **`FruitGrowthStage`**: 4 of 5 enum values (`Seed, Growing, Healthy, Vibrant`) are never
  passed to `SetFruitZoneStage` anywhere (only call site: `FruitHarvester.cs:197`, always
  `.Harvested`). `GetFruitZoneStage` (`GameState.cs:218`) has no confirmed caller found in
  this pass outside `VibrancyMeterUI.cs` reacting to the event, not calling the getter.
- **`StickmanState.Idle_Sad` clip name reused for `Leaving`**: comment in
  `StickmanController.cs:990-993` states Leaving used to visually equal Idle_Sad and was
  changed to Walk — the `Idle_Sad` string literal is still the *only* other reference, i.e.
  `StickmanState` enum values are all used, but the mapping comment flags a historical
  dead branch now bypassed (no current bug, just notable single-value history).
- **`NPCSpawner.patienceTime`, `maxRoamingStickmen`, `fruitSpawnPoints` etc.**: these
  Inspector fields are fully read inside `NPCSpawner.cs`, but `NPCSpawner` itself is never
  attached to, nor instantiated from, any code path used by `CoC_BlankGround` (see C.4) —
  the whole class's serialized configuration surface is unused in the target scene.
- **`LevelManager.Instance`**: referenced defensively (`if (LevelManager.Instance != null)`)
  in `NPCSpawner.cs:72`, `VibrancyObjectiveDriver.cs:52`, and `SceneDiagnostic.cs:64`, but
  `LevelManager` is attached only in `Assets/newmvp.unity` and `Assets/newunitydesign.unity`
  (confirmed via scene-file grep) — never in `CoC_BlankGround.unity`. In the target scene
  `LevelManager.Instance` is always `null`, so all three call sites' `LevelManager`-based
  branches are permanently dead code paths for this scene (the `null`-guard means they fail
  silently rather than erroring).

### C.4 Dead/unused-looking code (no incoming references in the target scene's reachable code, or not attached in the scene file)

The scene file `Assets/CoC_BlankGround.unity` contains **only 4** directly-serialized
`MonoBehaviour` script references (`m_Script: {fileID: 11500000, guid: …}`), confirmed by
grep of the raw YAML:

| GUID | Resolves to |
|---|---|
| `4c4f1547e8708114ebb44605ef512288` | `Assets/Scripts/Core/WellWishController.cs` |
| `e7816d764e47add49a88a78fa409dcef` | `Assets/Scripts/Core/EmptyIslandBootstrap.cs` |
| `4b2c1302035b5344999cc9334488bcad` | `Assets/myassets/DioramaController.cs` |
| `a79441f348de89743a2939f4d699eac1` | UNKNOWN — could not verify; no matching `.meta` found under `Assets/` in this pass (likely a built-in/package component, e.g. a UI or Input module) |

This confirms the architecture: almost everything else (`EmptyIslandPhaseRunner`,
`FirstVisitorArrival`, `FirstWishTreePlant`, `StickmanController`, `FirstPlantTutorial`,
`FruitPlantLessonFlow`, `EmptyIslandHUD`, `EmptyIslandShopUI`, etc.) is spawned at runtime
via `new GameObject(...).AddComponent<T>()` / static `Ensure()` calls, driven transitively
from `EmptyIslandBootstrap.Awake()` (`EmptyIslandBootstrap.cs:28` calls
`EmptyIslandPhaseRunner.Ensure()`). Nothing in that runtime-spawn chain is literally "in"
the scene file, which is expected for this project's pattern — not itself a defect — but it
means scene-file inspection alone cannot find orphaned scripts; call-graph tracing was
required (done below).

Classes confirmed **not referenced anywhere in the CoC_BlankGround-reachable code path**,
and confirmed to be attached only in *other* scene files (`newmvp.unity`,
`newunitydesign.unity`, `The Main RadiantOrchard_3d.unity`) or not instantiated at all:

- `Assets/Scripts/Core/NPCSpawner.cs` — attached in `newmvp.unity` and
  `The Main RadiantOrchard_3d.unity` only; no `AddComponent<NPCSpawner>` or `Ensure()`
  call exists anywhere in `Assets/Scripts`. For CoC_BlankGround, visitor spawning is instead
  handled by the unrelated, simpler `EmptyIslandMoreVisitors.cs`. **`NPCSpawner` is entirely
  dead code for the target scene.**
- `Assets/Scripts/Core/RewardManager.cs` — attached only in `newmvp.unity` and
  `The Main RadiantOrchard_3d.unity`; no instantiation call found in scripts.
- `Assets/Scripts/Core/QuizManager.cs` / `Assets/Scripts/UI/QuizPanelUI.cs` /
  `Assets/Scripts/Data/QuizDatabase.cs` — attached only in `newmvp.unity`,
  `newunitydesign.unity`, `The Main RadiantOrchard_3d.unity`.
- `Assets/Scripts/Core/OnboardingManager.cs` — attached only in `newunitydesign.unity` and
  `The Main RadiantOrchard_3d.unity`.
- `Assets/Scripts/Core/FruitPlotBuilding.cs` — attached only in
  `The Main RadiantOrchard_3d.unity`.
- `Assets/Scripts/Core/CoCVillageController.cs` / `CoCVillageLayout.cs` — only reachable via
  `MainSceneSquareGround.Apply()` → `CoCVillageLayout.Apply()`
  (`Assets/Scripts/Core/MainSceneSquareGround.cs:58`), which is itself only invoked from
  `MainScenePlayFraming.ApplyFraming()` when `isMain` is true
  (`Assets/Scripts/Core/MainScenePlayFraming.cs:36-45`) — a condition gated on the scene
  name containing "RadiantOrchard"/"Main Radiant". `CoC_BlankGround` does not match, so this
  whole chain is dead for the target scene even though `MainScenePlayFraming`'s
  `[RuntimeInitializeOnLoadMethod]` (line 9) runs unconditionally on every scene load,
  including CoC_BlankGround (it just no-ops there — confirmed by the name-match guard in
  `TryApply()`, lines 19-28).
- `Assets/Scripts/Core/LevelManager.cs` — not attached in `CoC_BlankGround.unity` (see
  C.3); all `LevelManager.Instance` consumers degrade to no-ops in this scene.

No commented-out large blocks or `[Obsolete]` attributes were found in the core
phase-runner files read in full for this report (`EmptyIslandPhaseRunner.cs`,
`FirstVisitorArrival.cs`, `FirstPlantTutorial.cs`, `FruitPlantLessonFlow.cs`,
`WellWishController.cs`, `StickmanController.cs`, `EmptyIslandLevelProgress.cs`,
`NPCSpawner.cs`, `EmptyIslandMoreVisitors.cs`) — the dead code found is whole-class/whole-
system dead for this scene, not inline commented fragments.

---

## D. Progression risks

All line numbers reference the files as read in full during this pass.

1. **Toast dismissal has no timeout and fully depends on `EmptyIslandCoachBar`'s callback
   firing.** `EmptyIslandPhaseRunner.EnterPhaseRoutine()` (lines 126-131):
   ```
   bool dismissed = false;
   ShowPhaseToast(p, () => dismissed = true);
   while (!dismissed) yield return null;
   ```
   `ShowPhaseToast()` (lines 402-422) sets `BlocksWorldInput = true` and relies entirely on
   `EmptyIslandCoachBar.SetTip(...)`'s `onDismiss` lambda being invoked by a UI button tap.
   If the coach-bar button is destroyed, fails to bind, or the tip panel is hidden by some
   other system before the player can tap it, `dismissed` never becomes true, the coroutine
   never leaves this `while`, and **`BlocksWorldInput` stays `true` forever** — a confirmed
   total-input-lock soft-lock trigger with no fallback. (`EmptyIslandCoachBar.cs` itself was
   not read in this pass — flagged in section E as the single highest-priority file to
   audit next.)

2. **`FirstVisitorArrival.ArrivalRoutine()` has a narrow unreachable-softlock edge case.**
   Lines 99-106: if `SpawnMainStickman()` returns null (missing `Resources/Stickman`
   prefab) **and** `SpawnPlaceholder()` also returns null, the coroutine does
   `yield break` without ever calling `MarkArrived()` →
   `EmptyIslandPhaseRunner.NotifyVisitorArrived()` is never invoked. Since
   `SpawnPlaceholder()` creates a primitive capsule and can only return null if
   `AddComponent<StickmanController>()` throws, this is a low-probability but real gap: no
   timeout exists on the `FirstVisitor` phase at all, unlike `FirstPlantTutorial` which
   does have a 45s timeout + retry + hard fallback (see point 4). If this path is hit, the
   phase runner is stuck at `FirstVisitor` indefinitely with no recovery.

3. **`IsBusy` clearing depends on exactly one teardown path per visitor lifecycle, and only
   one of two ways a visitor can terminate calls it correctly.**
   `FruitPlantLessonFlow.IsBusy` (lines 31-33) is `true` whenever a `FruitPlantLessonFlow`
   or `QuietFruitAssist` instance exists, and gates `EmptyIslandMoreVisitors.Loop()`
   (`EmptyIslandMoreVisitors.cs:40`) as well as Shop/Edit (per prior audit). The only
   confirmed caller of the cleanup method `FruitPlantLessonFlow.CancelFor(StickmanController)`
   is `StickmanController.GiveUp()` (`StickmanController.cs:605`), itself only reached when
   a stickman's **patience timer** expires (`StickmanController.cs:151-154`,
   `patienceTimer >= patienceTime`). If a lesson-bearing visitor is destroyed or disabled by
   any *other* path — e.g. `EmptyIslandPhaseRunner.CleanupAllStepUIs()` destroying a
   `FirstVisitorArrival`/`FirstPlantTutorial` GameObject mid-flow (lines 161-198), or a scene
   reload while `step == 2` inside `FruitPlantLessonFlow` — `CancelFor` is never called,
   `FruitPlantLessonFlow.IsBusy` stays `true` forever, and **all future visitors stop
   spawning and Shop/Edit remain permanently locked.** This matches the class's own code
   comment at lines 36-39 warning about exactly this failure mode, but the mitigation
   (`CancelFor`) is reachable from only the patience-timeout path, not from
   phase-transition-driven destruction.

4. **Asymmetric timeout/fallback coverage between `FirstPlantTutorial` and
   `FruitPlantLessonFlow`.** `FirstPlantTutorial` has `HarvestTimeoutFallback()` (90s,
   lines 254-262) and `HealTimeoutFallback()` (6s, lines 264-271) that force-advance the
   step if the player/engine never fires `OnHarvested`/`OnHealed`. The structurally
   identical per-visitor lesson flow used for every *subsequent* fruit,
   `FruitPlantLessonFlow`, has **no harvest timeout at all** — `ShowStep(2)` (lines 147-152)
   waits on `FruitHarvester.OnAnyHarvested` with no fallback coroutine, so if the harvest
   event never fires (e.g. the fruit is destroyed by a pooling edge case, or
   `IsHarvestable` silently goes false for a reason other than patience timeout — see
   `NPCSpawner.cs:258` comment acknowndging exactly this condition exists for other
   spawners), this specific lesson instance hangs with `IsBusy == true` forever, with
   recovery possible only via the patience-timeout path in point 3 above, not a self-timeout.
   It does have an 8s post-heal fallback (`FinishAfterHeal()`, lines 418-439) but nothing
   covering the harvest-wait step itself.

5. **`AdvanceTo()`'s "only +1 step" clamp silently reinterprets out-of-order calls rather
   than failing loudly.** `EmptyIslandPhaseRunner.AdvanceTo()` (lines 325-351): if a caller
   requests more than one step ahead, the method logs and substitutes `phase + 1` instead of
   the originally-requested phase (lines 334-338). Combined with the fact that `Notify*`
   methods (lines 353-375) only forward when `phase` exactly equals the expected source
   phase (e.g. `NotifyPlantDone()` only acts `if (phase == FirstPlant)`), any ordering bug
   upstream that calls `Notify*` from the wrong phase is swallowed as a silent no-op log
   line rather than surfaced — functionally safe against double-advance, but it means a
   genuine upstream bug (an event firing from the wrong phase) would be invisible except in
   the console log, and the flow would simply appear "stuck" to the player with the real
   cause masked.

6. **`EnterPhaseRoutine` is asymmetric about who may enter a phase and what state they
   expect.** The `WellWish` phase can be entered two ways with different implicit
   preconditions: normally via `AdvanceTo(WellWish)` from `PlantWishTree` (expects the Wish
   Tree just finished growing), or via `ResolveStartPhase()` on a *fresh app launch*
   (`EmptyIslandPhaseRunner.cs:100-106`) when `FirstWishTreePlant.IsPlanted` is already true
   from a previous session but `WishDone` is not — in that case `ActivatePhaseSystems`
   (line 216-220) calls `FirstWishTreePlant.EnsureTreeVisible()` instead of actually running
   the plant growth sequence. This is handled (not a bug), but it is an asymmetric entry
   path worth flagging: the two entries exercise different code (`FirstWishTreePlant.Ensure()`
   vs `EnsureTreeVisible()`), so a regression in one path would not necessarily show up when
   testing the other (e.g. testing only "fresh install" would not catch a resume-from-
   WellWish regression, and vice versa).

7. **Editor-only full progress reset on every Play.** `EmptyIslandPhaseRunner.Start()`
   (lines 60-71): under `UNITY_EDITOR`, `EmptyIslandProgress.ClearAll()` is called and the
   phase is hard-set to `Landmarks` unconditionally, every single time Play is pressed —
   `ResolveStartPhase()` (the real resume logic used in builds) is **never exercised in the
   Editor**. This means the resume-from-saved-phase logic (`ResolveStartPhase`, lines
   100-106) can only be tested in an actual device/standalone build, not in the Editor
   Play mode that is presumably used for most iteration — a real risk that progression bugs
   in the non-editor resume path go unnoticed until a build.

---

## E. Recommended next investigation

Smallest concrete set of files to read/audit before changing anything, in priority order:

1. **`Assets/Scripts/UI/EmptyIslandCoachBar.cs`** — every phase-advance gate in
   `EmptyIslandPhaseRunner` (toasts, Landmarks continue, mood tips) routes through
   `SetTip(...)`'s callback. This is the single biggest soft-lock surface identified in D.1
   and was not read in this pass; its `onDismiss` wiring and any internal destroy/cleanup
   logic must be verified before touching phase-advance code.
2. **`Assets/Scripts/Core/FirstWishTreePlant.cs`** — referenced constantly by
   `EmptyIslandPhaseRunner` (`Ensure`, `EnsureTreeVisible`, `IsPlanted`, `OnCompleted` event,
   `SpotName`, `TreeName`) but not read line-by-line in this pass; it is the other half of
   the two-entry-path asymmetry noted in D.6.
3. **`Assets/Scripts/Core/EmptyIslandProgress.cs`** — backs `ResolveStartPhase()`'s resume
   logic (`PlantDone`, `WishDone`) and is wiped every Editor Play (D.7); needs to be read to
   confirm exactly which PlayerPrefs keys it owns and whether any of them can desync from
   `EmptyIslandPhaseRunner.PrefsKey` or `FirstPlantTutorial.PrefsKey` / `WellWishController.PrefsKey`.
4. **`Assets/Scripts/Core/QuietFruitAssist.cs`** — the other half of
   `FruitPlantLessonFlow.IsBusy` (`QuietFruitAssist.IsActive`) and has its own
   `CancelFor(StickmanController)` called from `GiveUp()` — needs the same mid-flow-teardown
   scrutiny as `FruitPlantLessonFlow` (D.3) since it shares the identical gating risk.
5. **`Assets/Scripts/Core/EmptyIslandGuideStack.cs`** — a `Push`/`Pop` layer stack
   (`Layer.Lesson`, `Layer.Gesture`, `Layer.Reward`) that multiple independent flows
   (`FirstPlantTutorial`, `FruitPlantLessonFlow`, `EmptyIslandMoreVisitors`) push/pop into
   without reading each other — an unbalanced push/pop (e.g. a `Pop` on a destroyed-mid-flow
   object that never ran its matching cleanup, per D.3) could leave a layer stuck "on" and
   block subsequent UI; needs its own read before trusting any Push/Pop call site.
6. **`Assets/Scripts/Core/Level2Intro.cs`** — the actual call site of
   `EmptyIslandLevelProgress.AdvanceToLevel2()` was not located in this pass (flagged
   UNKNOWN in C.2); this file is the most likely owner and should be confirmed before any
   Level-2 work starts.
