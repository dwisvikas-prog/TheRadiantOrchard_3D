#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RadiantOrchard;

// Empty Island Step 1: colored CoC pad + Stone Well + Wisdom Tree only.
// Menu: Tools → CREATE CoC BLANK GROUND SCENE
public static class CreateCoCBlankScene
{
    const string ScenePath = "Assets/CoC_BlankGround.unity";

    [MenuItem("Tools/CREATE CoC BLANK GROUND SCENE", priority = 0)]
    [MenuItem("Radiant Orchard/CREATE CoC BLANK GROUND SCENE", priority = 0)]
    public static void Create()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureCheckerAssets();
        EnsureWishTreeImported();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Light
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.96f, 0.88f);
        light.intensity = 1.15f;
        lightGO.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

        // Empty Island world (colored pad + Well + Wisdom Tree only)
        CoCBlankGround.Build(Vector3.zero);
        TryUpgradeLandmarksWithPrefabs();

        var focusGo = new GameObject("CameraFocus");
        // Frame the Wisdom Tree hub
        focusGo.transform.position = new Vector3(0f, 2f, 0f);

        // Camera — CoC isometric. Ground is FIXED 44×44; camera zoom fits the DEVICE.
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        camGO.AddComponent<AudioListener>();
        var diorama = camGO.AddComponent<DioramaController>();

        float pitchDeg = 52f;
        float fovDeg = 38f;
        // CoC close-up: start near landmarks, not whole-pad overview
        float fitRadius = CoCBlankGround.PadHalf * 1.05f;
        float closeZoom = 9f;
        float farZoom = 36f;
        float startZoom = 16f;

        diorama.target = focusGo.transform;
        diorama.isometricPitch = pitchDeg;
        diorama.fieldOfView = fovDeg;
        diorama.lookDownBias = 1.8f;
        diorama.portraitFieldOfView = 34f;
        diorama.islandRadius = fitRadius;
        diorama.panMarginFraction = 0.7f;
        diorama.touchPanSpeed = 0.026f;
        diorama.mousePanSpeed = 0.10f;
        diorama.zoomSpeed = 0.020f;
        diorama.scrollZoomSpeed = 3.2f;
        diorama.zoomSmooth = 0.24f;
        diorama.moveSmooth = 0.15f;
        diorama.fruitGuardPixels = 0f;
        diorama.ConfigureCoCView(35f, closeZoom, farZoom, startZoom);

        cam.orthographic = false;
        cam.fieldOfView = fovDeg;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 300f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.45f, 0.72f, 0.91f, 1f);

        float pitch = pitchDeg * Mathf.Deg2Rad;
        float yaw = 35f * Mathf.Deg2Rad;
        camGO.transform.position = focusGo.transform.position + new Vector3(
            startZoom * Mathf.Sin(yaw) * Mathf.Cos(pitch),
            startZoom * Mathf.Sin(pitch),
            -startZoom * Mathf.Cos(yaw) * Mathf.Cos(pitch));
        camGO.transform.LookAt(focusGo.transform.position + Vector3.down * diorama.lookDownBias);

        // Step 2 systems
        WellWishController.Ensure();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        // Add to build settings (enabled) so Play works easily
        AddToBuildSettings(ScenePath);

        EditorUtility.DisplayDialog(
            "Empty Island + Wish Tree",
            "Scene ready:\n" + ScenePath + "\n\n" +
            "• CoC Wish/Wisdom Tree (Kenney Nature Kit)\n" +
            "• Stone Well + Well Wish tap\n" +
            "• CoC 44×44 pad\n\n" +
            "Rebuild if tree still old oak.",
            "OK");

        Debug.Log($"[CreateCoCBlankScene] Wish Tree + Empty Island → {ScenePath}");

        // Make Play/Stop obvious if Unity toolbar is hidden
        EmptyIslandWindow.Open();
    }

    static void EnsureWishTreeImported()
    {
        string fbx = System.IO.Path.Combine(Application.dataPath, "CoC_WishTree", "WishTree.fbx");
        if (System.IO.File.Exists(fbx))
            AssetDatabase.ImportAsset("Assets/CoC_WishTree/WishTree.fbx", ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
    }

    // Place CoC-style Wish/Wisdom Tree (Kenney Nature Kit) + scaled well.
    static void TryUpgradeLandmarksWithPrefabs()
    {
        var root = GameObject.Find(CoCBlankGround.RootName);
        if (root == null) return;
        var marks = root.transform.Find(CoCBlankGround.LandmarksName);
        if (marks == null) return;

        PlaceWishTree(marks);
        PlaceWell(marks);
    }

    static void PlaceWishTree(Transform marks)
    {
        // 1) Kenney CoC-style low-poly tree (downloaded)
        GameObject wishSrc =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CoC_WishTree/WishTree.fbx")
            ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CoC_WishTree/WishTree_Oak.fbx")
            ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CoC_WishTree/WishTree_Default.fbx")
            // 2) Project low-poly fruit tree
            ?? AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_green.prefab");

        var old = marks.Find("WisdomTree");
        Vector3 pos = old != null ? old.position : new Vector3(0f, 0f, 1.5f);
        if (old != null) Object.DestroyImmediate(old.gameObject);

        if (wishSrc == null)
        {
            CoCBlankGround.ReplaceWisdomTreeCoCScale();
            return;
        }

        GameObject inst;
        if (PrefabUtility.GetPrefabAssetType(wishSrc) != PrefabAssetType.NotAPrefab
            && PrefabUtility.IsPartOfPrefabAsset(wishSrc))
            inst = (GameObject)PrefabUtility.InstantiatePrefab(wishSrc, marks);
        else
            inst = (GameObject)Object.Instantiate(wishSrc, marks);

        inst.name = "WisdomTree";
        inst.transform.position = pos;

        // Kenney trees ≈ CoC obstacle size at ~1.0–1.2; Polytope needs ~0.55
        bool isKenney = AssetDatabase.GetAssetPath(wishSrc).Contains("CoC_WishTree");
        inst.transform.localScale = Vector3.one * (isKenney ? 1.1f : 0.55f);

        ApplyWishTreeLook(inst);
    }

    static void ApplyWishTreeLook(GameObject tree)
    {
        // Warm CoC leaf tint if materials came in grey / missing textures
        var leaf = new Color(0.18f, 0.72f, 0.22f);
        var bark = new Color(0.40f, 0.26f, 0.14f);
        var renderers = tree.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var mr = renderers[i];
            if (mr == null) continue;
            var mats = mr.sharedMaterials;
            for (int m = 0; m < mats.Length; m++)
            {
                if (mats[m] == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                    mats[m] = new Material(shader);
                }
                // Duplicate so we don't dirty shared kenney/polytope mats globally wrong
                var mat = new Material(mats[m]);
                bool trunk = mr.name.ToLowerInvariant().Contains("trunk")
                             || mr.name.ToLowerInvariant().Contains("bark")
                             || m == 0 && mats.Length > 1;
                var c = trunk ? bark : leaf;
                // Prefer real-green leaf look for Kenney (usually 1 material)
                if (mats.Length == 1) c = leaf;
                // Force vivid real green for wish tree
                if (!trunk) c = new Color(0.18f, 0.72f, 0.22f);
                mat.color = c;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.08f);
                mats[m] = mat;
            }
            mr.sharedMaterials = mats;
        }

        if (tree.GetComponentInChildren<Light>() == null)
        {
            var glow = new GameObject("WishGlow");
            glow.transform.SetParent(tree.transform, false);
            glow.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.9f, 0.45f);
            light.intensity = 1.25f;
            light.range = 5f;
        }

        // Tap collider for WellWish / future tree interact
        if (tree.GetComponentInChildren<Collider>() == null)
        {
            var col = tree.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1.0f, 0f);
            col.radius = 0.7f;
            col.height = 2.2f;
        }
    }

    static void PlaceWell(Transform marks)
    {
        // Always use reliable procedural CoC well (AssetsStore FBX was sideways / tiny).
        RadiantOrchard.StoneWellSetup.EnsureGoodWell(marks);
    }

    // Distance so the full pad (plus fringe) fits on a typical phone aspect.
    static float FitZoomForDevice(float fitRadius, float pitchDeg, float fovDeg)
    {
        float aspect = 9f / 16f; // portrait phone (CoC default)
        // Game view may be landscape in editor — still use a conservative fit
        if (Camera.current != null && Camera.current.pixelHeight > 0)
            aspect = (float)Camera.current.pixelWidth / Camera.current.pixelHeight;
        else if (Screen.height > 0)
            aspect = (float)Screen.width / Screen.height;

        float halfFov = fovDeg * 0.5f * Mathf.Deg2Rad;
        float pitch = pitchDeg * Mathf.Deg2Rad;
        // Rough ground coverage along the tighter screen axis
        float cover = fitRadius * 1.25f;
        float dist = cover / Mathf.Max(0.01f, Mathf.Tan(halfFov) / Mathf.Max(0.25f, Mathf.Cos(pitch)));
        if (aspect < 1f) dist *= 1.15f; // portrait needs a bit more distance
        return Mathf.Clamp(dist, 35f, 70f);
    }

    static void EnsureCheckerAssets()
    {
        const string folder = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Resources");

        string texPath = folder + "/CoC_BlankCheckerTex.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(texPath) == null)
        {
            int res = 128;
            int cells = 8;
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;
            int cell = res / cells;
            Color a = new Color(0.62f, 0.82f, 0.40f);
            Color b = new Color(0.48f, 0.72f, 0.32f);
            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
                tex.SetPixel(x, y, ((x / cell) + (y / cell)) % 2 == 0 ? a : b);
            tex.Apply();
            System.IO.File.WriteAllBytes(texPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(texPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
            if (importer != null)
            {
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.sRGBTexture = true;
                importer.SaveAndReimport();
            }
        }

        string matPath = folder + "/CoC_BlankChecker.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            var tint = new Color(0.55f, 0.78f, 0.38f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            mat.color = tint;
            // 44 tiles / 8 tex cells
            var tile = new Vector2(5.5f, 5.5f);
            if (mat.HasProperty("_BaseMap")) mat.SetTextureScale("_BaseMap", tile);
            if (mat.HasProperty("_MainTex")) mat.mainTextureScale = tile;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.05f);
            EditorUtility.SetDirty(mat);
        }
        AssetDatabase.SaveAssets();
    }

    static void AddToBuildSettings(string scenePath)
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list)
            if (s.path == scenePath) return;
        list.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
#endif
