#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RadiantOrchard;

// Builds the top-left Vibrancy Meter + fruit row and top-right Level/Pause UI
// from the reference mockup, wired to GameState via VibrancyMeterUI /
// LevelIndicatorUI / PauseMenuUI. Uses Layout Groups throughout instead of
// hand-placed anchors so it lays itself out correctly without needing a
// human to nudge pixel offsets in the Editor.
public static class BuildGameplayUI
{
    private const string SpritesFolder = "Assets/GameData/UI";

    private static readonly (string fruit, string hex)[] FruitOrderAndColor =
    {
        ("Strawberry", "E23D5A"), ("Pineapple", "F4C430"), ("Watermelon", "F06292"),
        ("Lemon", "F7E463"), ("Grapes", "8E44AD"), ("Apple", "E74C3C"),
        ("Peach", "FFB07C"), ("Banana", "F5D547"), ("Cherry", "C0392B"),
    };

    [MenuItem("Tools/Radiant Orchard/Build Gameplay UI")]
    private static void Build()
    {
        Undo.SetCurrentGroupName("Build Gameplay UI");
        int undoGroup = Undo.GetCurrentGroup();

        EnsureEventSystem();

        var old = GameObject.Find("GameplayUI");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var canvasGO = new GameObject("GameplayUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create GameplayUI Canvas");

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var circleSprite = UISpriteGenerator.CreateOrLoadCircleSprite(SpritesFolder + "/circle.png");
        var leafSprite = UISpriteGenerator.CreateOrLoadLeafSprite(SpritesFolder + "/leaf.png");
        var starSprite = UISpriteGenerator.CreateOrLoadStarSprite(SpritesFolder + "/star.png");
        var gradientSprite = UISpriteGenerator.CreateOrLoadGradientSprite(
            SpritesFolder + "/vibrancy_gradient.png", HexColor("FF5DA2"), HexColor("FFA940"), HexColor("4CD97B"));

        BuildVibrancyPanel(canvasGO.transform, circleSprite, leafSprite, gradientSprite);
        BuildTopRightPanel(canvasGO.transform, circleSprite, starSprite);
        BuildLevelCompletePopup(canvasGO.transform);
        BuildQuizPanel(canvasGO.transform);
        BuildTutorial(canvasGO.transform);
        BuildEditModeToggle(canvasGO.transform);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: Gameplay UI built (Vibrancy Meter + fruit row + Level pill + Pause + level-complete popup + quiz panel + tutorial + edit-mode toggle). Save the scene (Ctrl+S).");
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
    }

