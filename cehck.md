# THE RADIANT ORCHARD — MASTER UNITY DEVELOPMENT PROMPT

You are the lead Unity 6 game developer working on my mobile game:

THE RADIANT ORCHARD

IMPORTANT:
I am currently building this game in UNITY ONLY.

Do NOT integrate Flutter / flutter_unity_widget yet.
The Unity game must first become a fully playable vertical slice.
Flutter integration will happen later after the core Unity gameplay is stable.

I have also provided visual reference screenshots showing the general style I want:
- isometric mobile 3D diorama
- colorful stylized environment
- readable buildings/props
- cute low-poly/stylized characters
- fixed/isometric camera
- polished mobile-game presentation

Use these screenshots ONLY as visual/gameplay inspiration.
Do not copy copyrighted characters, logos, UI, names, assets, or exact designs.
All game assets must be original or properly licensed.

==================================================
1. GAME CONCEPT
   ==================================================

The Radiant Orchard is a stylized 3D mobile diorama game.

The player is the Caretaker of a floating island.

The island initially feels lonely and partially desaturated.

The player helps visiting characters by:
1. Understanding what they need
2. Growing/harvesting the required virtue fruit
3. Turning the fruit into a Vital Tonic
4. Delivering the tonic to the visitor
5. Restoring the visitor's colors
6. Increasing the island's Vibrancy

The game should feel:
- peaceful
- tactile
- satisfying
- visually rewarding
- simple to understand
- progressively more challenging
- optimized for mobile

==================================================
2. CORE GAMEPLAY LOOP
   ==================================================

The main loop is:

Visitor arrives
↓
Visitor displays a need/symptom
↓
Player identifies required virtue/fruit
↓
Player interacts with the correct fruit tree
↓
Fruit is harvested using a gesture
↓
Fruit transforms into Vital Tonic
↓
Tonic flies toward the visitor
↓
Visitor receives tonic
↓
Grey/desaturated visitor becomes colorful
↓
Celebration animation + VFX + sound
↓
Island Vibrancy increases
↓
Next visitor / next objective

This loop is the MOST IMPORTANT part of the game.

Build and test this loop before adding secondary features.

==================================================
3. NINE VIRTUES / FRUITS
   ==================================================

The game contains 9 virtue fruits:

Love
Fruit: Strawberry
Gesture: Double Tap
VFX: Red heart sparks

Joy
Fruit: Pineapple
Gesture: Double Tap
VFX: Golden sun flares

Peace
Fruit: Watermelon
Gesture: Double Tap
VFX: Teal wave rings

Patience
Fruit: Lemon
Gesture: Long Press
VFX: Bright yellow bubbles

Meekness
Fruit: Grapes
Gesture: Long Press
VFX: Soft purple mist

Self-Control
Fruit: Apple
Gesture: Long Press
VFX: Blue synapse sparks

Kindness
Fruit: Peach
Gesture: Fast Swipe
VFX: Peach pastel dust

Goodness
Fruit: Banana
Gesture: Fast Swipe
VFX: Yellow velocity trails

Faithfulness
Fruit: Cherry
Gesture: Fast Swipe
VFX: Ruby crystal shards

IMPORTANT:
Do NOT implement all 9 fruits immediately.

First implement ONLY:

Strawberry → Love → Double Tap

Once the complete gameplay loop works, use the same architecture/data system to add the other fruits.

==================================================
4. INITIAL ISLAND
   ==================================================

The starting island is a small floating 3D diorama.

It should contain:

- floating island/landmass
- grass/soil
- cliff sides
- surrounding water
- Stone Well
- Wisdom Tree
- small environmental props
- tree planting spots
- visitor path
- NPC spawn point

Initially the island should feel relatively quiet and desaturated.

DO NOT create a huge complicated map.

Start with a small graybox island.

Use Unity primitives/placeholders first if final assets are not available.

Example:

