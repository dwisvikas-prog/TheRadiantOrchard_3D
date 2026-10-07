# The Radiant Orchard — Level 1 & Level 2 Feature Audit

Scope: Read-only audit, all Level 1/2 systems wired exclusively in `Assets/CoC_BlankGround.unity`, driven by `Assets/Scripts/Core/EmptyIslandPhaseRunner.cs`.

Legend: ✅ Fully working | 🟡 Partially implemented | 🔴 Missing | ⚠️ Exists but broken/unclear | UNKNOWN — could not verify

---

## LEVEL 1 AUDIT

- **Grey/empty starting island**: ✅
  - Evidence: `Assets/Scripts/Core/EmptyIslandBootstrap.cs`, `StickmanController.EnterIdleSad()` applies grey material to visitors.

- **Wisdom Tree**: ✅
  - Evidence: `Assets/Scripts/Core/FirstWishTreePlant.cs` (`TreeName = "WisdomTree"`), `EmptyIslandBootstrap.EnsureWishTree()`. Auto-plants via `AutoPlantSequence()`: sapling → 10s grow timer (`WishTreeGrowTimer`) → full tree pop-in.

- **Stone Well**: ✅
  - Evidence: `Assets/Scripts/Core/StoneWellSetup.cs` (`EnsureGoodWell()`), `Assets/Scripts/Core/WellWishController.cs` (`GameObject.Find("StoneWell")`).

- **First wish**: ✅
  - Evidence: `WellWishController.OpenWish()` → `OnDropCoin()` → `CoinDropRoutine` → `EmptyIslandPhaseRunner.NotifyWishDone()`. Wish text is hardcoded ("Orchard Blessing") — no typing required.

- **Coin/Radiance/reward system**: 🟡 Partially implemented
  - Evidence: Coin exists only as a one-off cosmetic VFX prop in `WellWishController.CoinDropRoutine` — no persistent currency. Actual reward/progression metric is **Vibrancy** (`GameState.CurrentVibrancy`, `VibrancyManager.cs`), plus decoration unlocks (`RewardManager.cs`, `GameState.AddDecoration`).
  - Notes: No "Radiance" named system found anywhere in scripts. No shop currency/coin economy for buying items — shop items are free/placement-only.

- **First visitor tutorial**: ✅
  - Evidence: `Assets/Scripts/Core/FirstVisitorArrival.cs` (spawns "FirstVisitor", `StickmanGentleWalk.WalkTo`, mood set to "Frustrated" via `FruitLessonBook.Get(FruitType.Strawberry)`) chained into `Assets/Scripts/Core/FirstPlantTutorial.cs` (5-step guided plant→harvest→heal flow).

- **Sequential visitors**: ✅
  - Evidence: `EmptyIslandPhaseRunner` strictly gates phases one at a time (`AdvanceTo` only allows +1 step); `Assets/Scripts/Core/EmptyIslandMoreVisitors.cs` and `Assets/Scripts/Core/NPCSpawner.cs` pace subsequent visitors in the `CoreLoop` phase.

- **All 9 fruits**: ✅ (list confirmed)
  - Evidence: `Assets/Scripts/Data/GameEnums.cs` `FruitType` enum: **Strawberry, Pineapple, Watermelon, Lemon, Grapes, Apple, Peach, Banana, Cherry** (exactly 9). Mapped to tree/gesture in `Assets/Scripts/Core/FruitTreeCatalog.cs`, lesson entries in `FruitLessonBook.cs`/`FruitHealthMatrix.cs`.

- **Fruit unlocking**: ✅
  - Evidence: `Assets/Scripts/Core/FruitTeachProgress.cs` (`IsTaught`/`MarkTaught`/`AllTaught`/`TaughtCount`, PlayerPrefs-backed), called from `FirstPlantTutorial.Finish()`; `EmptyIslandHUD.UnlockFruit(FruitType.Strawberry)` called from both `EmptyIslandPhaseRunner.CoreLoop` and `FirstPlantTutorial.Finish()`.

- **Correct fruit highlighting**: ✅
  - Evidence: `Assets/Scripts/UI/FruitGlowHighlight.cs` (pulsing halo + breathing scale), invoked from `GestureWhereGuide.Show(...)` in `FirstPlantTutorial.cs` step 2 (harvest step).

