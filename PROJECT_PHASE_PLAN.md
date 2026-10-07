# The Radiant Orchard — Phase Status & Development Plan

**Date:** 23 Sep 2026  
**Source of truth:** Project Proposal (WhatsApp screenshots) + `cehck.md` phases + live Unity project audit  
**Playable scenes:** `Assets/newmvp.unity`, `Assets/The Main RadiantOrchard_3d.unity`

---

## 1. Verdict — Which Phase Is Actually Completed?

> **Important:** “% done” below = *code/systems exist*.  
> For **working quality / perfect or not**, see **`CORE_FEATURE_QUALITY_AUDIT.md`**.  
> Honest product readiness of the core loop is about **5.5–7/10**, not 95%.

| Phase | Name | Code exists | Working quality |
|------:|------|-------------|-----------------|
| **0** | Audit / docs | Done | OK |
| **1** | Scene foundation | Mostly done | Good in `newmvp` |
| **2** | First playable core loop | Done | **Playable, not perfect** (gestures fragile) |
| **3** | Level system L1–L5 | Done | Works via vibrancy; objectives soft |
| **4** | All 9 fruits + gestures | Done | Fruits OK; Long Press / swipe weak on device |
| **5** | Wisdom Tree quiz | Partial | Code yes; content ≠ proposal |
| **6** | Polish | In progress | Weak (placeholder VFX/SFX/haptics) |
| **7** | Mobile optimization | Not started | — |
| **8** | Flutter | Stub only | — |

### Bottom line

> Phases **0–4 exist as systems**, but they are **not “perfect / finished”**.  
> Treat the game as a **solid prototype**. Fix P0 issues in `CORE_FEATURE_QUALITY_AUDIT.md` before trusting phase-complete claims.

---

## 2. Proposal Feature Checklist (from attachments)

### 2.1 Virtue–Health Matrix (proposal)

| Virtue (proposal) | Fruit | Health / dialogue cue | Gesture | In project? |
|-------------------|-------|------------------------|---------|-------------|
| Love | Strawberry | Heart / Circulation | Double Tap | Fruit + gesture **yes**; virtue named **Kindness** instead |
| Joy | Pineapple | Immunity / Energy | Double Tap | Fruit + gesture **yes**; virtue named **Gratitude** |
| Peace | Watermelon | Nervous System | Double Tap | Fruit + gesture **yes**; virtue named **Respect** |
| Patience | Lemon | Detox / Digestion | Long Press | **Yes** (Patience matches) |
| Meekness | Grapes | Respiratory / Breath | Long Press | Fruit + gesture **yes**; virtue named **Forgiveness** |
| Self-Control | Apple | Brain / Focus | Long Press | Fruit + gesture **yes**; virtue named **Perseverance** |
| Kindness | Peach | Skin / Glow | Fast Swipe | Fruit + gesture **yes**; virtue named **Honesty** |
| Goodness | Banana | Muscle / Strength | Fast Swipe | Fruit + gesture **yes**; virtue named **Generosity** |
| Faithfulness | Cherry | Bone / Stability | Fast Swipe | Fruit + gesture **yes**; virtue named **Courage** |

**Decision needed:** Align live virtues to the Fruits-of-the-Spirit names in the proposal (Love, Joy, Peace…), or keep the current educational set (Kindness, Gratitude…). Health-stat dialogue/VFX from the proposal are **not** wired yet.

### 2.2 Core loop (proposal §3)

| Step | Proposal | Status | Notes |
|------|----------|--------|-------|
| Request | Grey Stickman + symptom bubble | **Done** | `SymptomBubble` shows fruit icon (not emoji text) |
| Harvest | Correct fruit + correct gesture | **Done** | `GestureManager` + `FruitHarvester` |
| Delivery | Fruit → 3D vital tonic → flies | **Done** | `TonicFlight` |
| Transformation | Color restore + joyful anim + Vibrancy | **Done** | Material swap + `Celebrate` + `GameState.AddVibrancy` |
| Wisdom Tree quiz | Every 5 levels | **Partial** | Code fires on Level 5; no interactive Wisdom Tree prop drive |
| Golden Harvest | 2× points on correct answer | **Done** | `StartVibrancyMultiplier(2, 30s)` |

### 2.3 Visual / audio aesthetic (proposal §4)

| Item | Status |
|------|--------|
| Low-poly 3D + pastel lighting | **Partial** — assets present; lighting polish still open |
| Prop squash / stretch (trees, well, benches) | **Missing / minimal** |
| Harvest particle “explosions” | **Partial** — procedural `SimpleVfx.Burst`, not authored per-virtue |
| Gesture-specific haptics | **Partial** — one `Handheld.Vibrate()` on harvest |
| Lo-fi music that speeds up with difficulty | **Partial** — ambient via `SfxPlayer`; no tempo ramp |