Island
├── Ground
├── Cliff
├── Water
├── StoneWell
├── WisdomTree
├── PlantingSpots
├── VisitorPath
└── NPCSpawnPoint

==================================================
5. CAMERA
   ==================================================

Use a fixed isometric-style camera.

Target:

- Orthographic camera
- readable 3D diorama
- slightly elevated angle
- island centered
- mobile-friendly framing
- no FPS camera
- no unnecessary camera movement

The camera should make the game immediately readable.

Build the camera early.

==================================================
6. ASSET WORKFLOW
   ==================================================

Do NOT spend the beginning of development creating perfect assets.

Use a 3-stage workflow:

STAGE 1:
Graybox / primitive assets

STAGE 2:
Replace important objects with stylized low-poly assets

STAGE 3:
Polish materials, lighting, VFX and animation

Required eventual assets:

ENVIRONMENT:
- Floating island
- Grass
- Soil
- Water
- Rocks
- Bushes
- Flowers
- Logs
- Bench
- Stone Well
- Wisdom Tree

GAMEPLAY:
- 9 fruit trees
- 9 fruit models
- Vital Tonic bottle
- thought bubble
- symptom icons

CHARACTER:
- 1 reusable stylized NPC base
- idle animation
- walking animation
- sad/slouched animation
- receiving animation
- celebration animation

The NPC should be reusable for many visitors.

Do NOT create 20 different character systems unnecessarily.

==================================================
7. NPC SYSTEM
   ==================================================

Create a reusable Visitor/NPC system.

NPC states should eventually support:

Idle
Walking
Waiting
ReceivingTonic
Healing
Celebrating
Leaving

Each visitor should have data such as:

- required virtue
- required fruit
- patience duration
- current patience
- spawn position
- destination
- healed state

Use a data-driven architecture.

Do NOT hard-code individual levels directly into VisitorController.

==================================================
8. FRUIT TREE SYSTEM
   ==================================================

Create a reusable FruitTree system.

Each tree should have configurable data:

- fruit type
- virtue
- harvest gesture
- harvest duration
- VFX
- sound
- cooldown
- fruit prefab

For the first prototype:

StrawberryTree
↓
Double Tap
↓
Harvest Strawberry

The same system must later support:

Pineapple
Watermelon
Lemon
Grapes
Apple
Peach
Banana
Cherry

Do NOT duplicate scripts for every fruit.

==================================================
9. HARVEST SYSTEM
   ==================================================

Create a reusable touch interaction system.

Flow:

Touch
↓
Raycast
↓
Detect interactable
↓
Identify FruitTree
↓
Check gesture
↓
Validate gesture
↓
Harvest
↓
Spawn fruit/VFX
↓
Create tonic

First implement:

DOUBLE TAP

Later add:

LONG PRESS

FAST SWIPE

Make the gesture system modular.

==================================================
10. VITAL TONIC
    ==================================================

When a fruit is successfully harvested:

Fruit
↓
small transformation effect
↓
Vital Tonic appears

Then the tonic should travel smoothly toward the requesting NPC.

Use a smooth curved trajectory.

Do NOT rely on uncontrolled Rigidbody physics for the main delivery animation.

The delivery should feel deliberate and satisfying.

Example:

Fruit
↓
✨
↓
Bottle
↓
╭──────╮
│      ╰──────→ NPC
╰──────╯

==================================================
11. COLOR RESTORATION
    ==================================================

This is one of the main visual features.

NPC begins:

DESATURATED / GREY

After receiving tonic:

GREY
↓
20%
↓
40%
↓
60%
↓
80%
↓
100% COLOR

Use Unity Shader Graph if appropriate.

Create a shader/material parameter such as:

_ColorRestorationProgress

Range:

0 → 1

The restoration should animate smoothly.

After restoration:

- celebration animation
- particles
- sound
- optional camera feedback
- vibrancy increase

==================================================
12. VIBRANCY SYSTEM
    ==================================================

