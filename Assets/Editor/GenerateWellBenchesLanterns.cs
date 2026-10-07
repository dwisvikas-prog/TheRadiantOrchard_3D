#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Linq;

// Stone well near the central tree, 3 rest benches along the inner path
// ring, and lantern posts spaced along the 4 main path branches. The well
// has a real matching asset in the project; benches and lanterns don't
// (audited — no bench/lamp/lantern/torch prop anywhere), so those are
// built procedurally in the same wood-primitive style the fences, signs,
// and dock already use.
public static class GenerateWellBenchesLanterns
{
    [MenuItem("Tools/Radiant Orchard/Generate Well, Benches, Lanterns")]
    private static void Generate()
    {
        if (Application.isPlaying) { Debug.LogError("Stop Play mode before running this."); return; }

        Undo.SetCurrentGroupName("Generate Well, Benches, Lanterns");
        int undoGroup = Undo.GetCurrentGroup();

        var group = FindOrCreateGroup("VillageProps");

        PlaceWell(group);
        PlaceBenches(group);
        PlaceLanterns(group);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: well, benches, and lanterns placed. Save the scene (Ctrl+S).");
    }

    // ---------- Well ----------
    static void PlaceWell(Transform group)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AssetsStore/Weel/Assets/Prefabs/well.prefab");
        if (prefab == null) { Debug.LogWarning("well.prefab not found — skipping well."); return; }

