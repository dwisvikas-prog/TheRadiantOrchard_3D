#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RadiantOrchard;

// Tools → Radiant Orchard → Fix Main Scene Framing
// Also auto-runs once after script compile if Main scene is open (see flag below).
public static class FixMainSceneFraming
{
    const string MainScenePath = "Assets/The Main RadiantOrchard_3d.unity";
    const string AutoFlagKey = "RadiantOrchard_AutoFixedMainFraming_v12_cocdefense";

    [MenuItem("Tools/Radiant Orchard/Fix Main Scene Framing")]
    public static void RunFromMenu()
    {
        Apply(openAndSave: true);
        EditorUtility.DisplayDialog(
            "Main Scene Framing",
            "Done!\n\nGround is square + props kept inside.\n1) Press Play\n2) Game view = 16:9 Landscape",
            "OK");
    }

    [MenuItem("Tools/Radiant Orchard/Make Main Ground Square")]
    public static void MakeGroundSquareFromMenu()
    {
        Apply(openAndSave: true);
        EditorUtility.DisplayDialog(
            "Main Ground Square",
            "Main scene ground is now a square.\nNothing playable should stick outside.\nPress Play to check.",
            "OK");
    }

    public static void RunBatch() => Apply(openAndSave: true);

    // When user presses Play on Main scene, fix framing FIRST so they don't
    // need to find any menu. This is the reliable path while the scene is binary.
    [InitializeOnLoadMethod]
    static void HookPlayMode()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;