Create a global Vibrancy system.

Example:

0%:
Very desaturated

25%:
Slight environmental color

50%:
More greenery/color

75%:
Strong colorful environment

100%:
Fully vibrant island

Vibrancy should influence visual presentation.

Keep this system modular so lighting, post-processing, shaders and environmental objects can react to the same value.

==================================================
13. LEVEL SYSTEM
    ==================================================

IMPORTANT:

Levels must be DATA-DRIVEN.

Do NOT hard-code:

if level == 1
if level == 2
if level == 3

Instead create something like:

LevelConfig

Possible fields:

- levelNumber
- visitorCount
- availableFruits
- patienceDuration
- requiredHeals
- spawnDelay
- difficulty
- isQuizLevel
- vibrancyReward
- unlocks

The exact values should be easy to modify in Unity Inspector.

==================================================
14. LEVEL PROGRESSION
    ==================================================

Initial 5-level progression:

LEVEL 1 — TUTORIAL

Goal:
Teach the entire basic gameplay loop.

- 1 visitor
- 1 fruit type
- Strawberry only
- generous/no meaningful timer
- tutorial guidance
- plant tree
- double tap harvest
- tonic delivery
- NPC color restoration
- celebration

Player should understand the game by the end of Level 1.

--------------------------------------------------

LEVEL 2 — PATIENCE INTRODUCTION

- 1–2 visitors
- introduce patience timer
- still relatively forgiving
- simple requests
- Strawberry/Pineapple can begin appearing

Main new mechanic:

PATIENT WAITING

Visitor has a patience meter.

--------------------------------------------------

LEVEL 3 — MULTIPLE REQUESTS

- approximately 2 visitors
- different fruit requests
- multiple fruit trees
- player must decide which visitor to handle first
- patience becomes more relevant

Main challenge:

MANAGING MULTIPLE REQUESTS

--------------------------------------------------

LEVEL 4 — MULTI-VISITOR PRESSURE

- approximately 2–3 visitors
- multiple fruit types
- shorter patience
- faster decision-making
- more simultaneous activity

Main challenge:

PRIORITIZATION + SPEED

--------------------------------------------------

LEVEL 5 — FINAL CHALLENGE BEFORE QUIZ

- approximately 3 visitors
- multiple fruit requests
- more pressure
- shorter patience
- player should use everything learned

After all required visitors are successfully healed:

DO NOT immediately start Level 6.

Open the Wisdom Tree Quiz.

==================================================
15. WISDOM TREE QUIZ
    ==================================================

Every 5 levels should trigger a Wisdom Tree Micro-Quiz.

Therefore:

Level 5 → Quiz
Level 10 → Quiz
Level 15 → Quiz
Level 20 → Quiz
etc.

Quiz should be data-driven.

Create:

QuizQuestion

Fields:

- question
- options
- correctAnswer
- explanation
- relatedVirtue
- reward

Example:

Question:

"A friend is feeling restless and anxious. What fruit do they need?"

Options:

Strawberry
Watermelon
Banana
Cherry

Correct answer:

Watermelon → Peace

After correct answer:

- Golden Harvest Mode
- 2x points
- warm golden lighting
- special particles
- temporary bonus state

If wrong:

- provide gentle feedback
- explain the correct concept
- allow retry or continue according to game design

Do NOT hard-code only one question.

Create a Quiz Database architecture so additional questions can easily be added.

==================================================
16. FUTURE LEVEL STRUCTURE
    ==================================================

The game is not limited to 5 levels.

The pattern should continue:

1
2
3
4
5
→ QUIZ

6
7
8
9
10
→ QUIZ

11
12
13
14
15
→ QUIZ

etc.

Difficulty should gradually increase through:

- more visitors
- more fruit types
- shorter patience
- more simultaneous requests
- more complex gesture combinations
- more strategic prioritization
- environmental expansion

Do not introduce every difficulty mechanic at once.