---

## 3. Stickman — Features & Animations (deep dive)

**Prefab:** `Assets/GameData/Prefabs/Stickman.prefab`  
**Controller:** `Assets/Scripts/Core/StickmanController.cs`  
**Animators:** `StickmanState.controller` + Male/Female walk overrides

### 3.1 State machine (implemented)

| State | Purpose | Animation clip used | Status |
|-------|---------|---------------------|--------|
| `Idle_Sad` | Waiting for tonic (grey) | Sad idle | **Done** |
| `Receiving` | Tonic arriving | Receiving pose | **Done** |
| `Celebrate` | Joyful reaction after heal | Celebrate | **Done** |
| `Roaming` | Happy walk around island | Walk (male/female) | **Done** |
| `Leaving` | Patience expired | Leave / sad idle | **Done** |

### 3.2 Stickman feature matrix

| Feature | Status | Detail |
|---------|--------|--------|
| Grey → color restore | **Partial** | Instant grey/color **material swap**; proposal wants smoother color restore / health VFX |
| Symptom / need UI | **Done** | `SymptomBubble` (fruit icon card) |
| Text thought bubble / dialogue by health stat | **Missing** | `ThoughtBubble` exists but is on legacy NPC path |
| Joyful celebration | **Done** | Celebrate anim + VFX burst + SFX |
| Patience timer → leave | **Done** | Configurable per level |
| NavMesh roam after heal | **Done** | Whole-island wander |
| Ambient self-care (Spirit) | **Done** | `NPCSpiritComponent` + fruit seeking |
| Look-at player / camera | **Done** | `NPCLookAt` |
| Gender walk variants | **Done** | Male/Female override controllers |
| Pool + spawn with matching fruit | **Done** | `NPCSpawner` |
| Per-health-stat visual (heart, glow, etc.) | **Missing** | Proposal matrix not implemented as VFX/dialogue |

### 3.3 Animation backlog (Phase 6)

Priority order:

1. **Smooth grey→color lerp** on Stickman (shader or material property), not hard swap  
2. Distinct **Receiving** polish (hands up / absorb tonic) if clip is weak  
3. Stronger **Celebrate** read on mobile (bigger motion / particles)  
4. Optional **Injured / LowSpirit** ambient poses (Spirit system already has conditions)  
5. Prop **squash/stretch** on well, benches, trees (proposal aesthetic)

---

## 4. Systems Already Built (do not rebuild)

| System | Key paths |
|--------|-----------|
| Game progression / save | `GameState`, `SaveSystem`, `SaveData` |
| Gestures | `GestureManager` (DoubleTap / LongPress / FastSwipe) |
| Harvest → tonic → heal | `FruitHarvester`, `TonicFlight`, `StickmanController` |
| Levels 1–5 | `LevelManager`, `GameData/Levels/Level_0N.asset` |
| Vibrancy HUD | `VibrancyMeterUI`, `ColorRestorationDriver` |
| Quiz plumbing | `QuizManager`, `QuizPanelUI`, `QuizDatabase.asset` |
| Tutorial pointer | `TutorialManager` (+ fuller `OnboardingManager` unused in `newmvp`) |
| Environment | Floating island, orchard zones, PolyOne fruits, rocks/trees packs |

---

## 5. Remaining Plan by Stage

### Stage A — Close Phase 5 (Quiz / Wisdom Tree) — **1–3 days**

- [ ] Wire QuizManager + QuizPanel into the **build scene** if not already on the active scene  
- [ ] Add a visible **Wisdom Tree Event** (camera focus or tree highlight when quiz opens)  
- [ ] Rewrite quiz answers to match **chosen virtue set** (proposal Peace/Watermelon vs current Respect)  
- [ ] Confirm Golden Harvest feedback is obvious in UI (banner / meter glow)  
- [ ] Playtest: finish Level 5 → quiz → correct → 2× vibrancy for 30s  

### Stage B — Data alignment (proposal vs live) — **0.5–1 day**

Pick one and stick to it:

**Option 1 (match proposal):** rename virtues → Love, Joy, Peace, Patience, Meekness, Self-Control, Kindness, Goodness, Faithfulness  

**Option 2 (keep current):** update proposal docs / quiz copy to Kindness, Gratitude, Respect, etc.

Also decide:

- [ ] Add `healthStat` + short dialogue lines to `FruitData` / `VirtueDefinition`  
- [ ] Show dialogue or emoji symptom (frustrated / anxious) as in proposal, not only fruit icon  

### Stage C — Stickman polish (Phase 6.1) — **3–5 days**