- **Planting**: ✅
  - Evidence: `FirstPlantTutorial.SpawnPlantSpot()`/`OnPlantSpotTapped()`/`GrowTreeAndFruit()` — glowing soil + ring visuals, tap-to-plant via `FirstPlantSpotClick`. Subsequent-fruit planting likely in `Assets/Scripts/Core/FruitPlantLessonFlow.cs` (referenced but not read line-by-line).

- **Growth animation**: 🟡 Partially implemented
  - Evidence: Wish Tree has an explicit growth animation (`FirstWishTreePlant.AutoPlantSequence`: sapling scale-lerp over `GrowSeconds=10f`, then pop-scale tree). `FruitTreeCatalog.SpawnTree()` for fruit trees instantiates full-size instantly — no visible seed→sapling→mature stages, despite a `FruitGrowthStage` enum (`Seed, Growing, Healthy, Vibrant, Harvested`) existing in `GameEnums.cs`; `GameState.SetFruitZoneStage` is only ever called with `.Harvested`.
  - Notes: growth-stage progression is defined but not driving any visual over time for fruit/trees — only the one-time Wish Tree sequence is a true growth animation.

- **Harvesting**: ✅
  - Evidence: `Assets/Scripts/Interaction/FruitHarvester.cs` — gesture-match harvesting (`TryHarvest`, screen-space tap-radius check, `IsHarvestable`, pooling, VFX burst, tonic dispatch to Stickman).

- **Gesture system**: ✅
  - Evidence: `Assets/Scripts/Core/GestureManager.cs` — DoubleTap/LongPress/FastSwipe via new Input System `EnhancedTouch`, with Editor/desktop mouse fallback (double-click, hold, drag-swipe, middle-click shortcut). `GameEnums.GestureType` matches exactly.

- **Tonic/healing**: ✅
  - Evidence: `Assets/Scripts/Core/TonicFlight.cs` (Bezier-arc flight + object pool), `StickmanController.ReceiveTonic()` (grey→color lerp, `HealVfx.Play`, `GameState.AddVibrancy`, `NotifyStickmanHealed`).

- **Patience**: ✅
  - Evidence: `StickmanController.cs` — `patienceTime = 25f`, `patienceTimer` incremented in `Idle_Sad` state, `SetPatienceTime(float)` public API (First visitor disables timeout via `SetPatienceTime(99999f)`).

- **Timeout**: ✅
  - Evidence: `StickmanController.GiveUp()` — triggered when `patienceTimer >= patienceTime`; applies vibrancy penalty (`const int penalty = 6`), shows coach message "A visitor left unhealed — vibrancy dropped", transitions to `Leaving` state. `FruitPlantLessonFlow.CancelFor(this)` also called to avoid soft-locking the lesson flow.

- **Vibrancy change**: ✅
  - Evidence: `Assets/Scripts/Core/GameState.cs` (`CurrentVibrancy`, `AddVibrancy`, `SetVibrancy`, `VibrancyChanged` event), `VibrancyManager.cs` (facade), `VibrancyObjectiveDriver.cs`, `Assets/Scripts/UI/VibrancyMeterUI.cs`; `LevelManager.cs` uses `requiredVibrancy` as the level-completion gate.

- **Tree information card**: ✅
  - Evidence: `Assets/Scripts/UI/TreeInfoCardUI.cs` (`Show(FruitType)` — fruit icon, virtue pill, "Feeling/Heals/Body system" text, gesture badge), opened via `TreeInfoTapController.cs`/`TreeInfoTarget.cs`.

- **"?" gesture legend**: ✅
  - Evidence: `Assets/Scripts/UI/GestureLegendCardUI.cs` — `Toggle()` builds a scrollable card listing every `FruitLessonBook.All` entry with icon, name, gesture (`GestureShort`).

- **Level 1 completion**: ✅
  - Evidence: `Assets/Scripts/Core/EmptyIslandLevelProgress.cs` exposes `OnLevel1Complete` (via `CheckLevel1Completion()`, called from `EmptyIslandBootstrap.Awake()` as a catch-up check), consumed by `Assets/Scripts/UI/Level1CompleteBanner.cs` ("You learned all 9 fruits and their virtues").
  - Notes: Completion condition is tied to `FruitTeachProgress.AllTaught()` (all 9 fruits taught), per comments in `Level1CompleteBanner.cs`/`EmptyIslandBootstrap.cs`.

- **Level 2 unlock**: ✅
  - Evidence: `Level1CompleteBanner.Continue()` calls `EmptyIslandLevelProgress.AdvanceToLevel2()` then `Level2Intro.Show()`.