==================================================
17. ISLAND EXPANSION
    ==================================================

The island should eventually grow as the player progresses.

Example:

Levels 1–5:
Small starting island

Levels 6–10:
New land area

Levels 11–15:
More environmental space

Levels 16+:
Additional areas/features

Do NOT build the entire large world immediately.

Use modular expansion.

==================================================
18. UI
    ==================================================

Initial UI should include:

- Vibrancy meter
- Level indicator
- basic pause/settings
- visitor request/thought bubble
- patience meter
- tutorial message
- quiz bottom sheet
- reward feedback

Keep gameplay UI clean and mobile-friendly.

3D world should remain the focus.

==================================================
19. VFX
    ==================================================

Eventually implement:

- fruit harvest VFX
- virtue-specific particles
- tonic transformation VFX
- tonic flight trail
- NPC healing effect
- color restoration effect
- celebration particles
- vibrancy transition
- Golden Harvest Mode

Do not create all VFX before the gameplay works.

Use placeholder particles initially.

==================================================
20. AUDIO
    ==================================================

Eventually:

- ambient background music
- tap sound
- harvest sound
- tonic transformation sound
- tonic delivery sound
- healing sound
- celebration sound
- quiz feedback
- Golden Harvest Mode sound

Keep audio modular.

==================================================
21. SAVE SYSTEM
    ==================================================

Eventually save:

- current level
- unlocked fruits
- unlocked island areas
- vibrancy progress
- quiz progress
- player progression

Do NOT build an overcomplicated save system for the first prototype.

==================================================
22. MOBILE OPTIMIZATION
    ==================================================

This is a mobile game.

Keep performance in mind from the beginning.

Watch:

- polygon count
- draw calls
- texture resolution
- particle count
- overdraw
- GC allocations
- Instantiate/Destroy usage
- unnecessary Update loops
- expensive shaders
- memory usage

Use object pooling where appropriate.

Do not prematurely optimize every line.

First make the gameplay correct, then profile and optimize based on actual measurements.

==================================================
23. UNITY PROJECT ARCHITECTURE
    ==================================================

Prefer a clean structure such as:

Assets/
│
├── Art/
│   ├── Environment/
│   ├── Characters/
│   ├── Fruits/
│   ├── Buildings/
│   └── Materials/
│
├── Audio/
│
├── VFX/
│
├── Prefabs/
│   ├── Environment/
│   ├── Characters/
│   ├── Fruits/
│   ├── Gameplay/
│   └── UI/
│
├── Scenes/
│
├── Scripts/
│   ├── Core/
│   ├── Gameplay/
│   ├── Characters/
│   ├── Fruits/
│   ├── Levels/
│   ├── Quiz/
│   ├── UI/
│   ├── VFX/
│   └── Save/
│
└── Data/
├── Fruits/
├── Levels/
└── Quiz/

Do not blindly create this structure if the existing project already has a good architecture.
First inspect the project.

==================================================
24. DEVELOPMENT PROCESS — VERY IMPORTANT
    ==================================================

DO NOT attempt to build the entire game in one step.

Follow this exact order:

PHASE 0:
READ-ONLY PROJECT AUDIT

Inspect:
- Unity version
- render pipeline
- scenes
- scripts
- prefabs
- packages
- current assets
- current architecture
- errors
- warnings
- existing systems

Do NOT modify anything.

Provide:
A. Existing systems
B. Working systems
C. Broken systems
D. Missing systems
E. Architecture risks
F. Recommended implementation order

Then wait for approval.

--------------------------------------------------

PHASE 1:
FOUNDATION

Implement only:

- scene foundation
- isometric camera
- graybox floating island
- water
- basic lighting
- Stone Well placeholder
- Wisdom Tree placeholder
- basic planting spots
- NPC spawn point/path

Test it.

Fix errors.

Do not continue until the scene is stable.

--------------------------------------------------