- [ ] Shader-based color restore on Stickman (reuse `GreyToColor` ideas)  
- [ ] Per-fruit heal VFX map (proposal): hearts, sun flares, wave rings, bubbles, mist, synapses, pastel dust, trails, crystal shards  
- [ ] Stronger Celebrate + heal SFX  
- [ ] Optional text ThoughtBubble for “healing dialogue”  
- [ ] Ensure Idle_Sad reads clearly grey/sad on device  

### Stage D — Island & loop polish (Phase 6.2) — **3–5 days**

- [ ] Hook `IslandGrowthController.OnLevelCompleted` from `GameState` / `LevelManager`  
- [ ] Unlock orchard zones / props as vibrancy rises  
- [ ] Prop squash/stretch on interact or ambient  
- [ ] Gesture-specific haptics (pulse Dual Tap, rumble Long Press, short burst Swipe)  
- [ ] Music tempo / energy ramp with level difficulty  
- [ ] Authored harvest VFX (not only procedural bursts)  

### Stage E — Content & onboarding (Phase 6.3) — **2–4 days**

- [ ] Level 1 strawberry-only constraint (tutorials) if desired by `cehck.md`  
- [ ] Wire `OnboardingManager` + GuideCharacter into main play scene  
- [ ] Levels 6+ only if product wants post-quiz content  
- [ ] Edit-mode decoration rewards polish  

### Stage F — Mobile optimization (Phase 7) — **3–5 days**

- [ ] Profile Android / iOS build (CPU, draw calls, GC)  
- [ ] Pooling audit (Stickman, tonic, VFX, fruit)  
- [ ] LOD / shadow / Adaptive Performance pass  
- [ ] Orientation, safe area, low-end device pass  

### Stage G — Flutter (Phase 8) — **later**

- [ ] Keep Unity vertical slice stable first (per `cehck.md`)  
- [ ] Expand `FlutterBridge` only after Phase 6–7 sign-off  

---

## 6. Suggested Milestone Order (next 2–3 weeks)

```
Week 1
  ├─ Stage A: Quiz + Wisdom Tree event polished
  ├─ Stage B: Virtue naming decision + data pass
  └─ Stage C start: Stickman color lerp + 3 sample heal VFX

Week 2
  ├─ Stage C finish: all 9 heal VFX + Celebrate polish
  ├─ Stage D: IslandGrowth wire + haptics + music ramp
  └─ Onboarding wired in build scene

Week 3
  ├─ Stage E: L1 tutorial constraint + content polish
  ├─ Device playtest + bugfix
  └─ Stage F start: performance profile
```

---

## 7. Risk / Architecture Notes

| Risk | Impact | Mitigation |
|------|--------|------------|
| Two virtue naming schemes | Quizzes & teaching copy confuse players | Decide Stage B before more content |
| Dual NPC paths (`StickmanController` vs legacy `NPCController`) | Dead code / confusion | Prefer Stickman loop; deprecate legacy |
| `IslandGrowthController` never called | Level-up feels flat | Wire in Stage D |
| Color = material swap | Feels abrupt vs proposal | Shader lerp in Stage C |
| Per-virtue health VFX missing | Proposal “matrix” incomplete | Stage C VFX map |
| Flutter early | Scope creep | Block until Phase 6–7 |

---

## 8. Quick Reference — Key Files

| Area | Path |
|------|------|
| Stickman logic | `Assets/Scripts/Core/StickmanController.cs` |
| Stickman prefab | `Assets/GameData/Prefabs/Stickman.prefab` |
| Animators | `Assets/GameData/Animators/StickmanState*.controller` |
| Gestures | `Assets/Scripts/Core/GestureManager.cs` |
| Harvest | `Assets/Scripts/Interaction/FruitHarvester.cs` |
| Fruits | `Assets/GameData/FruitData/FruitData_*.asset` |
| Virtues | `Assets/GameData/Virtues/Virtue_*.asset` |
| Levels | `Assets/GameData/Levels/Level_0N.asset` |
| Quiz | `Assets/Scripts/Core/QuizManager.cs`, `Assets/GameData/Quiz/` |
| Vibrancy | `Assets/Scripts/UI/VibrancyMeterUI.cs`, `ColorRestorationDriver.cs` |
| Island growth (unwired) | `Assets/Scripts/IslandGrowthController.cs` |
| Master feature list | `cehck.md` |

---

## 9. One-line Status for Stakeholders

**Core caretaker game (grey Stickman → gesture harvest → tonic → color → vibrancy → 5 levels) is playable. Quiz exists but needs Wisdom Tree presentation polish. Polish, proposal-accurate virtue/VFX matrix, island growth, and mobile optimization are the remaining phases — Flutter is last.**
