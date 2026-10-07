#if UNITY_EDITOR
// ============================================================================
//  WireLevel1Scene.cs
//  Tools → Radiant Orchard → Wire Level 1 Scene
//
//  One-click scene wiring for a fully playable Level 1.
//  Run AFTER:
//    1. Tools → Radiant Orchard → Setup Core Game Data
//    2. Tools → Radiant Orchard → Setup Level 1 Data
//
//  What this script does:
//    A. Creates / finds the "GameManagers" GameObject and adds:
//         GameState, LevelManager, NPCSpawner, GestureManager,
//         SfxPlayer (with AudioSource), QuizManager,
//         VibrancyObjectiveDriver
//    B. Wires LevelManager.allLevels[] → Level_01 asset
//    C. Wires NPCSpawner:
//         stickmanPrefab → GameData/Prefabs/Stickman.prefab
//         fruitPool[]    → Strawberry + Pineapple + Watermelon FruitData
//         spawnPoints[]  → creates 5 well-spaced spawn points on the island
//    D. Wires SfxPlayer audio clips → all 6 SFX_*.wav
//    E. Wires QuizManager.panelUI → creates QuizPanelUI Canvas if missing
//    F. Creates the HUD Canvas with:
//         VibrancyMeterUI  (fill bar + 3 L1 fruit icons)
//         LevelCompletePopupUI
//         TutorialManager  (intro panel + continue button + pointer)
//    G. Wires main Camera to CameraOrbitController
//    H. Starts the level via GameState.defaultLevelId = "level_01"
//    I. Marks scene dirty so Ctrl+S saves everything.
// ============================================================================
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RadiantOrchard;

public static class WireLevel1Scene
{
    // ── asset paths ──────────────────────────────────────────────────────────
    const string StickmanPrefabPath  = "Assets/GameData/Prefabs/Stickman.prefab";
    const string FruitDataPath       = "Assets/GameData/FruitData/FruitData_{0}.asset";
    const string LevelPath           = "Assets/GameData/Levels/Level_01.asset";
    const string SfxDir              = "Assets/GameData/Audio/SFX/";
    const string GreyMatPath         = "Assets/GameData/Materials/Stickman_Grey.mat";
    const string ColorMatPath        = "Assets/GameData/Materials/Stickman_Color.mat";
    const string AnimControllerPath  = "Assets/GameData/Animators/StickmanState.controller";
    const string MaleOverridePath    = "Assets/GameData/Animators/StickmanState_Male.overrideController";
    const string FemaleOverridePath  = "Assets/GameData/Animators/StickmanState_Female.overrideController";
    const string IconDir             = "Assets/GameData/Sprites/";

    [MenuItem("Tools/Radiant Orchard/Wire Level 1 Scene")]
    static void Wire()
    {
        Undo.SetCurrentGroupName("Wire Level 1 Scene");
        int undoGroup = Undo.GetCurrentGroup();

        // ── A. GameManagers root object ──────────────────────────────────────
        var managers = FindOrCreate("GameManagers");

        var gameState   = EnsureComponent<GameState>(managers);
        var levelMgr    = EnsureComponent<LevelManager>(managers);
        var npcSpawner  = EnsureComponent<NPCSpawner>(managers);
        var gestureMgr  = EnsureComponent<GestureManager>(managers);
        var sfxPlayer   = EnsureComponent<SfxPlayer>(managers);
        var quizMgr     = EnsureComponent<QuizManager>(managers);
        var objDriver   = EnsureComponent<VibrancyObjectiveDriver>(managers);

        // AudioSource for SfxPlayer
        var audioSrc = managers.GetComponent<AudioSource>();
        if (audioSrc == null) audioSrc = Undo.AddComponent<AudioSource>(managers);
        audioSrc.playOnAwake = false;

        // ── B. LevelManager → Level_01 ───────────────────────────────────────
        var level1 = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPath);
        if (level1 != null)
        {
            SerializedObject soLM = new SerializedObject(levelMgr);
            var allLevelsProp = soLM.FindProperty("allLevels");
            allLevelsProp.arraySize = 1;
            allLevelsProp.GetArrayElementAtIndex(0).objectReferenceValue = level1;
            soLM.ApplyModifiedProperties();
            Debug.Log("✓ LevelManager.allLevels[0] = Level_01");
        }
        else Debug.LogWarning("✗ Level_01.asset not found — run Setup Level 1 Data first");

