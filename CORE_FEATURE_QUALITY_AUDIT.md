# Core Feature Quality Audit

**Date:** 23 Sep 2026  
**Question answered:** Are the “done” core features actually working well / perfect?  
**Answer in one line:** **No — the core loop is playable and reasonably engineered, but it is not perfect. Treat it as a solid prototype (~6.5–7/10), not shipping quality.**

> **Update (Stickman anims):** Distinct animation types are now wired — see section at bottom of this file / `StickmanController` + `StickmanState.controller`. Idle_Sad ≠ Leaving; Celebrate has Happy/Thankful variants; Idle↔Walk Speed transitions fixed.

Play / test in: **`Assets/newmvp.unity`** (best wired).  
Build settings currently point at: **`Assets/The Main RadiantOrchard_3d.unity`** (different scene — risk).

---

## Overall grades (working quality, not “exists”)

| Feature | Exists? | Works in play? | Quality | Grade |
|---------|---------|----------------|---------|------:|
| Spawn Stickman + fruit | Yes | Yes (if spawner wired) | Good | **B** |
| Symptom bubble | Yes | Yes | OK (icon only) | **B-** |
| Double Tap harvest | Yes | Yes in Editor | Good | **B** |
| Long Press harvest | Yes | **Fragile on phone** | Weak | **C** |
| Fast Swipe harvest | Yes | **OK but imprecise** | Weak | **C+** |
| Tonic flight → heal | Yes | Yes | Prototype look | **B-** |
| Grey → color Stickman | Yes | Yes (instant swap) | Looks cheap | **C+** |
| Celebrate animation | Yes | Yes | Depends on clip quality | **B-** |
| Vibrancy meter | Yes | Yes | Functional | **B** |
| Level 1→5 advance | Yes | **Mostly** (see gaps) | Workaround-heavy | **C+** |
| Wisdom Tree quiz | Code yes | **Only at Level 5** | Wrong content vs proposal | **C** |
| Wrong-gesture feedback | No | Silent fail | Bad UX | **D** |
| Haptics / audio / VFX | Partial | Basic placeholders | Not polished | **C-** |

**Core loop verdict:** Working enough to demo. **Not** “everything perfect.”

---

## What actually works well

These parts are real systems with thoughtful edge-case handling (pooling, orphan Stickmen, fruit spacing history, save vibrancy reset). Code quality here is **above typical jam level**.

1. **Heal pipeline is coherent**  
   `NPCSpawner` → `FruitHarvester` → `TonicFlight` → `StickmanController.ReceiveTonic` → `GameState.AddVibrancy` → UI updates.

2. **Harvest safety**  
   Won’t spend a fruit on a Stickman that already celebrated / left (`IsHarvestable`). Pool double-push guarded.

3. **Editor testing**  
   Mouse maps to DoubleTap / LongPress / FastSwipe; middle-click = instant DoubleTap.

4. **Stickman state machine**  
   Idle_Sad → Receiving → Celebrate → Roaming / Leaving is clear and documented. Walk speed synced to NavMesh to reduce foot-slide.

5. **Vibrancy / level gate**  
   Levels complete primarily by reaching `requiredVibrancy`. Meter resets on new level (important bug was already fixed in `GameState.StartLevel`).

---

## What is NOT perfect (real issues)

### P0 — Will feel broken / wrong to players

| # | Issue | Why it matters | Where |
|---|--------|----------------|-------|
| 1 | **Wrong gesture = silence** | Player double-taps a LongPress fruit → nothing. No shake, no “wrong gesture” hint. Feels like “harvesting is broken.” | `FruitHarvester.TryHarvest` early-return |
| 2 | **Long Press only fires on `Stationary` touch** | Real fingers wobble → phase becomes `Moved` → Long Press **never fires**. Lemon / Grapes / Apple become hard on device. | `GestureManager.HandleTouch` |
| 3 | **Build scene ≠ best scene** | Player/build may open `The Main RadiantOrchard_3d.unity` while the clean loop is in `newmvp.unity`. “Works for me / doesn’t work” confusion. | `EditorBuildSettings.asset` |
| 4 | **Quiz is not the proposal quiz** | Proposal: “anxious friend → which fruit?” Current bank: generic kindness/courage ethics. Golden Harvest still works, teaching loop does not match design. | `QuizDatabase.asset` |

### P1 — Works, but feels unfinished

| # | Issue | Detail |
|---|--------|--------|
| 5 | Stickman color is **instant material swap** | Not a heal wash / shader lerp. Looks abrupt vs proposal. |
| 6 | Tonic is a **tiny colored sphere** | Not a “3D vital tonic bottle.” Trail helps; still placeholder. |
| 7 | VFX are **procedural bursts only** | Same burst style for all fruits — no heart / sun / wave per virtue. |
| 8 | Haptics = one `Handheld.Vibrate()` | Not pulse vs rumble per gesture. |
| 9 | Audio is **generated placeholder SFX** | Fine for prototype; not product. |
| 10 | Fast Swipe hit-test uses **swipe start only** | Swiping *onto* the fruit often fails; must start near fruit. |
| 11 | Symptom shows **fruit icon**, not frustrated/anxious emoji | Readable if you know the map; weaker teaching metaphor. |
| 12 | **`VibrancyObjectiveDriver` missing from `newmvp`** | Objectives rely on LevelManager **backfill** when vibrancy hits threshold. Levels can finish, but “help N NPCs” progress is fake/late. |

