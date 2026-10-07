# THE RADIANT ORCHARD — COMPLETE EXISTING PROJECT COMPLIANCE AUDIT

IMPORTANT:
DO NOT MODIFY ANYTHING.

Do NOT create files.
Do NOT delete files.
Do NOT rename anything.
Do NOT refactor anything.
Do NOT fix anything.
Do NOT generate code.

This task is ONLY a READ-ONLY forensic audit of the EXISTING Unity project.

The goal is to determine:

"Does the current Unity project and current scene actually match the requirements in the The Radiant Orchard Master Specification?"

I want you to inspect the EXISTING project deeply before we build anything else.

==================================================
1. MASTER SPECIFICATION
   ==================================================

Use the existing The Radiant Orchard master specification/check.md as the source of truth.

Do not assume that something is implemented just because a script or GameObject exists.

Verify actual functionality and references wherever possible.

==================================================
2. INSPECT THE ENTIRE PROJECT
   ==================================================

Perform a comprehensive audit of the project.

Inspect all relevant:

- Scenes
- Scripts
- Prefabs
- GameObjects
- Components
- Materials
- Shaders
- Shader Graphs
- Textures
- Models
- Animations
- Animator Controllers
- Particle Systems
- VFX
- Audio
- UI
- ScriptableObjects
- Data assets
- Packages
- Project Settings
- Input System
- Render Pipeline
- Lighting settings
- Build settings
- Mobile settings

Do NOT just inspect filenames.

Where possible, inspect the actual contents, references and relationships.

==================================================
3. CURRENT SCENE AUDIT
   ==================================================

Perform a complete hierarchy audit of the currently relevant gameplay scene.

For EVERY important GameObject, determine:

- Name
- Parent
- Active/inactive
- Components
- Scripts attached
- Prefab source
- Missing references
- Materials
- Collider
- Animator
- Relevant configuration
- Whether it is actually used

Pay special attention to:

- Island
- Ground
- Cliffs
- Water
- Stone Well
- Wisdom Tree
- NPCSpawner
- NPC spawn points
- Visitor path
- Planting spots
- Fruit trees
- UI Canvas
- Camera
- Lighting
- GameManagers
- VFX
- Audio

Detect duplicate systems and duplicate GameObjects.

==================================================
4. SCRIPT AUDIT
   ==================================================

Inspect ALL project scripts relevant to the game.

For each relevant script report:

- File path
- Class name
- Purpose
- Who references it
- What references it
- Whether it is actually attached/used
- Dependencies
- Important public fields
- Missing references
- Duplicate responsibilities
- Potential architectural problems
- Whether it matches the master specification

Do not only list scripts.

Explain what each system actually does.

Identify dead/orphan scripts that appear unused.

==================================================
5. PREFAB AUDIT
   ==================================================

Inspect relevant prefabs.

For each important prefab:

- What it represents
- Components
- Scripts
- Materials
- Animators
- Colliders
- Child objects
- Missing references
- Whether it is actually used in the current scene

Check for:

- broken prefab references
- missing scripts
- duplicate prefabs
- scene objects that should be prefabs
- prefabs that are not actually being used

==================================================
6. GAMEPLAY REQUIREMENT AUDIT
   ==================================================

Compare the current implementation against the complete gameplay specification.

Check each requirement individually.

Create a status for EVERY requirement:

IMPLEMENTED
PARTIALLY IMPLEMENTED
MISSING
BROKEN
UNKNOWN / CANNOT VERIFY

Do NOT mark something IMPLEMENTED merely because related files exist.

Verify actual implementation.

==================================================
7. CORE GAME LOOP
   ==================================================

Check whether this entire chain currently exists:

Visitor arrives
↓
Visitor has a request/symptom
↓
Thought bubble displays request
↓
Player identifies required fruit
↓
Player interacts with fruit tree
↓
Correct gesture is detected
↓
Fruit is harvested
↓
Fruit becomes Vital Tonic
↓
Tonic flies to visitor
↓
Visitor receives tonic
↓
Grey/desaturated visitor becomes colorful
↓
Celebration animation
↓
Vibrancy increases
↓
Next objective

For EVERY step say:

- Implemented
- Partial
- Missing
- Broken

And explain exactly what exists.

==================================================
8. NINE FRUIT SYSTEM
   ==================================================

Check the implementation of:

1. Strawberry / Love / Double Tap
2. Pineapple / Joy / Double Tap
3. Watermelon / Peace / Double Tap
4. Lemon / Patience / Long Press
5. Grapes / Meekness / Long Press
6. Apple / Self-Control / Long Press
7. Peach / Kindness / Fast Swipe
8. Banana / Goodness / Fast Swipe
9. Cherry / Faithfulness / Fast Swipe

Determine:

- Is there a reusable fruit system?
- Are fruits data-driven?
- Are gestures configurable?
- Are VFX configurable?
- Are fruits hard-coded or scalable?
- Can new fruits be added without duplicating gameplay code?

==================================================
9. NPC SYSTEM
   ==================================================

Check:

- NPC base model
- NPCSpawner
- spawn points
- movement
- path
- idle
- waiting
- sad/slouched state
- receiving state
- healing state
- celebration
- leaving
- Animator
- visitor data
- requested fruit
- patience timer
- multiple simultaneous visitors

Determine exactly what works and what doesn't.

==================================================
10. GREY → COLOR SYSTEM
    ==================================================

Check whether the specification's color restoration system exists.

Specifically inspect:

- Shader Graph
- material
- shader properties
- _ColorRestorationProgress or equivalent
- runtime control
- transition animation
- NPC material setup

Determine whether the system actually works at runtime or only exists as an unused shader/material.

