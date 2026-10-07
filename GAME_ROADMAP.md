# The Radiant Orchard — Full Game Roadmap
### CoC-style Progressive Build & Addiction Loop Plan
Scene: `CoC_BlankGround.unity`

---

## 1. Core Vision

CoC addictive kyu hai? Kyunki har session me player ke paas **teen cheezein hoti hain karne ko**:
1. **Kuch collect karna** (resources)
2. **Kuch build/upgrade karna** (progress dikhta hai)
3. **Kuch decide karna** (strategy — kya pehle karu)

Abhi humara game sirf "collect" pe based hai (fruit → heal). Roadmap ka goal: **build** aur **decide** wale hisse add karna, bina current core loop (mood → fruit → gesture → heal) ko tode.

---

## 2. Master Structure (Level-by-Level)

```
Level 1  → LEARN        (sab 9 fruits sikho)
Level 2  → BUILD        (decorate + Shop unlock)
Level 3  → SPEED        (timed pressure, multi-visitor)
Level 4  → EXPAND       (naya island-area unlock)
Level 5  → MASTERY      (quiz, bonus multiplier, prestige feel)
Level 6+ → LOOP         (Levels 3-5 ka mix, harder har baar)
```

Har level do cheezein deta hai: **naya unlock** (reward) + **naya constraint** (challenge). Yehi CoC ka formula hai.

---

## 3. LEVEL 1 — Learning Phase ✅ (mostly built)

**Goal:** Player saare 9 fruits seekhe — mood → fruit → gesture → heal.

### Kya hona chahiye (point-wise)
1. Island grey/empty se shuru — sirf Wish Tree + Stone Well
2. Wish Tree plant → Well pe wish (coin drop, real animation)
3. Pehla visitor (Strawberry) — guided, full teach
4. Har naya fruit visitor = full teach (soil tap → tree plant → real fruit grow → gesture glow/pulse → harvest → tonic → heal)
5. **Strictly sequential** — agla visitor tab hi aaye jab pehla poora khatam ho
6. Bottom tray — sirf unlocked fruits, jiska turn hai wo blink kare
7. Top "?" button — gesture legend (kaunsa fruit, kaunsa gesture)
8. Tap tree → info card (real image + virtue + benefits)
9. 9/9 fruits taught → **"Level 1 Complete!"** banner → Level 2 unlock

### Status
✅ Sab kuch upar wala ban chuka hai is conversation me. Baaki: playtest + polish.

### Missing / decide karna hai
- [ ] Kya "100 points" wala literal score chahiye, ya "9/9 fruits" hi completion trigger rahega? (abhi 9/9 hai)
- [ ] Level 1 me koi failure state ho ya nahi (abhi patience timeout pe sirf vibrancy girta hai)

---

## 4. LEVEL 2 — Build Phase ✅ (built)

**Goal:** Player decorate karna sikhe, island apna banaye.

### Kya hai
1. "Level 1 Complete" ke baad turant Level 2 intro (girl guide batati hai)
2. **Decoration goal**: Shop se 3 items place karo → +15 vibrancy bonus
3. Shop button top-right, real 3D snapshot icons (asli asset dikhte hain, color swatch nahi)
4. Edit mode — koi bhi tree/decoration drag karke move kar sakte ho, no-overlap check
5. Patience timeout ab **vibrancy penalty** deta hai (stakes/consequence — pehli baar real tension)
6. Visitors ab bhi sequential aate hain (overlap hata diya gaya — user ne mana kiya)

### Missing / next
- [ ] Decoration categories expand (abhi Grass/Bush/Rock/Boulder/Pine/4 fruit-trees) — koi CoC-style "wall/gate/flag" category add karna?
- [ ] Decoration ko **currency se kharidna** (abhi free hai) — spend-loop banana ya nahi, decide karo

---

## 5. LEVEL 3 — Speed Phase (NOT built yet)

**Goal:** Pehli baar real time-pressure + decision-making.

### Concept
1. **Golden Harvest Rush** — har 3-4 minute me 30-45 sec ka bonus window trigger hota hai. Us window me heal karne se **2x vibrancy**
2. **2 visitors ek saath** (controlled, rare — har 2-3 visitors ke baad 1 baar) — dono ki fruit alag ho, player choose kare pehle kisko heal kare
3. Wrong-order choice pe koi penalty nahi (abhi ke liye) — sirf ek naturally zyada patience wale ko chhod ke doosre ko pehle heal karne ka incentive (jisme patience kam bacha hai)
4. Screen pe ek chhota "2 visitors waiting" indicator (top banner jaisa already bana hai VisitorAlert wala)