        // ── C. NPCSpawner wiring ─────────────────────────────────────────────
        var stickmanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StickmanPrefabPath);
        SerializedObject soNPC = new SerializedObject(npcSpawner);

        // stickmanPrefab
        soNPC.FindProperty("stickmanPrefab").objectReferenceValue = stickmanPrefab;

        // fruitPool — only the 3 Level-1 fruits (all DoubleTap)
        var fruitPoolProp = soNPC.FindProperty("fruitPool");
        fruitPoolProp.arraySize = 3;
        string[] l1Fruits = { "Strawberry", "Pineapple", "Watermelon" };
        for (int i = 0; i < l1Fruits.Length; i++)
        {
            var fd = AssetDatabase.LoadAssetAtPath<RadiantOrchard.FruitData>(
                string.Format(FruitDataPath, l1Fruits[i]));
            fruitPoolProp.GetArrayElementAtIndex(i).objectReferenceValue = fd;
            if (fd == null) Debug.LogWarning($"✗ FruitData_{l1Fruits[i]}.asset not found");
            else            Debug.Log($"✓ NPCSpawner fruitPool[{i}] = {l1Fruits[i]}");
        }

        // spawnPoints — create 5 transforms in a ring around the island centre
        var spawnRoot = FindOrCreate("SpawnPoints");
        spawnRoot.transform.position = Vector3.zero;
        // Clear old spawn point children
        for (int i = spawnRoot.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(spawnRoot.transform.GetChild(i).gameObject);

        float[] spawnAngles  = { 0f, 72f, 144f, 216f, 288f };
        float   spawnRadius  = 15f;
        var     spawnTransforms = new Transform[spawnAngles.Length];
        for (int i = 0; i < spawnAngles.Length; i++)
        {
            float rad = spawnAngles[i] * Mathf.Deg2Rad;
            var sp = new GameObject("SpawnPoint_" + i);
            Undo.RegisterCreatedObjectUndo(sp, "SpawnPoint");
            sp.transform.SetParent(spawnRoot.transform, false);
            sp.transform.localPosition = new Vector3(
                Mathf.Sin(rad) * spawnRadius, 0f, Mathf.Cos(rad) * spawnRadius);
            // Face inward toward island centre
            sp.transform.localRotation = Quaternion.LookRotation(
                -sp.transform.localPosition.normalized, Vector3.up);
            spawnTransforms[i] = sp.transform;
        }

        var spawnPointsProp = soNPC.FindProperty("spawnPoints");
        spawnPointsProp.arraySize = spawnTransforms.Length;
        for (int i = 0; i < spawnTransforms.Length; i++)
            spawnPointsProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnTransforms[i];

        soNPC.ApplyModifiedProperties();
        Debug.Log("✓ NPCSpawner: stickmanPrefab, 3 L1 fruits, 5 spawn points wired");

        // ── D. SfxPlayer clips ───────────────────────────────────────────────
        SerializedObject soSfx = new SerializedObject(sfxPlayer);
        soSfx.FindProperty("source").objectReferenceValue = audioSrc;
        WireClip(soSfx, "gestureSuccess", SfxDir + "SFX_GestureSuccess.wav");
        WireClip(soSfx, "tonicPop",       SfxDir + "SFX_TonicPop.wav");
        WireClip(soSfx, "heal",           SfxDir + "SFX_Heal.wav");
        WireClip(soSfx, "quizCorrect",    SfxDir + "SFX_QuizCorrect.wav");
        WireClip(soSfx, "quizIncorrect",  SfxDir + "SFX_QuizIncorrect.wav");
        WireClip(soSfx, "levelComplete",  SfxDir + "SFX_LevelComplete.wav");
        soSfx.ApplyModifiedProperties();
        Debug.Log("✓ SfxPlayer: all 6 clips wired");

        // ── E + F. HUD Canvas ────────────────────────────────────────────────
        BuildHUDCanvas(managers, quizMgr);

        // ── G. Camera ────────────────────────────────────────────────────────
        WireCamera();

        // ── H. GameState default level ───────────────────────────────────────
        SerializedObject soGS = new SerializedObject(gameState);
        soGS.FindProperty("defaultLevelId").stringValue = "level_01";
        soGS.FindProperty("maxVibrancy").intValue = 100;
        soGS.ApplyModifiedProperties();
        Debug.Log("✓ GameState: defaultLevelId=level_01, maxVibrancy=100");

        // ── Stickman prefab integrity check ─────────────────────────────────
        PatchStickmanPrefab();

        // ── Done ─────────────────────────────────────────────────────────────
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("=================================================");
        Debug.Log("✅ Wire Level 1 Scene COMPLETE. Save scene (Ctrl+S) then press Play!");
        Debug.Log("   Controls: Double-click fruit to harvest • Middle-mouse = quick DoubleTap");
        Debug.Log("=================================================");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BUILD HUD CANVAS
    // ─────────────────────────────────────────────────────────────────────────
    static void BuildHUDCanvas(GameObject managers, QuizManager quizMgr)
    {
        // Find or create the main HUD Canvas
        var existingCanvas = GameObject.Find("HUD_Canvas");
        GameObject canvasGO;
        if (existingCanvas != null)
        {
            canvasGO = existingCanvas;
            Debug.Log("✓ Found existing HUD_Canvas — patching components");
        }
        else
        {
            canvasGO = new GameObject("HUD_Canvas");
            Undo.RegisterCreatedObjectUndo(canvasGO, "HUD Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080, 1920);
            canvasGO.AddComponent<GraphicRaycaster>();
            Debug.Log("✓ Created HUD_Canvas (ScreenSpaceOverlay)");
        }

        // ── Vibrancy Bar ─────────────────────────────────────────────────────
        var vibrancyBar = FindOrCreateChild(canvasGO, "VibrancyBar");
        var vibrancyUI  = EnsureComponent<VibrancyMeterUI>(vibrancyBar);

        // Build the fill bar background + fill image
        var barBg   = FindOrCreateChild(vibrancyBar, "BarBackground");
        var bgImage = EnsureComponent<Image>(barBg);
        bgImage.color = new Color(0.12f, 0.12f, 0.12f, 0.75f);
        var bgRect  = barBg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.05f, 0.92f);
        bgRect.anchorMax = new Vector2(0.95f, 0.97f);
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

        var fillGO  = FindOrCreateChild(barBg, "Fill");
        var fillImg = EnsureComponent<Image>(fillGO);
        fillImg.color = new Color(0.35f, 0.85f, 0.40f);
        fillImg.type  = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        var fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;

        // Wire VibrancyMeterUI.fillImage
        SerializedObject soVM = new SerializedObject(vibrancyUI);
        soVM.FindProperty("fillImage").objectReferenceValue = fillImg;

        // ── 3 L1 Fruit Icons in the bar ──────────────────────────────────────
        string[] l1Fruits  = { "Strawberry", "Pineapple", "Watermelon" };
        Color[]  iconColors = {
            HexColor("FF4757"), HexColor("FFD700"), HexColor("4CAF50")
        };
        var fruitIconsProp = soVM.FindProperty("fruitIcons");
        fruitIconsProp.arraySize = l1Fruits.Length;

        for (int i = 0; i < l1Fruits.Length; i++)
        {
            var iconGO   = FindOrCreateChild(barBg, "Icon_" + l1Fruits[i]);
            var iconImg  = EnsureComponent<Image>(iconGO);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                IconDir + "Icon_" + l1Fruits[i] + ".png");
            if (sprite != null) iconImg.sprite = sprite;
            iconImg.color = iconColors[i];
            iconImg.preserveAspect = true;

            var iconRect = iconGO.GetComponent<RectTransform>();
            float xAnchor = 0.15f + i * 0.35f;
            iconRect.anchorMin = new Vector2(xAnchor - 0.08f, 0.05f);
            iconRect.anchorMax = new Vector2(xAnchor + 0.08f, 0.95f);
            iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;

            fruitIconsProp.GetArrayElementAtIndex(i).objectReferenceValue = iconImg;
        }
        soVM.ApplyModifiedProperties();
        Debug.Log("✓ VibrancyMeterUI: fill image + 3 fruit icons wired");

        // ── Level Complete Popup ─────────────────────────────────────────────
        var popupGO  = FindOrCreateChild(canvasGO, "LevelCompletePopup");
        var popupUI  = EnsureComponent<LevelCompletePopupUI>(popupGO);

        // Panel background
        var panelGO  = FindOrCreateChild(popupGO, "Panel");
        var panelImg = EnsureComponent<Image>(panelGO);
        panelImg.color = new Color(0.05f, 0.05f, 0.05f, 0.88f);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0.35f);
        panelRect.anchorMax = new Vector2(0.85f, 0.65f);
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;

        // Message text
        var msgGO   = FindOrCreateChild(panelGO, "MessageText");
        var msgText = EnsureComponent<Text>(msgGO);
        msgText.text      = "Level Complete!";
        msgText.fontSize  = 48;
        msgText.alignment = TextAnchor.MiddleCenter;
        msgText.color     = Color.white;
        var msgRect = msgGO.GetComponent<RectTransform>();
        msgRect.anchorMin = new Vector2(0.05f, 0.55f);
        msgRect.anchorMax = new Vector2(0.95f, 0.90f);
        msgRect.offsetMin = msgRect.offsetMax = Vector2.zero;

        // Continue button
        var btnGO   = FindOrCreateChild(panelGO, "ContinueButton");
        var btn     = EnsureComponent<Button>(btnGO);
        var btnImg  = EnsureComponent<Image>(btnGO);
        btnImg.color = new Color(0.25f, 0.72f, 0.35f);
        var btnRect = btnGO.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.25f, 0.10f);
        btnRect.anchorMax = new Vector2(0.75f, 0.42f);
        btnRect.offsetMin = btnRect.offsetMax = Vector2.zero;

        var btnLabelGO  = FindOrCreateChild(btnGO, "Label");
        var btnLabel    = EnsureComponent<Text>(btnLabelGO);
        btnLabel.text      = "Continue";
        btnLabel.fontSize  = 36;
        btnLabel.alignment = TextAnchor.MiddleCenter;
        btnLabel.color     = Color.white;
        var lblRect = btnLabelGO.GetComponent<RectTransform>();
        lblRect.anchorMin = Vector2.zero;
        lblRect.anchorMax = Vector2.one;
        lblRect.offsetMin = lblRect.offsetMax = Vector2.zero;

        SerializedObject soPopup = new SerializedObject(popupUI);
        soPopup.FindProperty("panel").objectReferenceValue = panelGO;
        soPopup.FindProperty("messageText").objectReferenceValue = msgText;
        soPopup.FindProperty("continueButton").objectReferenceValue = btn;
        soPopup.ApplyModifiedProperties();

        // Start hidden
        panelGO.SetActive(false);
        Debug.Log("✓ LevelCompletePopupUI built and wired");

        // ── Tutorial Panel ───────────────────────────────────────────────────
        var tutorialGO  = FindOrCreateChild(canvasGO, "TutorialPanel");
        var tutMgr      = EnsureComponent<TutorialManager>(tutorialGO);

        // Intro panel background
        var introGO  = FindOrCreateChild(tutorialGO, "IntroPanel");
        var introImg = EnsureComponent<Image>(introGO);
        introImg.color = new Color(0.05f, 0.05f, 0.05f, 0.92f);
        var introRect = introGO.GetComponent<RectTransform>();
        introRect.anchorMin = new Vector2(0.05f, 0.15f);
        introRect.anchorMax = new Vector2(0.95f, 0.85f);
        introRect.offsetMin = introRect.offsetMax = Vector2.zero;

        // Tutorial title
        var tutTitleGO  = FindOrCreateChild(introGO, "TitleText");
        var tutTitleTxt = EnsureComponent<Text>(tutTitleGO);
        tutTitleTxt.text      = "Welcome to The Radiant Orchard!";
        tutTitleTxt.fontSize  = 40;
        tutTitleTxt.fontStyle = FontStyle.Bold;
        tutTitleTxt.alignment = TextAnchor.MiddleCenter;
        tutTitleTxt.color     = new Color(1f, 0.92f, 0.4f);
        var ttRect = tutTitleGO.GetComponent<RectTransform>();
        ttRect.anchorMin = new Vector2(0.05f, 0.72f);
        ttRect.anchorMax = new Vector2(0.95f, 0.92f);
        ttRect.offsetMin = ttRect.offsetMax = Vector2.zero;

        // Tutorial body text
        var tutBodyGO  = FindOrCreateChild(introGO, "BodyText");
        var tutBodyTxt = EnsureComponent<Text>(tutBodyGO);
        tutBodyTxt.text = "Grey Stickmen are arriving on the island.\n\n" +
                          "Look for the bubble above their head to see what they need.\n\n" +
                          "DOUBLE-TAP a fruit to harvest it and send it to them!\n\n" +
                          "Fill the Vibrancy bar to complete Level 1.";
        tutBodyTxt.fontSize  = 28;
        tutBodyTxt.alignment = TextAnchor.MiddleCenter;
        tutBodyTxt.color     = Color.white;
        var tbRect = tutBodyGO.GetComponent<RectTransform>();
        tbRect.anchorMin = new Vector2(0.05f, 0.25f);
        tbRect.anchorMax = new Vector2(0.95f, 0.70f);
        tbRect.offsetMin = tbRect.offsetMax = Vector2.zero;

        // Continue button for tutorial
        var tutBtnGO  = FindOrCreateChild(introGO, "ContinueButton");
        var tutBtn    = EnsureComponent<Button>(tutBtnGO);
        var tutBtnImg = EnsureComponent<Image>(tutBtnGO);
        tutBtnImg.color = new Color(0.25f, 0.72f, 0.35f);
        var tutBtnRect = tutBtnGO.GetComponent<RectTransform>();
        tutBtnRect.anchorMin = new Vector2(0.25f, 0.05f);
        tutBtnRect.anchorMax = new Vector2(0.75f, 0.22f);
        tutBtnRect.offsetMin = tutBtnRect.offsetMax = Vector2.zero;

        var tutBtnLblGO  = FindOrCreateChild(tutBtnGO, "Label");
        var tutBtnLbl    = EnsureComponent<Text>(tutBtnLblGO);
        tutBtnLbl.text      = "Let's Go!";
        tutBtnLbl.fontSize  = 34;
        tutBtnLbl.alignment = TextAnchor.MiddleCenter;
        tutBtnLbl.color     = Color.white;
        var tutLblRect = tutBtnLblGO.GetComponent<RectTransform>();
        tutLblRect.anchorMin = Vector2.zero;
        tutLblRect.anchorMax = Vector2.one;
        tutLblRect.offsetMin = tutLblRect.offsetMax = Vector2.zero;

        // Tutorial pointer arrow
        var pointerGO  = FindOrCreateChild(tutorialGO, "TutorialPointer");
        var pointerUI  = EnsureComponent<TutorialPointerUI>(pointerGO);
        var pointerImg = EnsureComponent<Image>(pointerGO);
        pointerImg.color = new Color(1f, 0.92f, 0.15f, 0.9f);
        var pointerRect = pointerGO.GetComponent<RectTransform>();
        pointerRect.sizeDelta = new Vector2(60f, 60f);

        // Wire TutorialManager
        SerializedObject soTut = new SerializedObject(tutMgr);
        soTut.FindProperty("introPanel").objectReferenceValue    = introGO;
        soTut.FindProperty("continueButton").objectReferenceValue = tutBtn;
        soTut.FindProperty("pointer").objectReferenceValue        = pointerUI;
        soTut.ApplyModifiedProperties();
        Debug.Log("✓ TutorialManager: intro panel + pointer wired");

        // ── Quiz Panel ───────────────────────────────────────────────────────
        var quizPanelGO = FindOrCreateChild(canvasGO, "QuizPanel");
        var quizPanelUI = EnsureComponent<QuizPanelUI>(quizPanelGO);

        var quizBgImg = EnsureComponent<Image>(quizPanelGO);
        quizBgImg.color = new Color(0.06f, 0.06f, 0.08f, 0.95f);
        var quizRect = quizPanelGO.GetComponent<RectTransform>();
        quizRect.anchorMin = new Vector2(0.05f, 0.20f);
        quizRect.anchorMax = new Vector2(0.95f, 0.80f);
        quizRect.offsetMin = quizRect.offsetMax = Vector2.zero;

        // Question text
        var qTextGO  = FindOrCreateChild(quizPanelGO, "QuestionText");
        var qText    = EnsureComponent<Text>(qTextGO);
        qText.fontSize  = 30;
        qText.alignment = TextAnchor.MiddleCenter;
        qText.color     = Color.white;
        var qTextRect = qTextGO.GetComponent<RectTransform>();
        qTextRect.anchorMin = new Vector2(0.05f, 0.62f);
        qTextRect.anchorMax = new Vector2(0.95f, 0.95f);
        qTextRect.offsetMin = qTextRect.offsetMax = Vector2.zero;

        // Result text
        var rTextGO  = FindOrCreateChild(quizPanelGO, "ResultText");
        var rText    = EnsureComponent<Text>(rTextGO);
        rText.fontSize  = 28;
        rText.alignment = TextAnchor.MiddleCenter;
        rText.color     = new Color(0.4f, 1.0f, 0.4f);
        var rTextRect = rTextGO.GetComponent<RectTransform>();
        rTextRect.anchorMin = new Vector2(0.05f, 0.50f);
        rTextRect.anchorMax = new Vector2(0.95f, 0.62f);
        rTextRect.offsetMin = rTextRect.offsetMax = Vector2.zero;

        // 3 answer buttons
        var optionBtns = new Button[3];
        Color[] btnColors = {
            new Color(0.20f, 0.50f, 0.82f),
            new Color(0.82f, 0.50f, 0.12f),
            new Color(0.52f, 0.18f, 0.72f)
        };
        for (int i = 0; i < 3; i++)
        {
            var obGO    = FindOrCreateChild(quizPanelGO, "OptionButton_" + i);
            var obBtn   = EnsureComponent<Button>(obGO);
            var obImg   = EnsureComponent<Image>(obGO);
            obImg.color = btnColors[i];
            var obRect  = obGO.GetComponent<RectTransform>();
            obRect.anchorMin = new Vector2(0.05f, 0.38f - i * 0.18f);
            obRect.anchorMax = new Vector2(0.95f, 0.53f - i * 0.18f);
            obRect.offsetMin = obRect.offsetMax = Vector2.zero;

            var obLblGO = FindOrCreateChild(obGO, "Label");
            var obLbl   = EnsureComponent<Text>(obLblGO);
            obLbl.fontSize  = 26;
            obLbl.alignment = TextAnchor.MiddleCenter;
            obLbl.color     = Color.white;
            var obLblRect = obLblGO.GetComponent<RectTransform>();
            obLblRect.anchorMin = Vector2.zero;
            obLblRect.anchorMax = Vector2.one;
            obLblRect.offsetMin = obLblRect.offsetMax = Vector2.zero;

            optionBtns[i] = obBtn;
        }

        // Wire QuizPanelUI
        SerializedObject soQP = new SerializedObject(quizPanelUI);
        soQP.FindProperty("questionText").objectReferenceValue   = qText;
        soQP.FindProperty("resultText").objectReferenceValue     = rText;
        var optionBtnsProp = soQP.FindProperty("optionButtons");
        optionBtnsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
            optionBtnsProp.GetArrayElementAtIndex(i).objectReferenceValue = optionBtns[i];
        soQP.ApplyModifiedProperties();

        // Wire QuizManager → QuizPanelUI
        SerializedObject soQM = new SerializedObject(quizMgr);
        soQM.FindProperty("panelUI").objectReferenceValue = quizPanelUI;
        soQM.ApplyModifiedProperties();

        // Start quiz panel hidden
        quizPanelGO.SetActive(false);
        Debug.Log("✓ QuizPanelUI + QuizManager wired (3 option buttons)");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // WIRE CAMERA
    // ─────────────────────────────────────────────────────────────────────────
    static void WireCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            // Create a camera if none exists
            var camGO = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(camGO, "Main Camera");
            cam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
            camGO.tag = "MainCamera";
        }

        // Position for a nice isometric-ish view of the island
        cam.transform.position = new Vector3(0f, 28f, -30f);
        cam.transform.rotation = Quaternion.Euler(42f, 0f, 0f);
        cam.backgroundColor    = new Color(0.45f, 0.72f, 0.90f); // sky blue

        // Add orbit controller if missing
        var orbit = cam.GetComponent<CameraOrbitController>();
        if (orbit == null) orbit = Undo.AddComponent<CameraOrbitController>(cam.gameObject);

        Debug.Log("✓ Main Camera positioned + CameraOrbitController added");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PATCH STICKMAN PREFAB
    // ─────────────────────────────────────────────────────────────────────────
    static void PatchStickmanPrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StickmanPrefabPath);
        if (prefab == null) { Debug.LogWarning("✗ Stickman.prefab not found"); return; }

        // Open prefab for editing
        string assetPath = AssetDatabase.GetAssetPath(prefab);
        var prefabContents = PrefabUtility.LoadPrefabContents(assetPath);

        bool dirty = false;

        var sc = prefabContents.GetComponent<StickmanController>();
        if (sc == null) sc = prefabContents.AddComponent<StickmanController>();

        // Materials
        var greyMat  = AssetDatabase.LoadAssetAtPath<Material>(GreyMatPath);
        var colorMat = AssetDatabase.LoadAssetAtPath<Material>(ColorMatPath);
        SerializedObject soSC = new SerializedObject(sc);

        if (greyMat != null)
        {
            soSC.FindProperty("greyMat").objectReferenceValue = greyMat;
            dirty = true;
        }
        if (colorMat != null)
        {
            soSC.FindProperty("colorMat").objectReferenceValue = colorMat;
            dirty = true;
        }

        // Animator controller
        var anim = prefabContents.GetComponent<Animator>();
        if (anim != null)
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimControllerPath);
            if (ctrl != null && anim.runtimeAnimatorController == null)
            {
                anim.runtimeAnimatorController = ctrl;
                dirty = true;
            }

            var maleOverride   = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(MaleOverridePath);
            var femaleOverride = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(FemaleOverridePath);
            if (maleOverride != null)
            {
                soSC.FindProperty("maleWalkOverride").objectReferenceValue = maleOverride;
                dirty = true;
            }
            if (femaleOverride != null)
            {
                soSC.FindProperty("femaleWalkOverride").objectReferenceValue = femaleOverride;
                dirty = true;
            }
        }

        // VibrancyReward — 10 per heal (10 heals = 100 = L1 complete)
        soSC.FindProperty("vibrancyReward").intValue = 10;
        dirty = true;

        soSC.ApplyModifiedProperties();

        if (dirty)
        {
            PrefabUtility.SaveAsPrefabAsset(prefabContents, assetPath);
            Debug.Log("✓ Stickman.prefab patched: greyMat, colorMat, animator, vibrancyReward=10");
        }

        PrefabUtility.UnloadPrefabContents(prefabContents);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────
    static GameObject FindOrCreate(string name)
    {
        var go = GameObject.Find(name);
        if (go == null)
        {
            go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        }
        return go;
    }

    static GameObject FindOrCreateChild(GameObject parent, string name)
    {
        var existing = parent.transform.Find(name);
        if (existing != null) return existing.gameObject;

        var child = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(child, "Create " + name);
        child.transform.SetParent(parent.transform, false);

        // Every UI child needs a RectTransform
        if (parent.GetComponent<Canvas>() != null || parent.GetComponent<RectTransform>() != null)
        {
            if (child.GetComponent<RectTransform>() == null)
                child.AddComponent<RectTransform>();
        }

        return child;
    }

    static T EnsureComponent<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c == null) c = Undo.AddComponent<T>(go);
        return c;
    }

    static void WireClip(SerializedObject so, string propName, string path)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        so.FindProperty(propName).objectReferenceValue = clip;
        if (clip == null) Debug.LogWarning($"✗ AudioClip not found: {path}");
    }

    static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }
}
#endif