        EditorApplication.delayCall += TryAutoFixOpenMainScene;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode) return;
        var scene = EditorSceneManager.GetActiveScene();
        if (!IsMainScene(scene)) return;
        Debug.Log("[FixMainSceneFraming] Play pressed — applying framing before Play...");
        Apply(openAndSave: true);
    }

    static void TryAutoFixOpenMainScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (SessionState.GetBool(AutoFlagKey, false)) return;
        var scene = EditorSceneManager.GetActiveScene();
        if (!IsMainScene(scene)) return;
        SessionState.SetBool(AutoFlagKey, true);
        Apply(openAndSave: true);
        Debug.Log("[FixMainSceneFraming] Auto-applied to open Main scene. Press Play.");
    }

    static bool IsMainScene(UnityEngine.SceneManagement.Scene scene)
    {
        if (!scene.IsValid()) return false;
        string path = (scene.path ?? "").Replace('\\', '/');
        string name = scene.name ?? "";
        return path.EndsWith("The Main RadiantOrchard_3d.unity") ||
               name.IndexOf("RadiantOrchard_3d", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("Main Radiant", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // After scripts recompile, if Main scene is already open, fix it automatically
    // so the user does not need to hunt for the menu.
    static void Apply(bool openAndSave)
    {
        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

        TameEnvironmentGiants();
        Bounds islandBounds = MainSceneSquareGround.Apply();
        Vector3 focusPoint = new Vector3(islandBounds.center.x,
            Mathf.Clamp(islandBounds.center.y, 1f, 8f), islandBounds.center.z);
        FrameMainCamera(focusPoint, islandBounds);
        FixStickmanMaterials();
        EnsureOrientationLock();

        EditorSceneManager.MarkSceneDirty(scene);
        if (openAndSave)
        {
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[FixMainSceneFraming] Saved '{MainScenePath}'. focus={focusPoint} size={islandBounds.size}");
        }
    }

    static void TameEnvironmentGiants()
    {
        string[] hideNames =
        {
            "OceanDepth", "ShallowRing", "HorizonHaze", "SkyZenith",
            "Sky_Horizon", "Sky_Mid", "Sky_Top", "OceanFar"
        };

        foreach (var name in hideNames)
        {
            foreach (var go in FindAllByName(name))
            {
                var t = go.transform;
                Vector3 s = t.localScale;
                bool ridiculous = s.x > 100f || s.y > 60f || s.z > 100f || t.position.y > 30f;

                if (ridiculous && (name.StartsWith("Sky_") || name == "SkyZenith" || name == "HorizonHaze"))
                {
                    foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                    {
                        Undo.RecordObject(r, "Hide sky band");
                        r.enabled = false;
                    }
                }
                else if (name == "OceanDepth")
                {
                    Undo.RecordObject(t, "OceanDepth");
                    t.localScale = new Vector3(Mathf.Min(s.x, 55f), Mathf.Min(s.y, 22f), Mathf.Min(s.z, 55f));
                    if (t.position.y > -5f)
                        t.position = new Vector3(t.position.x, -30f, t.position.z);
                }

                foreach (var col in go.GetComponentsInChildren<Collider>(true))
                    Object.DestroyImmediate(col);
            }
        }

        foreach (var ocean in FindAllByName("OceanSurface"))
        {
            Undo.RecordObject(ocean.transform, "Ocean height");
            if (ocean.transform.position.y > -2f)
                ocean.transform.position = new Vector3(ocean.transform.position.x, -10f, ocean.transform.position.z);
        }
    }

    static Bounds ComputeIslandBounds(out Vector3 focus)
    {
        var renderers = new List<Renderer>();
        string[] roots = { "Island", "FloatingIsland", "Orchard", "Ground", "PlayableIsland", "RadiantOrchard" };
        foreach (var rootName in roots)
        {
            var root = GameObject.Find(rootName);
            if (root == null) continue;
            renderers.AddRange(root.GetComponentsInChildren<Renderer>(true));
        }

        if (renderers.Count < 5)
        {
            foreach (var r in Object.FindObjectsByType<Renderer>())
            {
                if (r == null) continue;
                string path = GetPath(r.transform);
                if (path.Contains("Environment_SeaSky")) continue;
                if (r.name.StartsWith("Sky") || r.name.StartsWith("Ocean") || r.name.Contains("Horizon")) continue;
                if (r is SpriteRenderer) continue;
                if (r.bounds.size.magnitude > 400f) continue;
                renderers.Add(r);
            }
        }

        if (renderers.Count == 0)
        {
            focus = new Vector3(0f, 3f, 0f);
            return new Bounds(focus, new Vector3(40f, 10f, 40f));
        }

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Count; i++)
            b.Encapsulate(renderers[i].bounds);

        focus = new Vector3(b.center.x, Mathf.Clamp(b.center.y, 1f, 8f), b.center.z);
        return b;
    }

    static void FrameMainCamera(Vector3 focusPoint, Bounds islandBounds)
    {
        var cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            cam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            go.AddComponent<AudioListener>();
            Undo.RegisterCreatedObjectUndo(go, "Create Main Camera");
        }
        if (cam.CompareTag("Untagged")) cam.tag = "MainCamera";

        var focusGo = GameObject.Find("CameraFocus");
        if (focusGo == null)
        {
            focusGo = new GameObject("CameraFocus");
            Undo.RegisterCreatedObjectUndo(focusGo, "Create CameraFocus");
        }
        Undo.RecordObject(focusGo.transform, "Place CameraFocus");
        focusGo.transform.position = focusPoint;

        var orbit = cam.GetComponent<CameraOrbitController>();
        if (orbit != null) { Undo.RecordObject(orbit, "Disable orbit"); orbit.enabled = false; }
        var flyover = cam.GetComponent<IntroCameraFlyover>();
        if (flyover != null) { Undo.RecordObject(flyover, "Disable flyover"); flyover.enabled = false; }

        var diorama = cam.GetComponent<DioramaController>();
        if (diorama == null) diorama = Undo.AddComponent<DioramaController>(cam.gameObject);

        float radius = Mathf.Max(islandBounds.extents.x, islandBounds.extents.z, MainSceneSquareGround.GrassHalf);
        float pitchDeg = 52f;
        float fovDeg = 40f;
        float fitRadius = MainSceneSquareGround.GrassHalf * 1.25f;
        float closeZoom = 22f;
        float farZoom = Mathf.Clamp(fitRadius * 1.85f, 90f, 130f);
        float startZoom = farZoom * 0.92f;

        Undo.RecordObject(diorama, "Configure DioramaController");
        diorama.target = focusGo.transform;
        diorama.isometricPitch = pitchDeg;
        diorama.fieldOfView = fovDeg;
        diorama.lookDownBias = Mathf.Clamp(fitRadius * 0.06f, 2f, 6f);
        diorama.portraitFieldOfView = 34f;
        diorama.islandRadius = fitRadius;
        diorama.panMarginFraction = 0.7f;
        diorama.touchPanSpeed = 0.035f;
        diorama.mousePanSpeed = 0.12f;
        diorama.zoomSpeed = 0.08f;
        diorama.scrollZoomSpeed = 8f;
        diorama.fruitGuardPixels = 40f;

        Undo.RecordObject(cam, "Frame camera");
        cam.orthographic = false;
        cam.fieldOfView = fovDeg;
        cam.rect = new Rect(0f, 0f, 1f, 1f);
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 600f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.45f, 0.72f, 0.91f, 1f);

        const float cocYawDeg = 35f;
        diorama.ConfigureCoCView(cocYawDeg, closeZoom, farZoom, startZoom);

        float pitch = pitchDeg * Mathf.Deg2Rad;
        float yaw = cocYawDeg * Mathf.Deg2Rad;
        Undo.RecordObject(cam.transform, "Place camera");
        cam.transform.position = focusPoint + new Vector3(
            startZoom * Mathf.Sin(yaw) * Mathf.Cos(pitch),
            startZoom * Mathf.Sin(pitch),
            -startZoom * Mathf.Cos(yaw) * Mathf.Cos(pitch));
        cam.transform.LookAt(focusPoint + Vector3.down * diorama.lookDownBias);
    }

    static void FixStickmanMaterials()
    {
        var grey = AssetDatabase.LoadAssetAtPath<Material>("Assets/GameData/Materials/Stickman_Grey.mat");
        var color = AssetDatabase.LoadAssetAtPath<Material>("Assets/GameData/Materials/Stickman_Color.mat");
        if (grey == null) return;

        foreach (var sm in Object.FindObjectsByType<StickmanController>(FindObjectsInactive.Include))
        {
            var so = new SerializedObject(sm);
            var greyProp = so.FindProperty("greyMat");
            var colorProp = so.FindProperty("colorMat");
            if (greyProp != null && greyProp.objectReferenceValue == null)
                greyProp.objectReferenceValue = grey;
            if (colorProp != null && colorProp.objectReferenceValue == null && color != null)
                colorProp.objectReferenceValue = color;
            so.ApplyModifiedPropertiesWithoutUndo();

            foreach (var r in sm.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                bool broken = r.sharedMaterial == null ||
                              r.sharedMaterial.shader == null ||
                              r.sharedMaterial.shader.name.Contains("InternalError");
                if (broken)
                {
                    Undo.RecordObject(r, "Fix stickman mat");
                    r.sharedMaterial = grey;
                }
            }
        }
    }

    static void EnsureOrientationLock()
    {
        if (Object.FindAnyObjectByType<OrientationLock>() != null) return;
        var managers = GameObject.Find("GameManagers");
        if (managers == null)
        {
            managers = new GameObject("GameManagers");
            Undo.RegisterCreatedObjectUndo(managers, "GameManagers");
        }
        Undo.AddComponent<OrientationLock>(managers);
    }

    static List<GameObject> FindAllByName(string name)
    {
        var list = new List<GameObject>();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (t != null && t.name == name) list.Add(t.gameObject);
        return list;
    }

    static string GetPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}
#endif