PHASE 2:
FIRST PLAYABLE LOOP

Implement ONLY:

- one NPC
- Strawberry Tree
- double-tap harvesting
- strawberry
- tonic
- tonic flight
- NPC healing
- grey-to-color restoration
- celebration
- vibrancy increase

This is the first major vertical slice.

Test the COMPLETE LOOP.

--------------------------------------------------

PHASE 3:
LEVEL SYSTEM

Implement:

- LevelConfig
- LevelManager
- Level 1
- Level 2
- Level 3
- Level 4
- Level 5

Make difficulty configurable.

--------------------------------------------------

PHASE 4:
MULTIPLE FRUITS

Add the remaining fruit system using the same architecture.

Do not duplicate systems unnecessarily.

--------------------------------------------------

PHASE 5:
QUIZ

Implement:

- QuizQuestion
- Quiz database
- Wisdom Tree quiz
- Level 5 trigger
- answer validation
- feedback
- Golden Harvest Mode

--------------------------------------------------

PHASE 6:
POLISH

Add:

- final assets
- VFX
- animations
- audio
- haptics
- lighting polish
- UI polish

--------------------------------------------------

PHASE 7:
OPTIMIZATION

Profile the actual mobile build.

Fix real performance bottlenecks.

--------------------------------------------------

PHASE 8:
FLUTTER INTEGRATION

ONLY after the Unity game is stable.

==================================================
25. DEBUGGING RULES
    ==================================================

After every implementation phase:

1. Compile the project.
2. Check Unity Console.
3. Check for errors.
4. Check for missing references.
5. Check prefab references.
6. Check scene references.
7. Test the gameplay manually.
8. Fix issues caused by your changes.
9. Do not move to the next phase if the current phase is broken.

If you encounter an issue:

FIRST identify the root cause.

Do not randomly patch symptoms.

Do not rewrite unrelated systems.

Do not delete working code just to simplify your implementation.

==================================================
26. CODE QUALITY RULES
    ==================================================

Use:

- clear class names
- small focused components
- ScriptableObjects for configurable game data where appropriate
- reusable systems
- inspector-configurable values
- minimal coupling
- event-driven communication where useful

Avoid:

- giant GameManager classes
- duplicated fruit scripts
- duplicated level logic
- hard-coded level-specific behavior
- magic numbers everywhere
- unnecessary singletons
- unnecessary dependencies
- premature abstraction

==================================================
27. CRITICAL RULE
    ==================================================

NEVER assume something exists.

Before using a class, prefab, material, shader, scene, package, asset or method:

VERIFY IT EXISTS.

If it does not exist:
- create it only if required
- document what you created
- keep it minimal

Never invent existing functionality.

==================================================
28. WHAT I WANT FROM YOU
    ==================================================

Your job is to act as a senior Unity game developer.

Do not just write code.

Think about:

- gameplay architecture
- player experience
- mobile performance
- maintainability
- asset workflow
- level progression
- debugging
- scalability

But do not over-engineer.

Build the smallest working version first.

==================================================
29. FIRST ACTION
    ==================================================

START NOW WITH PHASE 0.

DO NOT MODIFY THE PROJECT YET.

Perform a READ-ONLY audit of the current Unity project.

Return a structured report:

1. Project Overview
2. Existing Scenes
3. Existing Scripts
4. Existing Prefabs
5. Existing Assets
6. Existing Packages
7. Render Pipeline
8. Current Camera
9. Current Input System
10. Current UI
11. Existing Gameplay Systems
12. Compile Errors
13. Missing References
14. Architecture Problems
15. Missing Features
16. Recommended Development Order
17. Exact Phase 1 Implementation Plan

For every finding, label it:

CONFIRMED ISSUE
POSSIBLE ISSUE
MISSING FEATURE
RECOMMENDATION

DO NOT modify, delete, rename, or refactor anything during this audit.

After the audit, WAIT for my instruction before implementing Phase 1.