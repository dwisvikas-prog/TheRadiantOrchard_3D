# The Radiant Orchard — Master Build Prompt
### Use this with Claude CLI inside your Unity project folder

Copy-paste this whole prompt into Claude CLI (running inside your Unity project root) one section at a time, in order. Each section is written to naturally trigger the right Unity skill.

---

## SECTION 1 — Project Foundation & Island Zones

```
I'm building "The Radiant Orchard" — a Unity 6 mobile game (URP) where a floating
island starts grey/desaturated and progressively grows and colors in as the player
completes levels.

Set up the scene structure:
- Create empty parent GameObjects for island zones: Zone_Well (always visible,
  Level 1), Zone_Orchard (unlocks Level 2), Zone_Waterfall (unlocks Level 4,
  reuses my existing water script), Zone_Hills (unlocks Level 6)
- Each zone should start at localScale 0 except Zone_Well
- Attach the IslandGrowthController script (already in Assets/Scripts) to a new
  "GameManagers" empty GameObject and wire up the chunk list in the Inspector
- Set up an isometric camera angle appropriate for a diorama-style mobile game
```
*(This triggers Unity scene-setup workflow; if you want NavMesh baked per zone, follow up with Section 2 immediately after.)*

---

## SECTION 2 — Stickman Navigation (triggers `initialize-ai-navigation`)

```
Set up Unity AI Navigation for the Stickman NPCs:
- Bake a NavMesh surface across all island zones (including currently-hidden
  ones so it's ready when they unlock)
- Add a NavMeshAgent to the Stickman prefab (radius suited to a low-poly
  humanoid under 1500 tris)
- Set up a NavMesh link between Zone_Well and Zone_Orchard so Stickmen can
  path between them once both are unlocked
- When a new zone unlocks via IslandGrowthController, trigger a NavMesh rebake
  for that zone only (not the whole island, for performance)
```

---

## SECTION 3 — Grey-to-Color Shader (triggers `shader-graph-create-custom-node`)

```
Create a Shader Graph URP Lit shader called "GreyToColorRestore" with:
- A float parameter _ColorRestorationProgress (0-1), exposed for scripting
- A Lerp node blending between a fully desaturated (grayscale) sample of the
  base texture and the full-color base texture, driven by that float
- Apply this shader to the Stickman material and to each island zone's
  terrain/prop materials
- The ColorRestorationController.cs script (already in Assets/Scripts) drives
  this param via Shader.PropertyToID("_ColorRestorationProgress") — make sure
  the exposed name matches exactly
```

---

## SECTION 4 — Touch Gesture & Harvest UI (triggers `ui` skill)

```
Build the mobile touch UI for fruit harvesting:
- A top HUD bar showing the Vibrancy Meter (UI Slider or Image fill)
- 3D world-space thought bubble prefab above each Stickman showing a symptom
  icon, using a Canvas set to World Space render mode
- Detect whether this project uses UGUI or UI Toolkit before building, and
  match the existing convention
- Wire the GestureDetector.cs script (already in Assets/Scripts) to fire on
  FruitTree objects when tapped/held/swiped within their collider bounds
```

---

## SECTION 5 — Fruit Sprite/Texture Atlas (triggers `manage-sprite-atlas`)

```
Set up a SpriteAtlas for the 9 virtue-fruit icons (Strawberry, Pineapple,
Watermelon, Lemon, Grapes, Apple, Peach, Banana, Cherry) used in the symptom
bubble UI. Use the prebuild IPreprocessBuildWithReport approach. Configure for
mobile texture compression (ASTC for both platforms).
```

---

## SECTION 6 — Collision Debugging (use if harvest doesn't register)

```
My FruitTree harvest gesture isn't triggering OnTriggerEnter when the player
taps/swipes over it in the 3D scene. Diagnose using standard 3D PhysX checks —
collider setup, rigidbody requirements, layer masks.
```

---

## SECTION 7 — Wire Everything Together

```
Connect the full gameplay loop in GameManager.cs (already in Assets/Scripts):
1. GestureDetector fires on a FruitTree → correct gesture harvests the fruit
2. FruitTree spawns a TonicFlight prefab that arcs to the requesting Stickman
3. On arrival, StickmanController.Heal() runs the color-restore shader lerp
   and joyful animation
4. VibrancyMeter increments; at threshold, GameManager calls
   IslandGrowthController.OnLevelCompleted(nextLevel)
Test this end-to-end in Play mode with one Zone_Orchard fruit tree and one
spawned Stickman.
```

---

## Notes for whoever runs this
- Sections 1–5 each map to a distinct Unity skill (scene setup, navigation,
  shaders, UI, sprite atlas) so Claude CLI will route correctly if run inside
  the Unity project with these skills enabled.
- Run sections in order — Section 7 depends on 1–4 being done first.
- The C# scripts referenced above are provided separately in `/Scripts` —
  drop them into `Assets/Scripts/` before running Section 1.