---

## LEVEL 2 AUDIT

- **Level 2 transition**: ✅
  - Evidence: `Assets/Scripts/Core/Level2Intro.cs` — `Wire()` subscribes `EmptyIslandLevelProgress.OnDecorationProgress`/`OnLevel2DecorationGoalComplete`; `Show()` instructs player to open Shop and place `Level2DecorationTarget` decorations.

- **Guide/tutorial**: ✅
  - Evidence: `Level2Intro.Show()` reuses the existing `EmptyIslandCoachBar` (same guide used in Level 1); progress toasts via `SetNewEvent("Decoration placed! X/Y")`.
  - Notes: No separate, dedicated Level-2-only tutorial script — it reuses the existing guide/coach-bar system.

- **Shop**: ✅
  - Evidence: `Assets/Scripts/UI/EmptyIslandShopUI.cs` — CoC-style shop panel, category tabs (All/Nature/Rocks/Trees), scrollable cards from `Assets/Scripts/Core/IslandShopCatalog.cs` (9 entries: grass, bush, rock1, rock2, pine, green/apple/pear/plum trees).

- **3D item previews**: 🟡 Partially implemented
  - Evidence: Shop cards use `Assets/Scripts/UI/ShopIconCapture.cs` to render a captured static icon (`GetIconAsync`), not a live-rotating 3D preview. The real 3D model only appears as a translucent "ghost" (`SpawnVisual(entry, pos, ghost:true)`, 40% alpha) once the player is placing the item.
  - Notes: Static icon + placement-time ghost, not an interactive/rotatable 3D preview inside the shop panel itself.

- **Decoration placement**: ✅
  - Evidence: `EmptyIslandShopUI.SelectItem()` → `WaitTapPlace()` coroutine — tap-to-place on snapped grid tile (`EditModeManager.SnapToCoCTile`), validated via `IsSpotOk()` (distance checks vs well/wish tree/other trees/decorations), spawns visual, registers with `EditModeManager.RegisterDecoration`, `GameState.AddDecoration`, `EmptyIslandLevelProgress.RegisterDecorationPlaced()`.

- **Drag/move**: ✅
  - Evidence: `Assets/Scripts/Core/EditModeManager.cs` — full CoC-style drag system (`EnterEditMode`/`ExitEditMode`, touch + mouse, `TryStartDrag`/`DragTo`/`EndDrag`), works for `decorations` and `treeRoots` (fruit trees + HQ scenery), grid-snapped (`SnapToCoCTile`).

- **Collision/overlap prevention**: ✅
  - Evidence: `EditModeManager.IsTileFree()` checks distance vs Stone Well, Wish Tree, and all movables using per-type `Footprint()` radii; red/green ghost tile feedback (`SetGhostColor`); `FindNearestFree()` fallback on invalid drop. Shop placement separately validated via `EmptyIslandShopUI.IsSpotOk()`.

- **Decoration persistence**: ✅
  - Evidence: `Assets/Scripts/Core/GameState.cs` — `UnlockedDecorations` list, `AddDecoration`, `UpdateDecorationPositions` (called from `EditModeManager.SaveLayout()` on edit-mode exit), persisted via `SaveSystem.Save/Load`; rebuilt on scene load in `Assets/Scripts/Core/RewardManager.cs` `Start()` (`foreach (var entry in GameState.Instance.UnlockedDecorations) BuildDecoration(...)`).

---

## Notable Gaps / Uncertainties

1. No "Radiance" terminology/system exists in code — **Vibrancy** is the actual implemented currency/progress metric. Coin exists only as a cosmetic wish-well prop.
2. `FruitGrowthStage` enum (Seed/Growing/Healthy/Vibrant) is defined but not driving any real growth animation for fruit trees — only the Wish Tree has an authored grow sequence.
3. Shop "3D item previews" are static captured icons, not live-rotating previews; the only true 3D view of the model is the translucent placement ghost.
4. **UNKNOWN** — `Assets/Scripts/Core/FruitPlantLessonFlow.cs` (repeat-visitor fruit teaching for fruits 2-9) exists and is wired in (gates Shop/Edit via `IsBusy`, called from `EmptyIslandPhaseRunner.CoreLoop`), but was not read line-by-line. Exact per-fruit teaching mechanics beyond the first (Strawberry) are unverified.
