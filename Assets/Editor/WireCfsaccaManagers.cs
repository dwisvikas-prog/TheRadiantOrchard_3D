#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RadiantOrchard;

// One-click completion of the manager set in the newest gameplay scene
// (cfsacca.unity — binary-serialized, so this must run inside the editor
// rather than being patched as text). Adds ONLY what's missing and wires
// serialized references where matching objects exist, so it's safe to
// re-run: everything reports what it found, changed nothing, or added and
// wired the piece.
//
// Menu: Tools > Radiant Orchard > Wire cfsacca Managers - Phase 7
// Then save the scene (Ctrl+S). Verify in Play mode: heal Stickmen until
// vibrancy hits 100 -> Level Complete fires and the level advances.
public static class WireCfsaccaManagers
{
    private const string ManagersObjectName = "GameManagers";

    [MenuItem("Tools/Radiant Orchard/Wire cfsacca Managers - Phase 7")]
    private static void Wire()
    {
        var managers = GameObject.Find(ManagersObjectName);
        if (managers == null)
        {
            managers = new GameObject(ManagersObjectName);
            Undo.RegisterCreatedObjectUndo(managers, "Create " + ManagersObjectName);
            Debug.Log("WireCfsaccaManagers: created '" + ManagersObjectName + "' (did not exist in this scene).");
        }

        int added = 0;

        // --- GameState must exist; everything else keys off it -------------
        if (managers.GetComponent<GameState>() == null)
        {
            Undo.AddComponent<GameState>(managers);
            added++;
            Debug.Log("WireCfsaccaManagers: added GameState.");
        }

        // --- LevelManager + level list -------------------------------------
        var levelManager = managers.GetComponent<LevelManager>();
        if (levelManager == null)
        {
            levelManager = Undo.AddComponent<LevelManager>(managers);
            added++;
            Debug.Log("WireCfsaccaManagers: added LevelManager.");
        }

        var guids = AssetDatabase.FindAssets("t:LevelDefinition", new[] { "Assets/GameData/Levels" });
        var levels = new LevelDefinition[guids.Length];
        for (int i = 0; i < guids.Length; i++)
            levels[i] = AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(guids[i]));