### P2 — Code / architecture smells (not always visible)

| # | Issue | Detail |
|---|--------|--------|
| 13 | No timeout on **`Receiving` state** | If tonic coroutine dies, Stickman can sit forever in Receiving (Update has no `case Receiving`). |
| 14 | Dual NPC paths | Stickman loop + legacy `NPCController` / ThoughtBubble still in project. |
| 15 | Fruit placement comments conflict | Header comments say “orchard path far away”; `SpawnFruitFor` now puts fruit **beside** Stickman. Current code wins (better UX), docs lie. |
| 16 | Virtue names ≠ proposal | Teaching content can confuse stakeholders who read Love/Joy/Peace docs. |

---

## Feature-by-feature working checklist

### A. Core loop (must be demoable)

| Step | Working? | Perfect? |
|------|----------|----------|
| Grey Stickman arrives | Yes | No (spawn pacing / clarity can confuse) |
| Bubble shows need | Yes | No (fruit icon ≠ symptom emoji) |
| Correct gesture harvests | Usually | No (Long Press fragile; wrong gesture silent) |
| Tonic flies | Yes | No (placeholder sphere) |
| Stickman colors + celebrates | Yes | No (swap + generic VFX) |
| Vibrancy goes up | Yes | Mostly |
| Next visitor appears | Yes | Yes enough |

**Demo tip:** In Editor, use **middle-click** on DoubleTap fruits to avoid gesture frustration while testing the rest of the loop.

### B. Stickman

| Item | Working? | Perfect? |
|------|----------|----------|
| Idle_Sad | Yes | Depends on anim clip |
| Receiving | Yes | No fail-safe timeout |
| Celebrate | Yes | Needs stronger VFX |
| Roaming walk | Yes | Good code; art TBD |
| Leave on patience | Yes | Leaving anim is still sad-idle based |
| Grey/color mats | Must be assigned on prefab | Instant swap only |

### C. Levels

| Item | Working? | Perfect? |
|------|----------|----------|
| L1–L5 data assets | Yes | Yes enough |
| Vibrancy gate | Yes | Yes |
| Objective progress | Soft / backfilled | Not honest tracking without driver |
| Level complete SFX / popup | Present in code | Scene wiring must be verified in play |

### D. Quiz

| Item | Working? | Perfect? |
|------|----------|----------|
| Fires every 5 levels | Yes (Level 5) | Only once in current 5-level game |
| UI panel | Code present | Needs play verify of refs |
| Golden Harvest 2× | Yes in code | Needs visible HUD feedback |
| Matches proposal questions | **No** | — |

---

## Honest scorecard

| Question | Answer |
|----------|--------|
| Is the core loop **implemented**? | **Yes** |
| Does it **run end-to-end** in `newmvp`? | **Yes, if managers stay wired** |
| Is it **reliable on a real phone**? | **Risky** (Long Press + silent wrong gesture) |
| Is it **proposal-perfect**? | **No** |
| Is code quality **bad**? | **No** — core scripts are careful and documented |
| Is it **shipping / “perfect”**? | **No** |

```
Existence / architecture ..........  8 / 10
Playable core loop ................  7 / 10
Mobile gesture reliability ........  5 / 10
Feel / polish (VFX audio haptics) .  4 / 10
Proposal fidelity (quiz/virtues) ..  4 / 10
Overall product readiness .........  5.5 / 10
```

---

## What to fix first if you want “perfect core”

Do these before calling Phase 2–4 “done for real”:

1. **Long Press:** fire while held even if finger moves slightly (Moved phase + distance tolerance).  
2. **Wrong gesture feedback:** bounce fruit + short fail SFX / haptic.  
3. **Set build scene to `newmvp`** (or fully parity-wire Main).  
4. **Add `VibrancyObjectiveDriver` to play scene.**  
5. **Receiving timeout** (e.g. 3s → return Idle_Sad if tonic never arrives).  
6. **Rewrite quiz** to fruit/symptom questions from the proposal.  
7. Then polish: color lerp, tonic bottle, per-fruit VFX.

---

## How to verify yourself (10-minute playtest)

Open **`newmvp.unity`** → Play:

1. Wait ~few seconds → Stickman + fruit appear.  
2. Middle-click fruit (Editor) → tonic → color + celebrate → bar moves.  
3. Try Long Press fruit with slight mouse wobble → note if it fails.  
4. Wrong gesture on purpose → confirm silent fail (bug).  
5. Heal until vibrancy hits Level 1 threshold → level should advance.  
6. Reach Level 5 complete → quiz panel should appear.

If step 1 fails: check Console for `NPCSpawner` / `GestureManager` warnings.

---

## Bottom line for you

Earlier “95% / 100%” meant **systems exist and are wired on paper**.  

**Working quality:** the core loop is a **good playable prototype**, not a perfect finished feature set.  
Highest risk: **mobile Long Press**, **silent failed harvests**, **wrong build scene**, **quiz ≠ proposal**.

Fix the P0 list above before you trust any “phase complete” claim.