==================================================
11. VIBRANCY SYSTEM
    ==================================================

Check:

- Vibrancy Manager/system
- global vibrancy value
- level relationship
- lighting relationship
- environment color relationship
- UI vibrancy meter
- reward increases
- persistence

Determine whether Vibrancy is actually connected to gameplay.

==================================================
12. LEVEL SYSTEM
    ==================================================

Check whether levels are properly data-driven.

Required progression:

Level 1
Level 2
Level 3
Level 4
Level 5
→ Quiz

Then:

Level 6–10
→ Quiz

etc.

Check:

- LevelManager
- LevelConfig
- ScriptableObjects/data
- visitor count
- fruit availability
- patience duration
- difficulty
- spawn timing
- rewards
- quiz trigger

IMPORTANT:

Determine whether levels are genuinely configurable or hard-coded.

==================================================
13. LEVEL-BY-LEVEL COMPLIANCE
    ==================================================

Compare current implementation against the intended first five levels:

LEVEL 1:
- tutorial
- one visitor
- basic fruit
- generous difficulty
- complete core loop

LEVEL 2:
- 1–2 visitors
- patience introduced
- multiple/simple requests

LEVEL 3:
- multiple visitors
- different fruit requests
- prioritization

LEVEL 4:
- 2–3 visitors
- multiple fruit types
- shorter patience
- increased pressure

LEVEL 5:
- multiple requests
- highest difficulty of first tier
- completion triggers Wisdom Tree Quiz

For each level report what actually exists.

==================================================
14. QUIZ SYSTEM
    ==================================================

Check:

- Wisdom Tree interaction
- Quiz UI
- bottom sheet
- question data
- answer options
- correct answer
- validation
- feedback
- explanation
- reward
- Golden Harvest Mode
- 2x points
- golden lighting
- particles
- Level 5 trigger
- every-5-level trigger

Determine whether the system is scalable to many questions.

==================================================
15. UI AUDIT
    ==================================================

Inspect:

- Canvas
- EventSystem
- HUD
- Vibrancy bar
- level counter
- thought bubbles
- symptom icons
- tutorial
- pause
- quiz bottom sheet
- reward UI
- mobile scaling

Check anchors, Canvas Scaler and references.

==================================================
16. VFX AUDIT
    ==================================================

Check:

- fruit harvest VFX
- 9 virtue VFX
- tonic flight trail
- healing effect
- grey-to-color effect
- celebration effect
- Golden Harvest particles
- vibrancy transition

Determine which are actually connected to gameplay.

==================================================
17. AUDIO / HAPTICS AUDIT
    ==================================================

Check:

- background music
- harvest sounds
- tonic sounds
- healing sound
- celebration
- quiz feedback
- Golden Harvest audio
- haptics/input feedback

Determine what exists and what is actually triggered.

==================================================
18. MOBILE PERFORMANCE AUDIT
    ==================================================

Inspect the project for obvious mobile risks:

- excessive polygon count
- excessive texture sizes
- unnecessary materials
- duplicate materials
- excessive particle counts
- expensive shaders
- excessive realtime lights
- unnecessary Update loops
- Instantiate/Destroy patterns
- missing pooling
- unnecessary allocations
- excessive Canvas rebuilds
- expensive post processing
- inappropriate quality settings

Do NOT optimize anything yet.

Only report findings.

==================================================
19. PROJECT SETTINGS AUDIT
    ==================================================

Check:

- Unity version
- URP/Built-in/HDRP
- target platform
- Android settings
- iOS settings
- scripting backend
- API compatibility
- input system
- graphics settings
- quality settings
- resolution
- orientation
- package dependencies
- build scenes

Report anything that may affect the intended mobile game.

==================================================
20. ARCHITECTURE AUDIT
    ==================================================

Determine whether the current architecture can scale to the full game.

Specifically look for:

- giant GameManager
- duplicated managers
- duplicate NPCSpawner
- hard-coded level logic
- hard-coded fruit logic
- duplicated scripts
- circular dependencies
- unnecessary singletons
- scene references that will break
- tightly coupled systems
- unused systems
- dead code
- systems that should be ScriptableObject/data-driven

Do NOT refactor anything.

==================================================
21. MASTER REQUIREMENT MATRIX
    ==================================================

At the end create a comprehensive table:

| Requirement | Current Implementation | Status | Evidence | Missing Work |
|-------------|-----------------------|--------|----------|--------------|

Use:

IMPLEMENTED
PARTIAL
MISSING
BROKEN
UNKNOWN

Do not use subjective ratings.

==================================================
22. FILE-BY-FILE SUMMARY
    ==================================================

Provide a concise list of relevant files:

File
Purpose
Used?
Related Requirement
Status
Problems

Do not dump irrelevant Unity-generated files.

Focus on project-owned files that affect the game.

==================================================
23. CURRENT STATE SUMMARY
    ==================================================

At the end provide:

A. What is already correctly built
B. What is partially built
C. What is completely missing
D. What is broken
E. What is duplicated
F. What should be preserved
G. What should eventually be fixed
H. What should NOT be touched
I. Recommended implementation order

==================================================
24. CRITICAL RULE
    ==================================================

DO NOT FIX ANYTHING.

DO NOT IMPLEMENT ANYTHING.

DO NOT REFACTOR ANYTHING.

This is a forensic audit only.

I specifically want to know whether the EXISTING project is actually compliant with the Master Specification before we continue development.

If you cannot verify something, explicitly say:

"UNKNOWN — could not verify"

Never assume.

Do not claim a feature is implemented merely because a class, prefab, or GameObject exists.

After completing the entire audit, STOP and wait for my next instruction.