### Implementation hints (jab banayenge)
- `EmptyIslandMoreVisitors.cs` me ek naya coroutine branch — Level 3+ pe occasionally 2nd visitor allow karo (controlled overlap, random ~20% chance)
- `GameState.StartVibrancyMultiplier(2f, 40f)` already exists (Golden Harvest ka base already code me hai!) — bas trigger wire karna hai

### Trigger karne ka tarika
- Timer-based (har X seconds ek window)
- Ya milestone-based (har 5 heals ke baad 1 window)

---

## 6. LEVEL 4 — Expand Phase (NOT built yet)

**Goal:** Island literally bada/naya dikhe — visible, satisfying progress.

### Concept
1. Target vibrancy (jaise 80%) tak pahunchne pe **naya zone unlock** — pad ka ek naya hissa "reveal" ho (fog/grey se color me badle, ya naya area camera me aaye)
2. Naye zone me: naya decoration category unlock (jo Level 2 Shop me nahi tha)
3. Ek naya landmark ban sakta hai (jaise "Second Well" ya "Garden Arch") jo sirf is level pe milta hai

### Kyu zaroori hai
CoC me Town Hall upgrade ke baad naya area/building type unlock hota hai — yehi feeling honi chahiye: "mai bada ho raha hoon", sirf number nahi.

---

## 7. LEVEL 5 — Mastery Phase (partially planned — proposal me tha)

**Goal:** Knowledge test + bonus reward — proposal ka "Wisdom Tree Micro-Quiz" concept.

### Concept
1. Har 5 levels ke baad, gameplay pause — Wisdom Tree ke paas ek scenario aata hai ("Dost pareshaan/anxious hai. Kaunsa fruit chahiye?")
2. Sahi answer → **Golden Harvest Mode** (already code me `StartVibrancyMultiplier` hai) — poore island pe warm glow filter + score double, limited time
3. Galat answer → koi bhi negative nahi, sirf dobara try karne ka option (learning-focused, punishing nahi)

### Status
- `QuizManager.cs`, `QuizPanelUI.cs` already project me hain (dusre scenes ke liye bane the) — CoC_BlankGround ke liye adapt karna padega

---

## 8. "Addictive banane" ke Core Principles (CoC se seekhe)

| CoC Principle | Radiant Orchard me kaise apply ho |
|---|---|
| **Session has a start & end goal** | Har Level ka clear target (9 fruits / 3 decor / X vibrancy) — mil chuka |
| **Something always "cooking"** | Golden Harvest timer, ya "next visitor in Xs" countdown dikhna chahiye |
| **Visible progress, not just numbers** | Island literally bada/colorful hona chahiye level ke sath (Level 4 concept) |
| **Small decisions matter** | Multi-visitor prioritization (Level 3) |
| **Collect → Spend loop** | Vibrancy ko currency banao — Shop items free na ho, kharch karke unlock ho |
| **Rare/special reward** | Golden Harvest, Wisdom Tree quiz bonus |
| **Social/comparison (baad me, optional)** | Screenshot-share button, "island score" jo share ho sake |

---

## 9. Suggested Build Order (priority)

1. ✅ Level 1 (done) — polish/test
2. ✅ Level 2 (done) — polish/test
3. **Level 3 — Golden Harvest timer** (sabse easy, base code already exists — `GameState.StartVibrancyMultiplier`)
4. **Level 3 — controlled 2-visitor overlap** (thoda risky, careful testing chahiye — pehle already ek baar hata chuke hain kyunki bug tha, is baar controlled/rare rakhna)
5. **Vibrancy → currency conversion** (Shop items ko cost dena)
6. **Level 4 — naya zone unlock** (visual/content work, art-heavy)
7. **Level 5 — Wisdom Tree quiz** (existing QuizManager adapt karna)

---

## 10. Open Questions (decide karna hai jab shuru karein)

- [ ] Level 3 ka "2 visitors" — kitna risky/frequent ho? (recommend: rare, ~1 in 4 visitors, sirf Level 3+ me)
- [ ] Currency system chahiye ya sab free rahe? (recommend: currency — sabse zyada "game" jaisa feel dega)
- [ ] Level 4 ka naya zone — kitna bada scope? (chhota start karo — ek naya 10x10 patch, sirf 1 baar)
- [ ] Quiz questions kaun likhega — content/copy chahiye hoga 9 virtues ke sahi scenario ke sath

---

*Is file ko update karte rehna jab koi level ban jaye — status checkbox tick karo, "Missing/next" section update karo.*
