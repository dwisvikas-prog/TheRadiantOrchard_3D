# E.1–E.6 Follow-up Audit (Read-Only)

Follow-up pass on the 6 files flagged in `Forensic_Verification_Report.md` section E. Read-only — no files modified. Builds directly on the P0/P1/P2 breakdown and narrows down the exact mechanism behind P0.1.

---

## 1. `Assets/Scripts/UI/EmptyIslandCoachBar.cs` — root cause of P0.1 pinpointed

`EmptyIslandPhaseRunner.ShowPhaseToast()` always calls `SetTip(tip, withOk: true, …)`, so the dismiss normally only fires via `OnOkClicked()` (button tap) — `AutoHideBubble` (the no-button auto-hide path) is **not** in play for phase toasts, since `withOk=true` disables it (`Show()`, lines 298-299). That narrows the bug, but doesn't remove it:

- `Hide()` (lines 75-84) and `GoIdleQuiet()` (lines 87-96) both do `currentOnOk = null` **without invoking it**. Any other system calling `EmptyIslandCoachBar.Hide()` / `GoIdleQuiet()` / `SetTip()` while a phase toast's OK button is still waiting for a tap **silently drops the pending callback** → `BlocksWorldInput` stays `true` forever, exactly as report section D.1 predicted.
- Concrete trigger found: `QuietFruitAssist.Begin()` line 72 calls `EmptyIslandCoachBar.GoIdleQuiet()`. If that call ever races against an unresolved phase toast (toast shown, player hasn't tapped OK yet, and a quiet-fruit-assist flow starts in parallel), it's a real soft-lock, not just theoretical.
- `EmptyIslandGuideStack.AllowsCoachTip` (line 47 of that file) is hardcoded `return true` — the entire `pendingTip` / `FlushPending()` queueing mechanism inside `EmptyIslandCoachBar` (`SetTip()` lines 60-67, `FlushPending()` lines 98-110) is dead code, never actually gated by anything.

**Conclusion:** the real fix surface for P0.1 is not "coach-bar dismiss missing" broadly — it's specifically **`Hide()`/`GoIdleQuiet()` clearing `currentOnOk` without invoking it**, plus identifying every call site (confirmed: `QuietFruitAssist.Begin`) that can call those methods while a phase toast's callback is still pending.

---

## 2. `Assets/Scripts/Core/FirstWishTreePlant.cs` — confirms D.6, no new soft-lock

Two independent tree-creation paths exist as the forensic report predicted:
- `AutoPlantSequence()` (lines 74-131) — full growth: sapling → 10s timer → pop-in tree.
- `EnsureTreeVisible()` (lines 45-55) — instant show-on-resume, used when `IsPlanted` is already true from a previous session.

Growth here is real and complete — this is the **only** script in the codebase with actual progressive seed→grown visuals, which confirms P1.1's premise: fruit trees don't get this treatment anywhere, but the capability/pattern to build it already exists in this file.

No soft-lock risk found: `FinishStep()` (lines 146-156) always calls `NotifyWishTreePlanted()` (or `OnCompleted` as a fallback), and there's no blocking wait without a callback path.

---

## 3. `Assets/Scripts/Core/EmptyIslandProgress.cs` — confirms D.7, clean

Pure PlayerPrefs key registry/facade. `ClearAll()` (lines 12-22) deletes 6 keys, including `FirstWishTreePlant.PrefsKey`. No desync risk found — every key is owned by exactly one class (`EmptyIslandPhaseRunner`, `WellWishController`, `FirstPlantTutorial`, `FirstWishTreePlant`, `EmptyIslandNextGuide`), and this file only re-exposes them. Not a soft-lock contributor.

---

## 4. `Assets/Scripts/Core/QuietFruitAssist.cs` — mirrors `FruitPlantLessonFlow`'s exact risk (extends D.3)

Same shape as `FruitPlantLessonFlow`:
- `IsActive` (line 16) stays true for as long as a `QuietFruitAssist` instance exists.
- The only teardown path besides a successful harvest (`OnHarvested()`, lines 92-103) is `CancelFor()` (lines 19-33), called exclusively from `StickmanController.GiveUp()` on patience timeout.
- No harvest timeout exists here either (same gap as P0.3), specifically for **already-taught** fruits (fruits 2-9 on repeat visits, once a fruit no longer needs the full lesson bubble).

This means P0.2/P0.3 fixes should cover both `FruitPlantLessonFlow` and `QuietFruitAssist` — they share the identical failure mode and the identical missing-timeout gap.

---

## 5. `Assets/Scripts/UI/EmptyIslandGuideStack.cs` — smaller than the name implies, confirms a real gap

Despite the name, this is **not** an actual stack — it's a single static `Layer current` field (line 21):
- `Push(layer)` (lines 25-37) unconditionally overwrites `current` — no stack push, no history kept.
- `Pop(layer)` (lines 39-44) only resets `current` to `None` if it currently equals `layer`; otherwise it's a silent no-op.

If two systems `Push` different layers without popping in matching order, the second `Push` silently overwrites the first with nothing to restore it. Not a soft-lock by itself, but it confirms D.5's concern about ordering bugs being swallowed silently rather than surfaced.

---

## 6. `Assets/Scripts/Core/Level2Intro.cs` — resolves the C.2 "UNKNOWN"

`EmptyIslandLevelProgress.AdvanceToLevel2()` is called from exactly one place in the entire codebase: `Assets/Scripts/UI/Level1CompleteBanner.cs:59`, immediately followed by `Level2Intro.Show()` at line 60. This closes the open question from the forensic report — there is no ambiguity or second code path; Level 2 only ever starts through the Level 1 completion banner.

---

## Net effect on the P0/P1 plan

- **P0.1** is now a precise, small fix surface: patch `Hide()`/`GoIdleQuiet()` in `EmptyIslandCoachBar.cs` to invoke any pending `currentOnOk` before clearing it (or otherwise guarantee the phase-toast callback always fires), and verify `QuietFruitAssist.Begin()`'s call to `GoIdleQuiet()` can't race an unresolved toast. Not a coach-bar rewrite.
- **P0.2 / P0.3** should be scoped to cover **both** `FruitPlantLessonFlow.cs` and `QuietFruitAssist.cs` — they share the identical `IsBusy`/`IsActive` stuck-true risk and the identical missing-harvest-timeout gap.
- **P1.1** (visible fruit growth) has a ready template to copy from: `FirstWishTreePlant.AutoPlantSequence()`'s sapling→timer→pop-in sequence is the only real growth implementation in the project.
- No new P0-level soft-locks were found beyond what the main forensic report already flagged — this pass narrowed mechanisms, it didn't expand scope.

Still read-only — no project files modified in this pass.

---

## P0 Fixes Applied (2026-10-01)

All three P0 items from this audit have been implemented as a targeted stability pass. No refactor, no P1/P2 scope.

### P0.1 — Coach bar / input soft-lock — FIXED
`Assets/Scripts/UI/EmptyIslandCoachBar.cs`: `Hide()` and `GoIdleQuiet()` merged into one `CloseAndResolvePending()` path that captures `currentOnOk`, nulls it first (reentrancy-safe), then invokes it after the hide/idle-motion work. A pending phase-toast callback (or any other `withOk` callback) can no longer be silently discarded, so `BlocksWorldInput` can no longer be stuck `true` forever. `OnOkClicked()` behavior is unchanged — still fires its callback exactly once.

### P0.2 — Stuck FruitPlantLessonFlow / QuietFruitAssist state — FIXED
`Assets/Scripts/Core/FruitPlantLessonFlow.cs` and `Assets/Scripts/Core/QuietFruitAssist.cs`: the inline cleanup in each `CancelFor()` was extracted into an idempotent `Teardown()` (guarded by a `torndown` flag — safe to call twice, no duplicate harvest/heal/Vibrancy/despawn). `Assets/Scripts/Core/StickmanController.cs`'s `Despawn()` now also calls `FruitPlantLessonFlow.CancelFor(this)` as a safety net, so `IsBusy`/`IsActive` can't stay stuck no matter which path removes the visitor (not just `GiveUp()`).

### P0.3 — Harvest wait fallback — FIXED
Both `FruitPlantLessonFlow` (step 2) and `QuietFruitAssist` now start a 90-second fallback coroutine when they start waiting on a harvest event — matching `FirstPlantTutorial`'s existing 90s timeout convention. On expiry it calls the same `Teardown()` (no harvest awarded, no heal, no Vibrancy) so the player/visitor state is safely recovered instead of waiting forever.

**Verification status:** static code review only — no Unity Editor/Play Mode compile or runtime test was available this session. Full before/after detail and the regression-flow trace (A–G) are in the session report; not duplicated here.

**Not changed:** no scenes, prefabs, fruit mappings, gesture requirements, Vibrancy/patience values, UI design, shop/decoration/NPC/tonic behavior. No P1/P2 work started.