        var wisdomTree = GameObject.Find("WisdomTree");
        Vector3 pos = (wisdomTree != null ? wisdomTree.transform.position : Vector3.zero) + new Vector3(0f, 0f, 5f);

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
        Undo.RegisterCreatedObjectUndo(inst, "Create Well");
        inst.name = "StoneWell";
        inst.transform.position = pos;
        inst.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
    }

    // ---------- Benches ----------
    static void PlaceBenches(Transform group)
    {
        var pathways = GameObject.Find("Pathways");
        if (pathways == null) { Debug.LogWarning("Pathways group not found — skipping benches."); return; }

        string[] spots = { "PathRing14_0", "PathRing14_12", "PathRing14_24" };
        var woodSeat = new Color(0.5f, 0.35f, 0.2f);
        var woodLeg = new Color(0.35f, 0.24f, 0.14f);

        int benchIndex = 0;
        foreach (var spotName in spots)
        {
            var spot = pathways.transform.Find(spotName);
            if (spot == null) continue;

            var bench = new GameObject("Bench_" + benchIndex);
            Undo.RegisterCreatedObjectUndo(bench, "Create Bench");
            bench.transform.SetParent(group, false);

            // Push it slightly off the path centerline so it doesn't block walking,
            // facing back toward the path/tree.
            Vector3 outward = (spot.position - (GameObject.Find("WisdomTree") != null ? GameObject.Find("WisdomTree").transform.position : Vector3.zero));
            outward.y = 0f;
            outward = outward.sqrMagnitude > 0.01f ? outward.normalized : Vector3.forward;
            bench.transform.position = spot.position + outward * 1.6f;
            bench.transform.rotation = Quaternion.LookRotation(-outward, Vector3.up);

            var seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(seat, "Create Bench Seat");
            seat.name = "Seat";
            seat.transform.SetParent(bench.transform, false);
            seat.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            seat.transform.localScale = new Vector3(1.5f, 0.1f, 0.5f);
            ApplyColor(seat, woodSeat);
            Object.DestroyImmediate(seat.GetComponent<Collider>());

            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(back, "Create Bench Backrest");
            back.name = "Backrest";
            back.transform.SetParent(bench.transform, false);
            back.transform.localPosition = new Vector3(0f, 0.75f, -0.22f);
            back.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
            back.transform.localScale = new Vector3(1.5f, 0.55f, 0.08f);
            ApplyColor(back, woodSeat);
            Object.DestroyImmediate(back.GetComponent<Collider>());

            foreach (float side in new[] { -1f, 1f })
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(leg, "Create Bench Leg");
                leg.name = "Leg";
                leg.transform.SetParent(bench.transform, false);
                leg.transform.localPosition = new Vector3(side * 0.65f, 0.2f, 0f);
                leg.transform.localScale = new Vector3(0.12f, 0.4f, 0.45f);
                ApplyColor(leg, woodLeg);
                Object.DestroyImmediate(leg.GetComponent<Collider>());
            }

            benchIndex++;
        }
    }

    // ---------- Lantern posts ----------
    static void PlaceLanterns(Transform group)
    {
        var pathways = GameObject.Find("Pathways");
        if (pathways == null) { Debug.LogWarning("Pathways group not found — skipping lanterns."); return; }

        string[] branches = { "PathBranch0", "PathBranch90", "PathBranch180", "PathBranch270" };
        var woodPost = new Color(0.32f, 0.22f, 0.13f);
        var lanternGlow = new Color(1f, 0.78f, 0.4f);
        int lanternIndex = 0;

        foreach (var branch in branches)
        {
            for (int i = 1; i <= 10; i += 3)
            {
                var spot = pathways.transform.Find(branch + "_" + i);
                if (spot == null) continue;

                Vector3 along = Vector3.zero;
                var next = pathways.transform.Find(branch + "_" + (i + 1));
                var prev = pathways.transform.Find(branch + "_" + (i - 1));
                if (next != null) along = next.position - spot.position;
                else if (prev != null) along = spot.position - prev.position;
                if (along.sqrMagnitude < 0.01f) along = Vector3.forward;
                Vector3 side = Vector3.Cross(along.normalized, Vector3.up);

                float sign = lanternIndex % 2 == 0 ? 1f : -1f;
                Vector3 pos = spot.position + side * sign * 1.8f;

                BuildLanternPost(group, pos, woodPost, lanternGlow, lanternIndex);
                lanternIndex++;
            }
        }
    }

    static void BuildLanternPost(Transform group, Vector3 pos, Color woodColor, Color glowColor, int index)
    {
        var post = new GameObject("LanternPost_" + index);
        Undo.RegisterCreatedObjectUndo(post, "Create Lantern Post");
        post.transform.SetParent(group, false);
        post.transform.position = pos;

        var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(pole, "Create Lantern Pole");
        pole.name = "Pole";
        pole.transform.SetParent(post.transform, false);
        pole.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        pole.transform.localScale = new Vector3(0.12f, 1.0f, 0.12f);
        ApplyColor(pole, woodColor);
        Object.DestroyImmediate(pole.GetComponent<Collider>());

        var arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(arm, "Create Lantern Arm");
        arm.name = "Arm";
        arm.transform.SetParent(post.transform, false);
        arm.transform.localPosition = new Vector3(0.35f, 1.95f, 0f);
        arm.transform.localScale = new Vector3(0.7f, 0.08f, 0.08f);
        ApplyColor(arm, woodColor);
        Object.DestroyImmediate(arm.GetComponent<Collider>());

        var lantern = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(lantern, "Create Lantern Body");
        lantern.name = "LanternBody";
        lantern.transform.SetParent(post.transform, false);
        lantern.transform.localPosition = new Vector3(0.65f, 1.72f, 0f);
        lantern.transform.localScale = new Vector3(0.22f, 0.3f, 0.22f);
        var lanternMat = ApplyColor(lantern, glowColor);
        if (lanternMat.HasProperty("_EmissionColor"))
        {
            lanternMat.EnableKeyword("_EMISSION");
            lanternMat.SetColor("_EmissionColor", glowColor * 0.9f);
        }
        Object.DestroyImmediate(lantern.GetComponent<Collider>());

        var light = new GameObject("LanternLight");
        Undo.RegisterCreatedObjectUndo(light, "Create Lantern Light");
        light.transform.SetParent(post.transform, false);
        light.transform.localPosition = new Vector3(0.65f, 1.72f, 0f);
        var lightComp = light.AddComponent<Light>();
        lightComp.type = LightType.Point;
        lightComp.color = glowColor;
        lightComp.intensity = 1.3f;
        lightComp.range = 6f;
        lightComp.shadows = LightShadows.None;
    }

    // ---------- helpers ----------
    static Material ApplyColor(GameObject go, Color color)
    {
        bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        var shader = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");
        var renderer = go.GetComponent<MeshRenderer>();
        var mat = new Material(shader) { color = color };
        if (renderer != null) renderer.sharedMaterial = mat;
        return mat;
    }

    static Transform FindOrCreateGroup(string groupName)
    {
        var go = GameObject.Find(groupName);
        if (go != null) return go.transform;
        go = new GameObject(groupName);
        Undo.RegisterCreatedObjectUndo(go, "Create " + groupName);
        return go.transform;
    }
}
#endif