    private static void BuildVibrancyPanel(Transform canvasParent, Sprite circleSprite, Sprite leafSprite, Sprite gradientSprite)
    {
        var panel = CreateUIObject("VibrancyPanel", canvasParent);
        AnchorCorner(panel, new Vector2(0f, 1f), new Vector2(20f, -20f));

        AddImage(panel, new Color(0.06f, 0.1f, 0.18f, 0.72f), null);

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 12, 12);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        AddLayoutElement(panel, preferredWidth: 360f);
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 110f); // explicit fallback: LayoutElement alone has no effect on a panel with no parent layout group

        var title = CreateUIObject("Title", panel.transform);
        AddText(title, "Vibrancy Meter", 24, TextAnchor.MiddleLeft, Color.white);
        AddLayoutElement(title, preferredHeight: 28f);

        var barRow = CreateUIObject("BarRow", panel.transform);
        var barRowLayout = barRow.AddComponent<HorizontalLayoutGroup>();
        barRowLayout.spacing = 8f;
        barRowLayout.childAlignment = TextAnchor.MiddleLeft;
        barRowLayout.childControlWidth = true;
        barRowLayout.childControlHeight = true;
        barRowLayout.childForceExpandWidth = false;
        AddLayoutElement(barRow, preferredHeight: 22f);

        var barBg = CreateUIObject("BarBackground", barRow.transform);
        AddImage(barBg, new Color(1f, 1f, 1f, 0.12f), null);
        AddLayoutElement(barBg, flexibleWidth: 1f, preferredHeight: 18f);

        var fillGO = CreateUIObject("Fill", barBg.transform);
        StretchFull(fillGO);
        var fillImage = AddImage(fillGO, Color.white, gradientSprite);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 0f;

        var leaf = CreateUIObject("LeafIcon", barRow.transform);
        var leafImage = AddImage(leaf, HexColor("4CD97B"), leafSprite);
        leaf.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        AddLayoutElement(leaf, preferredWidth: 22f, preferredHeight: 22f);

        var fruitRow = CreateUIObject("FruitRow", panel.transform);
        var fruitRowLayout = fruitRow.AddComponent<HorizontalLayoutGroup>();
        fruitRowLayout.spacing = 6f;
        fruitRowLayout.childAlignment = TextAnchor.MiddleLeft;
        fruitRowLayout.childControlWidth = true;
        fruitRowLayout.childControlHeight = true;
        AddLayoutElement(fruitRow, preferredHeight: 24f);

        var fruitIcons = new Image[FruitOrderAndColor.Length];
        for (int i = 0; i < FruitOrderAndColor.Length; i++)
        {
            var icon = CreateUIObject("Fruit_" + FruitOrderAndColor[i].fruit, fruitRow.transform);
            fruitIcons[i] = AddImage(icon, HexColor(FruitOrderAndColor[i].hex), circleSprite);
            AddLayoutElement(icon, preferredWidth: 24f, preferredHeight: 24f);
        }

        var meterUI = panel.AddComponent<VibrancyMeterUI>();
        var so = new SerializedObject(meterUI);
        so.FindProperty("fillImage").objectReferenceValue = fillImage;
        var iconsProp = so.FindProperty("fruitIcons");
        iconsProp.arraySize = fruitIcons.Length;
        for (int i = 0; i < fruitIcons.Length; i++)
            iconsProp.GetArrayElementAtIndex(i).objectReferenceValue = fruitIcons[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildTopRightPanel(Transform canvasParent, Sprite circleSprite, Sprite starSprite)
    {
        var row = CreateUIObject("TopRightRow", canvasParent);
        AnchorCorner(row, new Vector2(1f, 1f), new Vector2(-20f, -20f));

        var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 10f;
        rowLayout.childAlignment = TextAnchor.MiddleRight;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        AddLayoutElement(row, preferredHeight: 48f);

        var fitter = row.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        row.GetComponent<RectTransform>().sizeDelta = new Vector2(230f, 48f); // fallback starting size before the fitter recalculates

        // Level pill
        var pill = CreateUIObject("LevelPill", row.transform);
        AddImage(pill, new Color(0.06f, 0.1f, 0.18f, 0.72f), null);
        AddLayoutElement(pill, preferredWidth: 150f, preferredHeight: 48f);

        var pillLayout = pill.AddComponent<HorizontalLayoutGroup>();
        pillLayout.padding = new RectOffset(14, 14, 8, 8);
        pillLayout.spacing = 8f;
        pillLayout.childAlignment = TextAnchor.MiddleCenter;
        pillLayout.childControlWidth = true;
        pillLayout.childControlHeight = true;

        var star = CreateUIObject("StarIcon", pill.transform);
        AddImage(star, HexColor("FFD34D"), starSprite);
        AddLayoutElement(star, preferredWidth: 22f, preferredHeight: 22f);

        var levelTextGO = CreateUIObject("LevelText", pill.transform);
        var levelText = AddText(levelTextGO, "Level 1", 22, TextAnchor.MiddleCenter, Color.white);
        AddLayoutElement(levelTextGO, flexibleWidth: 1f);

        // Pause button
        var pauseGO = CreateUIObject("PauseButton", row.transform);
        AddImage(pauseGO, new Color(0.06f, 0.1f, 0.18f, 0.72f), circleSprite);
        AddLayoutElement(pauseGO, preferredWidth: 48f, preferredHeight: 48f);
        var pauseButton = pauseGO.AddComponent<Button>();

        var pauseLabelGO = CreateUIObject("PauseLabel", pauseGO.transform);
        StretchFull(pauseLabelGO);
        AddText(pauseLabelGO, "II", 20, TextAnchor.MiddleCenter, Color.white);

        // Pause panel (hidden by default)
        var pausePanel = CreateUIObject("PausePanel", canvasParent);
        StretchFull(pausePanel);
        AddImage(pausePanel, new Color(0f, 0f, 0f, 0.6f), null);

        var pauseMenuBox = CreateUIObject("PauseMenuBox", pausePanel.transform);
        pauseMenuBox.transform.SetParent(pausePanel.transform, false);
        var boxRect = pauseMenuBox.GetComponent<RectTransform>();
        boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(360f, 220f);
        AddImage(pauseMenuBox, new Color(0.1f, 0.14f, 0.22f, 0.95f), null);

        var boxLayout = pauseMenuBox.AddComponent<VerticalLayoutGroup>();
        boxLayout.padding = new RectOffset(24, 24, 24, 24);
        boxLayout.spacing = 14f;
        boxLayout.childAlignment = TextAnchor.MiddleCenter;
        boxLayout.childControlWidth = true;
        boxLayout.childControlHeight = true;

        var pausedTitle = CreateUIObject("PausedTitle", pauseMenuBox.transform);
        AddText(pausedTitle, "Paused", 28, TextAnchor.MiddleCenter, Color.white);
        AddLayoutElement(pausedTitle, preferredHeight: 40f);

        var resumeButton = CreateButtonWithLabel(pauseMenuBox.transform, "Resume");
        var restartButton = CreateButtonWithLabel(pauseMenuBox.transform, "Restart Level");

        pausePanel.SetActive(false);

        var pauseMenu = row.gameObject.AddComponent<PauseMenuUI>();
        var so = new SerializedObject(pauseMenu);
        so.FindProperty("pausePanel").objectReferenceValue = pausePanel;
        so.FindProperty("pauseButton").objectReferenceValue = pauseButton;
        so.FindProperty("resumeButton").objectReferenceValue = resumeButton;
        so.FindProperty("restartButton").objectReferenceValue = restartButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        var levelIndicator = row.gameObject.AddComponent<LevelIndicatorUI>();
        var soLevel = new SerializedObject(levelIndicator);
        soLevel.FindProperty("levelText").objectReferenceValue = levelText;
        soLevel.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildLevelCompletePopup(Transform canvasParent)
    {
        var popupPanel = CreateUIObject("LevelCompletePopup", canvasParent);
        StretchFull(popupPanel);
        AddImage(popupPanel, new Color(0f, 0f, 0f, 0.6f), null);

        var box = CreateUIObject("LevelCompleteBox", popupPanel.transform);
        var boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(400f, 240f);
        AddImage(box, new Color(0.1f, 0.14f, 0.22f, 0.95f), null);

        var boxLayout = box.AddComponent<VerticalLayoutGroup>();
        boxLayout.padding = new RectOffset(24, 24, 24, 24);
        boxLayout.spacing = 16f;
        boxLayout.childAlignment = TextAnchor.MiddleCenter;
        boxLayout.childControlWidth = true;
        boxLayout.childControlHeight = true;

        var titleGO = CreateUIObject("Title", box.transform);
        AddText(titleGO, "Level Complete!", 30, TextAnchor.MiddleCenter, HexColor("FFD34D"));
        AddLayoutElement(titleGO, preferredHeight: 44f);

        var messageGO = CreateUIObject("Message", box.transform);
        var messageText = AddText(messageGO, "Great job!", 20, TextAnchor.MiddleCenter, Color.white);
        AddLayoutElement(messageGO, preferredHeight: 32f);

        var continueButton = CreateButtonWithLabel(box.transform, "Continue");

        popupPanel.SetActive(false);

        // Attached to the always-active canvas root, not popupPanel itself —
        // Start() never runs on a component whose GameObject starts inactive,
        // which silently stopped this from ever receiving LevelCompleted.
        var popup = canvasParent.gameObject.AddComponent<LevelCompletePopupUI>();
        var so = new SerializedObject(popup);
        so.FindProperty("panel").objectReferenceValue = popupPanel;
        so.FindProperty("messageText").objectReferenceValue = messageText;
        so.FindProperty("continueButton").objectReferenceValue = continueButton;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildQuizPanel(Transform canvasParent)
    {
        var quizPanel = CreateUIObject("QuizPanel", canvasParent);
        StretchFull(quizPanel);
        AddImage(quizPanel, new Color(0f, 0f, 0f, 0.6f), null);

        var box = CreateUIObject("QuizBox", quizPanel.transform);
        var boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(480f, 380f);
        AddImage(box, new Color(0.1f, 0.14f, 0.22f, 0.95f), null);

        var boxLayout = box.AddComponent<VerticalLayoutGroup>();
        boxLayout.padding = new RectOffset(28, 28, 24, 24);
        boxLayout.spacing = 14f;
        boxLayout.childAlignment = TextAnchor.UpperCenter;
        boxLayout.childControlWidth = true;
        boxLayout.childControlHeight = true;

        var titleGO = CreateUIObject("Title", box.transform);
        AddText(titleGO, "Quick Question", 26, TextAnchor.MiddleCenter, HexColor("FFD34D"));
        AddLayoutElement(titleGO, preferredHeight: 36f);

        var questionGO = CreateUIObject("Question", box.transform);
        var questionText = AddText(questionGO, string.Empty, 20, TextAnchor.MiddleCenter, Color.white);
        questionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        questionText.verticalOverflow = VerticalWrapMode.Overflow;
        AddLayoutElement(questionGO, preferredHeight: 90f, flexibleWidth: 1f);

        var optionButtons = new Button[3];
        var optionLabels = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            var optGO = CreateUIObject("Option" + i, box.transform);
            AddImage(optGO, new Color(1f, 1f, 1f, 0.14f), null);
            AddLayoutElement(optGO, preferredHeight: 44f, flexibleWidth: 1f);
            optionButtons[i] = optGO.AddComponent<Button>();

            var labelGO = CreateUIObject("Label", optGO.transform);
            StretchFull(labelGO);
            optionLabels[i] = AddText(labelGO, "Option " + (i + 1), 18, TextAnchor.MiddleCenter, Color.white);
        }

        var resultGO = CreateUIObject("ResultText", box.transform);
        var resultText = AddText(resultGO, string.Empty, 18, TextAnchor.MiddleCenter, HexColor("4CD97B"));
        AddLayoutElement(resultGO, preferredHeight: 26f);
        resultGO.SetActive(false);

        quizPanel.SetActive(false);

        var panelUI = quizPanel.AddComponent<QuizPanelUI>();
        var soPanel = new SerializedObject(panelUI);
        soPanel.FindProperty("panel").objectReferenceValue = quizPanel;
        soPanel.FindProperty("questionText").objectReferenceValue = questionText;
        var btnProp = soPanel.FindProperty("optionButtons");
        btnProp.arraySize = optionButtons.Length;
        for (int i = 0; i < optionButtons.Length; i++)
            btnProp.GetArrayElementAtIndex(i).objectReferenceValue = optionButtons[i];
        var lblProp = soPanel.FindProperty("optionLabels");
        lblProp.arraySize = optionLabels.Length;
        for (int i = 0; i < optionLabels.Length; i++)
            lblProp.GetArrayElementAtIndex(i).objectReferenceValue = optionLabels[i];
        soPanel.FindProperty("resultText").objectReferenceValue = resultText;
        soPanel.ApplyModifiedPropertiesWithoutUndo();

        // Attached to the always-active canvas root, not quizPanel itself —
        // same reason as LevelCompletePopupUI below: Start() never runs on a
        // component whose GameObject starts inactive.
        var quizManager = canvasParent.gameObject.AddComponent<QuizManager>();
        var soManager = new SerializedObject(quizManager);
        soManager.FindProperty("panelUI").objectReferenceValue = panelUI;
        soManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildTutorial(Transform canvasParent)
    {
        var introPanel = CreateUIObject("IntroPanel", canvasParent);
        StretchFull(introPanel);
        AddImage(introPanel, new Color(0f, 0f, 0f, 0.6f), null);

        var box = CreateUIObject("IntroBox", introPanel.transform);
        var boxRect = box.GetComponent<RectTransform>();
        boxRect.anchorMin = boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(520f, 360f);
        AddImage(box, new Color(0.1f, 0.14f, 0.22f, 0.95f), null);

        var boxLayout = box.AddComponent<VerticalLayoutGroup>();
        boxLayout.padding = new RectOffset(28, 28, 24, 24);
        boxLayout.spacing = 14f;
        boxLayout.childAlignment = TextAnchor.UpperCenter;
        boxLayout.childControlWidth = true;
        boxLayout.childControlHeight = true;

        var titleGO = CreateUIObject("Title", box.transform);
        AddText(titleGO, "Welcome!", 28, TextAnchor.MiddleCenter, HexColor("FFD34D"));
        AddLayoutElement(titleGO, preferredHeight: 40f);

        var bodyGO = CreateUIObject("Body", box.transform);
        var bodyText = AddText(bodyGO,
            "Tap fruits with the right gesture to brew tonics for them:\n" +
            "Double-tap  •  Hold  •  Swipe\n\n" +
            "Watch the glowing marker — it shows which fruit to try next and how.",
            18, TextAnchor.MiddleCenter, Color.white);
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Overflow;
        AddLayoutElement(bodyGO, preferredHeight: 190f, flexibleWidth: 1f);

        var continueButton = CreateButtonWithLabel(box.transform, "Got it!");

        var pointerGO = new GameObject("TutorialPointer");
        Undo.RegisterCreatedObjectUndo(pointerGO, "Create TutorialPointer");
        var pointer = pointerGO.AddComponent<TutorialPointerUI>();

        // Attached to the always-active canvas root, not introPanel itself —
        // same Start()-never-runs-on-an-inactive-object gotcha as the other
        // popups here.
        var tutorial = canvasParent.gameObject.AddComponent<TutorialManager>();
        var so = new SerializedObject(tutorial);
        so.FindProperty("introPanel").objectReferenceValue = introPanel;
        so.FindProperty("continueButton").objectReferenceValue = continueButton;
        so.FindProperty("pointer").objectReferenceValue = pointer;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildEditModeToggle(Transform canvasParent)
    {
        var editManagerGO = GameObject.Find("EditModeManager");
        if (editManagerGO == null)
        {
            editManagerGO = new GameObject("EditModeManager");
            Undo.RegisterCreatedObjectUndo(editManagerGO, "Create EditModeManager");
        }
        var editManager = editManagerGO.GetComponent<EditModeManager>();
        if (editManager == null) editManager = editManagerGO.AddComponent<EditModeManager>();

        // Small circular button, bottom-left — out of the way of the top rows.
        var buttonGO = CreateUIObject("EditModeButton", canvasParent);
        AnchorCorner(buttonGO, new Vector2(0f, 0f), new Vector2(20f, 20f));
        AddImage(buttonGO, new Color(0.06f, 0.1f, 0.18f, 0.72f), null);
        AddLayoutElement(buttonGO, preferredWidth: 140f, preferredHeight: 48f);
        buttonGO.GetComponent<RectTransform>().sizeDelta = new Vector2(140f, 48f);
        var button = buttonGO.AddComponent<Button>();

        var labelGO = CreateUIObject("Label", buttonGO.transform);
        StretchFull(labelGO);
        var buttonLabel = AddText(labelGO, "Edit Island", 18, TextAnchor.MiddleCenter, Color.white);

        // Hint banner, bottom-center, hidden until edit mode is active.
        var hintGO = CreateUIObject("EditHintBanner", canvasParent);
        var hintRect = hintGO.GetComponent<RectTransform>();
        hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0f, 90f);
        hintRect.sizeDelta = new Vector2(520f, 44f);
        AddImage(hintGO, new Color(0.06f, 0.1f, 0.18f, 0.85f), null);

        var hintTextGO = CreateUIObject("Text", hintGO.transform);
        StretchFull(hintTextGO);
        AddText(hintTextGO, "Drag your rewards to place them — tap Done when finished.", 16, TextAnchor.MiddleCenter, Color.white);
        hintGO.SetActive(false);

        var toggle = canvasParent.gameObject.AddComponent<EditModeToggleUI>();
        var so = new SerializedObject(toggle);
        so.FindProperty("editModeManager").objectReferenceValue = editManager;
        so.FindProperty("toggleButton").objectReferenceValue = button;
        so.FindProperty("buttonLabel").objectReferenceValue = buttonLabel;
        so.FindProperty("hintBanner").objectReferenceValue = hintGO;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Button CreateButtonWithLabel(Transform parent, string label)
    {
        var go = CreateUIObject(label.Replace(" ", "") + "Button", parent);
        AddImage(go, new Color(1f, 1f, 1f, 0.14f), null);
        AddLayoutElement(go, preferredHeight: 44f);
        var button = go.AddComponent<Button>();

        var labelGO = CreateUIObject("Label", go.transform);
        StretchFull(labelGO);
        AddText(labelGO, label, 20, TextAnchor.MiddleCenter, Color.white);

        return button;
    }

    // --- small UI-building helpers ---

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void AnchorCorner(GameObject go, Vector2 corner, Vector2 offset)
    {
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = corner;
        rect.anchorMax = corner;
        rect.pivot = corner;
        rect.anchoredPosition = offset;
    }

    private static void StretchFull(GameObject go)
    {
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Image AddImage(GameObject go, Color color, Sprite sprite)
    {
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = sprite != null ? Image.Type.Simple : Image.Type.Simple;
        return image;
    }

    private static Text AddText(GameObject go, string content, int fontSize, TextAnchor alignment, Color color)
    {
        var text = go.AddComponent<Text>();
        text.text = content;
        text.font = GetDefaultFont();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static LayoutElement AddLayoutElement(GameObject go, float preferredWidth = -1f, float preferredHeight = -1f, float flexibleWidth = -1f)
    {
        var element = go.AddComponent<LayoutElement>();
        if (preferredWidth >= 0f) element.preferredWidth = preferredWidth;
        if (preferredHeight >= 0f) element.preferredHeight = preferredHeight;
        if (flexibleWidth >= 0f) element.flexibleWidth = flexibleWidth;
        return element;
    }

    private static Font GetDefaultFont()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    private static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }
}
#endif