        var so = new SerializedObject(levelManager);
        var prop = so.FindProperty("allLevels");
        prop.arraySize = levels.Length;
        for (int i = 0; i < levels.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("WireCfsaccaManagers: LevelManager wired with " + levels.Length + " level(s).");

        // --- Heal -> objective bridge ---------------------------------------
        if (managers.GetComponent<VibrancyObjectiveDriver>() == null)
        {
            Undo.AddComponent<VibrancyObjectiveDriver>(managers);
            added++;
            Debug.Log("WireCfsaccaManagers: added VibrancyObjectiveDriver (heals now feed level objectives).");
        }

        // --- NPCSpawner content: fruit pool / spawn points / stickman prefab --
        // A spawner with an empty fruitPool or zero spawn points silently
        // spawns nothing, so wire whatever is missing from project assets and
        // scene objects named "SpawnPoint*".
        var spawner = Object.FindFirstObjectByType<NPCSpawner>(FindObjectsInactive.Include);
        if (spawner != null)
        {
            var soSpawn = new SerializedObject(spawner);
            bool spawnChanged = false;

            var fruitPoolProp = soSpawn.FindProperty("fruitPool");
            if (fruitPoolProp != null && fruitPoolProp.arraySize == 0)
            {
                var fGuids = AssetDatabase.FindAssets("t:FruitData", new[] { "Assets/GameData/FruitData" });
                // RadiantOrchard.FruitData must be fully qualified — the legacy
                // global FruitData class otherwise wins the name lookup and
                // LoadAssetAtPath would silently return nothing.
                var fruits = new System.Collections.Generic.List<RadiantOrchard.FruitData>();
                foreach (var g in fGuids)
                {
                    var fruit = AssetDatabase.LoadAssetAtPath<RadiantOrchard.FruitData>(AssetDatabase.GUIDToAssetPath(g));
                    if (fruit != null) fruits.Add(fruit); // skips legacy FruitData assets of the wrong class
                }
                if (fruits.Count > 0)
                {
                    fruitPoolProp.arraySize = fruits.Count;
                    for (int i = 0; i < fruits.Count; i++)
                        fruitPoolProp.GetArrayElementAtIndex(i).objectReferenceValue = fruits[i];
                    spawnChanged = true;
                    Debug.Log("WireCfsaccaManagers: NPCSpawner.fruitPool wired with " + fruits.Count + " FruitData asset(s).");
                }
            }

            var spawnPointsProp = soSpawn.FindProperty("spawnPoints");
            if (spawnPointsProp != null && spawnPointsProp.arraySize == 0)
            {
                var named = new System.Collections.Generic.List<Transform>();
                foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                    if (t.name.StartsWith("SpawnPoint")) named.Add(t);
                if (named.Count > 0)
                {
                    spawnPointsProp.arraySize = named.Count;
                    for (int i = 0; i < named.Count; i++)
                        spawnPointsProp.GetArrayElementAtIndex(i).objectReferenceValue = named[i];
                    spawnChanged = true;
                    Debug.Log("WireCfsaccaManagers: NPCSpawner.spawnPoints wired with " + named.Count + " point(s).");
                }
            }

            var prefabProp = soSpawn.FindProperty("stickmanPrefab");
            if (prefabProp != null && prefabProp.objectReferenceValue == null)
            {
                foreach (var g in AssetDatabase.FindAssets("Stickman t:Prefab"))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g));
                    if (prefab != null && prefab.GetComponentInChildren<RadiantOrchard.StickmanController>(true) != null)
                    {
                        prefabProp.objectReferenceValue = prefab;
                        spawnChanged = true;
                        Debug.Log("WireCfsaccaManagers: NPCSpawner.stickmanPrefab wired to '" + prefab.name + "'.");
                        break;
                    }
                }
            }

            if (spawnChanged) soSpawn.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning("WireCfsaccaManagers: no NPCSpawner in this scene — visitors won't spawn until one is added.");
        }

        // --- UI host canvas --------------------------------------------------
        // QuizManager/TutorialManager need to live on an always-active object;
        // BuildGameplayUI uses the GameplayUI canvas — reuse any existing
        // Screen Space Overlay canvas when that one is absent.
        var canvas = FindOrCreateUICanvas(ref added);

        // --- Quiz ------------------------------------------------------------
        var quizManager = canvas.GetComponent<QuizManager>();
        if (quizManager == null)
        {
            quizManager = Undo.AddComponent<QuizManager>(canvas.gameObject);
            added++;
            Debug.Log("WireCfsaccaManagers: added QuizManager to '" + canvas.name + "'.");
        }

        var quizPanel = Object.FindFirstObjectByType<QuizPanelUI>(FindObjectsInactive.Include);
        if (quizPanel == null)
        {
            quizPanel = BuildQuizPanel(canvas.transform);
            added++;
        }
        var soQuiz = new SerializedObject(quizManager);
        soQuiz.FindProperty("panelUI").objectReferenceValue = quizPanel;
        soQuiz.ApplyModifiedPropertiesWithoutUndo();

        // Quiz panel starts hidden (QuizPanelUI.Show activates it).
        if (quizPanel.gameObject.activeSelf) quizPanel.gameObject.SetActive(false);

        // --- Tutorial ---------------------------------------------------------
        var tutorial = canvas.GetComponent<TutorialManager>();
        if (tutorial == null)
        {
            tutorial = Undo.AddComponent<TutorialManager>(canvas.gameObject);
            added++;
            Debug.Log("WireCfsaccaManagers: added TutorialManager to '" + canvas.name + "'.");
        }

        var intro = GameObject.Find("IntroPanel");
        if (intro == null || intro.transform.parent != canvas.transform)
        {
            intro = BuildIntroPanel(canvas.transform);
            added++;
        }

        var pointer = Object.FindFirstObjectByType<TutorialPointerUI>(FindObjectsInactive.Include);
        if (pointer == null)
        {
            var pointerGO = new GameObject("TutorialPointer");
            Undo.RegisterCreatedObjectUndo(pointerGO, "Create TutorialPointer");
            pointer = pointerGO.AddComponent<TutorialPointerUI>();
            added++;
        }

        var soTut = new SerializedObject(tutorial);
        soTut.FindProperty("introPanel").objectReferenceValue = intro;
        soTut.FindProperty("continueButton").objectReferenceValue = intro.GetComponentInChildren<Button>(true);
        soTut.FindProperty("pointer").objectReferenceValue = pointer;
        soTut.ApplyModifiedPropertiesWithoutUndo();

        // --- Reward manager (decoration rewards every 3 heals) ----------------
        var rewardManager = managers.GetComponent<RewardManager>();
        if (rewardManager == null)
        {
            rewardManager = Undo.AddComponent<RewardManager>(managers);
            added++;
            Debug.Log("WireCfsaccaManagers: added RewardManager.");
        }
        var soReward = new SerializedObject(rewardManager);
        var virtuePoolProp = soReward.FindProperty("virtuePool");
        if (virtuePoolProp != null && virtuePoolProp.arraySize == 0)
        {
            var vGuids = AssetDatabase.FindAssets("t:VirtueDefinition", new[] { "Assets/GameData/Virtues" });
            virtuePoolProp.arraySize = vGuids.Length;
            for (int i = 0; i < vGuids.Length; i++)
                virtuePoolProp.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<VirtueDefinition>(AssetDatabase.GUIDToAssetPath(vGuids[i]));
            Debug.Log("WireCfsaccaManagers: RewardManager.virtuePool wired with " + vGuids.Length + " virtue(s).");
        }
        // Optional: only link an EditModeManager if the scene happens to have one.
        soReward.FindProperty("editModeManager").objectReferenceValue =
            Object.FindFirstObjectByType<EditModeManager>(FindObjectsInactive.Include);
        soReward.ApplyModifiedPropertiesWithoutUndo();

        // --- Level complete popup --------------------------------------------
        if (Object.FindFirstObjectByType<LevelCompletePopupUI>(FindObjectsInactive.Include) == null)
        {
            BuildLevelCompletePopup(canvas.transform, canvas.gameObject);
            added++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("WireCfsaccaManagers: done — " + added + " piece(s) added, everything else already present. Save the scene (Ctrl+S).");
    }

    private static void BuildLevelCompletePopup(Transform canvasParent, GameObject componentHost)
    {
        var popupPanel = CreateUIObject("LevelCompletePopup", canvasParent);
        StretchFull(popupPanel);
        popupPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        var box = CreateUIObject("LevelCompleteBox", popupPanel.transform);
        var boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(420f, 260f);
        box.AddComponent<Image>().color = new Color(0.1f, 0.14f, 0.22f, 0.95f);

        var title = CreateUIObject("Title", box.transform);
        var tRect = title.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0f, 1f);
        tRect.anchorMax = new Vector2(1f, 1f);
        tRect.pivot = new Vector2(0.5f, 1f);
        tRect.offsetMin = new Vector2(24f, -80f);
        tRect.offsetMax = new Vector2(-24f, -24f);
        var titleText = AddText(title, "Level Complete!", 30, TextAnchor.MiddleCenter);
        titleText.color = new Color(1f, 0.83f, 0.30f);

        var message = CreateUIObject("Message", box.transform);
        var mRect = message.GetComponent<RectTransform>();
        mRect.anchorMin = new Vector2(0f, 0.5f);
        mRect.anchorMax = new Vector2(1f, 0.5f);
        mRect.offsetMin = new Vector2(24f, -20f);
        mRect.offsetMax = new Vector2(-24f, 30f);
        var messageText = AddText(message, "Great job!", 20, TextAnchor.MiddleCenter);

        var buttonGO = CreateUIObject("ContinueButton", box.transform);
        var bRect = buttonGO.GetComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0.5f, 0f);
        bRect.anchorMax = new Vector2(0.5f, 0f);
        bRect.pivot = new Vector2(0.5f, 0f);
        bRect.anchoredPosition = new Vector2(0f, 24f);
        bRect.sizeDelta = new Vector2(200f, 48f);
        buttonGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.14f);
        var continueButton = buttonGO.AddComponent<Button>();
        AddLabel(buttonGO, "Continue", 20);

        popupPanel.SetActive(false);

        // On the always-active canvas root, not popupPanel — Start() never
        // runs on a component whose GameObject starts inactive (same gotcha
        // BuildGameplayUI documents).
        var popup = componentHost.AddComponent<LevelCompletePopupUI>();
        var so = new SerializedObject(popup);
        so.FindProperty("panel").objectReferenceValue = popupPanel;
        so.FindProperty("messageText").objectReferenceValue = messageText;
        so.FindProperty("continueButton").objectReferenceValue = continueButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("WireCfsaccaManagers: built LevelCompletePopup.");
    }

    // Development utility: wipes the local save + tutorial-seen flag so the
    // Level 1 completion path can be re-tested from a fresh start.
    [MenuItem("Tools/Radiant Orchard/Reset Save Progress")]
    private static void ResetSaveProgress()
    {
        string path = System.IO.Path.Combine(Application.persistentDataPath, "radiant_orchard_save.json");
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
            Debug.Log("ResetSaveProgress: deleted save file — " + path);
        }
        else
        {
            Debug.Log("ResetSaveProgress: no save file found at " + path);
        }

        // TutorialManager.SeenPrefKey is internal to Assembly-CSharp, so the
        // literal is repeated here rather than referenced.
        PlayerPrefs.DeleteKey("RadiantOrchard_TutorialSeen");
        PlayerPrefs.Save();
        Debug.Log("ResetSaveProgress: tutorial-seen flag cleared. Enter Play mode for a fresh Level 1.");
    }

    private static Canvas FindOrCreateUICanvas(ref int added)
    {
        var preferred = GameObject.Find("GameplayUI");
        if (preferred != null)
        {
            var preferredCanvas = preferred.GetComponent<Canvas>();
            if (preferredCanvas != null) return preferredCanvas;
        }

        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Debug.Log("WireCfsaccaManagers: reusing existing overlay canvas '" + canvas.name + "' as UI host.");
                return canvas;
            }
        }

        var go = new GameObject("GameplayUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go, "Create GameplayUI Canvas");
        var newCanvas = go.GetComponent<Canvas>();
        newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        added++;
        Debug.Log("WireCfsaccaManagers: created GameplayUI canvas (none existed).");
        return newCanvas;
    }

    private static QuizPanelUI BuildQuizPanel(Transform canvasParent)
    {
        var panel = CreateUIObject("QuizPanel", canvasParent);
        StretchFull(panel);
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        var box = CreateUIObject("QuizBox", panel.transform);
        var boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(480f, 380f);
        box.AddComponent<Image>().color = new Color(0.1f, 0.14f, 0.22f, 0.95f);

        var questionGO = CreateUIObject("Question", box.transform);
        var qRect = questionGO.GetComponent<RectTransform>();
        qRect.anchorMin = new Vector2(0f, 1f);
        qRect.anchorMax = new Vector2(1f, 1f);
        qRect.pivot = new Vector2(0.5f, 1f);
        qRect.offsetMin = new Vector2(28f, -120f);
        qRect.offsetMax = new Vector2(-28f, -28f);
        var qText = AddText(questionGO, string.Empty, 20, TextAnchor.MiddleCenter);

        var buttons = new Button[3];
        var labels = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            var opt = CreateUIObject("Option" + i, box.transform);
            var oRect = opt.GetComponent<RectTransform>();
            oRect.anchorMin = new Vector2(0f, 1f);
            oRect.anchorMax = new Vector2(1f, 1f);
            oRect.pivot = new Vector2(0.5f, 1f);
            float y = -150f - i * 56f;
            oRect.offsetMin = new Vector2(28f, y - 44f);
            oRect.offsetMax = new Vector2(-28f, y);
            opt.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.14f);
            buttons[i] = opt.AddComponent<Button>();
            labels[i] = AddLabel(opt, "Option " + (i + 1), 18);
        }

        var resultGO = CreateUIObject("ResultText", box.transform);
        var rRect = resultGO.GetComponent<RectTransform>();
        rRect.anchorMin = new Vector2(0f, 0f);
        rRect.anchorMax = new Vector2(1f, 0f);
        rRect.pivot = new Vector2(0.5f, 0f);
        rRect.offsetMin = new Vector2(28f, 20f);
        rRect.offsetMax = new Vector2(-28f, 46f);
        var rText = AddText(resultGO, string.Empty, 18, TextAnchor.MiddleCenter);
        rText.color = new Color(0.30f, 0.85f, 0.48f);
        resultGO.SetActive(false);

        panel.SetActive(false);

        var panelUI = panel.AddComponent<QuizPanelUI>();
        var so = new SerializedObject(panelUI);
        so.FindProperty("panel").objectReferenceValue = panel;
        so.FindProperty("questionText").objectReferenceValue = qText;
        var btnProp = so.FindProperty("optionButtons");
        btnProp.arraySize = buttons.Length;
        for (int i = 0; i < buttons.Length; i++)
            btnProp.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
        var lblProp = so.FindProperty("optionLabels");
        lblProp.arraySize = labels.Length;
        for (int i = 0; i < labels.Length; i++)
            lblProp.GetArrayElementAtIndex(i).objectReferenceValue = labels[i];
        so.FindProperty("resultText").objectReferenceValue = rText;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("WireCfsaccaManagers: built QuizPanel.");
        return panelUI;
    }

    private static GameObject BuildIntroPanel(Transform canvasParent)
    {
        var intro = CreateUIObject("IntroPanel", canvasParent);
        StretchFull(intro);
        intro.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        var box = CreateUIObject("IntroBox", intro.transform);
        var boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(520f, 360f);
        box.AddComponent<Image>().color = new Color(0.1f, 0.14f, 0.22f, 0.95f);

        var body = CreateUIObject("Body", box.transform);
        var bRect = body.GetComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0f, 1f);
        bRect.anchorMax = new Vector2(1f, 1f);
        bRect.pivot = new Vector2(0.5f, 1f);
        bRect.offsetMin = new Vector2(28f, -240f);
        bRect.offsetMax = new Vector2(-28f, -28f);
        var bodyText = AddText(body,
            "Tap fruits with the right gesture to brew tonics for them:\n" +
            "Double-tap  /  Hold  /  Swipe",
            18, TextAnchor.MiddleCenter);

        // A dismiss button is mandatory — TutorialManager only hides the intro
        // through continueButton.onClick, so without one the player is stuck.
        var buttonGO = CreateUIObject("ContinueButton", box.transform);
        var btnRect = buttonGO.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0f);
        btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.anchoredPosition = new Vector2(0f, 24f);
        btnRect.sizeDelta = new Vector2(200f, 48f);
        buttonGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.14f);
        buttonGO.AddComponent<Button>();
        AddLabel(buttonGO, "Got it!", 20);

        Debug.Log("WireCfsaccaManagers: built IntroPanel with dismiss button.");
        return intro;
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Text AddText(GameObject go, string content, int fontSize, TextAnchor alignment)
    {
        var text = go.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Text AddLabel(GameObject buttonGO, string content, int fontSize)
    {
        var labelGO = CreateUIObject("Label", buttonGO.transform);
        StretchFull(labelGO);
        return AddText(labelGO, content, fontSize, TextAnchor.MiddleCenter);
    }

    private static void StretchFull(GameObject go)
    {
        var rect = go.GetComponent<RectTransform>();
        if (rect == null) return;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
#endif
