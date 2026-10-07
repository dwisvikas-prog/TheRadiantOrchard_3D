#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// River (entering upper-right, curving through a lily-pad pond, exiting as
// the bottom waterfall), a second waterfall on the right cliff, two wooden
// bridges, and a dock+rowboat on the left — the remaining pieces from the
// reference spec. All procedural, own materials (see GenerateOrchardZones —
// same reasoning: no dependency on any purchased asset's baked shader).
public static class GenerateWaterFeatures
{
    [MenuItem("Tools/Radiant Orchard/Generate River, Waterfalls and Dock")]
    private static void Generate()
    {
        Undo.SetCurrentGroupName("Generate Water Features");
        int undoGroup = Undo.GetCurrentGroup();

        var root = FindOrCreateGroup("WaterFeatures");
        ClearChildren(root);

        // Matches WaterFixer.cs's own default water color (Assets/myassets/
        // WaterFixer.cs) — an existing script in the project — so the pond
        // and river read as the same water, not two different materials.
        var riverColor = new Color(0.05f, 0.35f, 0.55f, 0.75f);
        var waterfallColor = new Color(0.75f, 0.92f, 1f, 0.8f);
        var woodColor = new Color(0.45f, 0.3f, 0.16f);
        var boatColor = new Color(0.5f, 0.32f, 0.16f);

        // 0deg = "top" of the island (matches GenerateOrchardZones), clockwise.
        var riverPoints = new[]
        {
            PointOnCircle(45f, 34f),
            PointOnCircle(75f, 20f),
            PointOnCircle(140f, 11f),
            PointOnCircle(170f, 7f),  // pond center
            PointOnCircle(190f, 16f),
            PointOnCircle(180f, 39f), // reaches all the way into the waterfall column below, not stopping short of it
        };

        BuildRiverSegments(root, riverPoints, riverColor);
        BuildRiverJoints(root, riverPoints, riverColor);
        BuildPond(root, riverPoints[3], riverColor);
        BuildBridge(root, riverPoints[1], riverPoints[2], woodColor);
        BuildBridge(root, riverPoints[4], riverPoints[5], woodColor);

        BuildWaterfall(root, PointOnCircle(180f, 39f), waterfallColor); // bottom
        BuildWaterfall(root, PointOnCircle(90f, 39f), waterfallColor);  // right side

        BuildDockAndBoat(root, 270f, woodColor, boatColor); // left side

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: river, pond, 2 bridges, 2 waterfalls, dock+boat generated. Save the scene (Ctrl+S).");
    }

    private static Vector3 PointOnCircle(float angleDeg, float radius)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(rad) * radius, 0f, Mathf.Cos(rad) * radius);
    }

    private static void BuildRiverSegments(Transform parent, Vector3[] points, Color color)
    {
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];
            Vector3 mid = (a + b) * 0.5f;
            float length = Vector3.Distance(a, b) * 1.05f;
            float width = Mathf.Lerp(3f, 5f, i / (float)(points.Length - 2));

            var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(seg, "Create River Segment");
            seg.name = "RiverSegment_" + i;
            seg.transform.SetParent(parent, false);
            seg.transform.localPosition = mid + new Vector3(0f, 0.15f, 0f);
            seg.transform.localRotation = Quaternion.LookRotation(b - a, Vector3.up);
            seg.transform.localScale = new Vector3(width, 0.1f, length);
            ApplyAnimatedWaterMaterial(seg, color, length * 0.5f);
            Object.DestroyImmediate(seg.GetComponent<Collider>());
        }
    }

    // Each straight segment is independently rotated to its own two
    // endpoints, so at sharper turns the outer edge of one segment doesn't
    // meet the next — leaving a visible notch/gap right at the joint. A
    // round water disc at every interior waypoint covers that regardless of
    // the angle mismatch. The entry point also gets a small rock so the
    // river doesn't visually start from nothing.
    private static void BuildRiverJoints(Transform parent, Vector3[] points, Color color)
    {
        for (int i = 1; i < points.Length - 1; i++)
        {
            if (i == 3) continue; // the pond (built separately) already covers this waypoint

            var joint = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(joint, "Create River Joint");
            joint.name = "RiverJoint_" + i;
            joint.transform.SetParent(parent, false);
            joint.transform.localPosition = points[i] + new Vector3(0f, 0.15f, 0f);
            float width = Mathf.Lerp(3f, 5f, i / (float)(points.Length - 2));
            joint.transform.localScale = new Vector3(width * 1.1f, 0.1f, width * 1.1f);
            ApplyAnimatedWaterMaterial(joint, color, 1f);
            Object.DestroyImmediate(joint.GetComponent<Collider>());
        }

        var source = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Undo.RegisterCreatedObjectUndo(source, "Create River Source Rock");
        source.name = "RiverSource";
        source.transform.SetParent(parent, false);
        source.transform.localPosition = points[0] + new Vector3(0f, 0.4f, 0f);
        source.transform.localScale = new Vector3(2.5f, 1.2f, 2.5f);
        ApplyColor(source, new Color(0.4f, 0.38f, 0.36f));
        Object.DestroyImmediate(source.GetComponent<Collider>());
    }

    private static void BuildPond(Transform parent, Vector3 center, Color color)
    {
        var pond = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(pond, "Create Pond");
        pond.name = "Pond";
        pond.transform.SetParent(parent, false);
        pond.transform.localPosition = center + new Vector3(0f, 0.12f, 0f);
        pond.transform.localScale = new Vector3(9f, 0.05f, 9f);
        ApplyAnimatedWaterMaterial(pond, color, 3f);
        Object.DestroyImmediate(pond.GetComponent<Collider>());

        // Still water gets a radial ripple (ConcentricRippleEffect, Assets/
        // myassets/effects/PondWaterEffect.cs) instead of the river's
        // directional scroll — WaterScroll from the material call above is
        // replaced here since a pond shouldn't look like it's "flowing" one way.
        var linearScroll = pond.GetComponent<WaterScroll>();
        if (linearScroll != null) Object.DestroyImmediate(linearScroll);
        var ripple = pond.AddComponent<ConcentricRippleEffect>();
        ripple.rippleSpeed = 3f;
        ripple.frequency = 14f;
        ripple.amplitude = 0.015f;

        for (int i = 0; i < 5; i++)
        {
            float angle = i * 72f * Mathf.Deg2Rad;
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(pad, "Create Lily Pad");
            pad.name = "LilyPad_" + i;
            pad.transform.SetParent(parent, false);
            pad.transform.localPosition = center + new Vector3(Mathf.Cos(angle) * 3f, 0.2f, Mathf.Sin(angle) * 3f);
            pad.transform.localScale = new Vector3(0.6f, 0.02f, 0.6f);
            ApplyColor(pad, new Color(0.2f, 0.5f, 0.22f));
            Object.DestroyImmediate(pad.GetComponent<Collider>());
        }
    }

    private static void BuildBridge(Transform parent, Vector3 a, Vector3 b, Color woodColor)
    {
        Vector3 mid = (a + b) * 0.5f;
        Vector3 dir = (b - a).normalized;
        Quaternion spanRotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(0f, 90f, 0f);

        var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(deck, "Create Bridge Deck");
        deck.name = "BridgeDeck";
        deck.transform.SetParent(parent, false);
        deck.transform.localPosition = mid + new Vector3(0f, 0.3f, 0f);
        deck.transform.localRotation = spanRotation;
        deck.transform.localScale = new Vector3(1.8f, 0.15f, 6f);
        ApplyColor(deck, woodColor);
        Object.DestroyImmediate(deck.GetComponent<Collider>());

        Vector3 spanAxis = Vector3.Cross(dir, Vector3.up).normalized;
        foreach (var side in new[] { -1f, 1f })
        {
            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(rail, "Create Bridge Rail");
            rail.name = "BridgeRail";
            rail.transform.SetParent(parent, false);
            rail.transform.localPosition = mid + spanAxis * side * 2.8f + new Vector3(0f, 0.65f, 0f);
            rail.transform.localRotation = spanRotation;
            rail.transform.localScale = new Vector3(0.1f, 0.5f, 6f);
            ApplyColor(rail, woodColor);
            Object.DestroyImmediate(rail.GetComponent<Collider>());
        }
    }

    private static void BuildWaterfall(Transform parent, Vector3 edgePoint, Color color)
    {
        var fall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(fall, "Create Waterfall");
        fall.name = "Waterfall";
        fall.transform.SetParent(parent, false);
        Vector3 outward = edgePoint.normalized;
        fall.transform.localPosition = edgePoint + outward * 1.5f + new Vector3(0f, -5f, 0f);
        fall.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);
        fall.transform.localScale = new Vector3(6f, 12f, 0.3f);
        ApplyAnimatedWaterMaterial(fall, color, 6f);
        var scroll = fall.GetComponent<WaterScroll>();
        if (scroll != null) scroll.scrollSpeedY = 1.4f; // faster than the river — falling water, not drifting
        Object.DestroyImmediate(fall.GetComponent<Collider>());
    }

    // Water sits at y=-3.5 (IslandSceneGenerator's waterLevelOffset), well
    // below the island edge at y=0 — a flat plank out there floats in mid-air
    // with nothing under it. This ramps down from the edge to the actual
    // water surface instead, on support posts, ending in a small platform
    // with the boat tied up at the water line.
    private static void BuildDockAndBoat(Transform parent, float angleDeg, Color woodColor, Color boatColor)
    {
        const float waterY = -3.5f;
        Vector3 outward = PointOnCircle(angleDeg, 1f);

        Vector3 startTop = PointOnCircle(angleDeg, 36f); // still on the island edge, y=0
        Vector3 endAtWater = PointOnCircle(angleDeg, 47f);
        endAtWater.y = waterY + 0.15f; // deck sits just above the water surface

        Vector3 mid = (startTop + endAtWater) * 0.5f;
        float rampLength = Vector3.Distance(startTop, endAtWater);

        var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(ramp, "Create Dock Ramp");
        ramp.name = "Dock";
        ramp.transform.SetParent(parent, false);
        ramp.transform.localPosition = mid;
        ramp.transform.localRotation = Quaternion.LookRotation(endAtWater - startTop, Vector3.up);
        ramp.transform.localScale = new Vector3(2f, 0.15f, rampLength);
        ApplyColor(ramp, woodColor);
        Object.DestroyImmediate(ramp.GetComponent<Collider>());

        // A short flat platform at the water end, easier to read as "dock"
        // than the ramp's sloped end alone
        var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(platform, "Create Dock Platform");
        platform.name = "DockPlatform";
        platform.transform.SetParent(parent, false);
        platform.transform.localPosition = endAtWater + outward * 2f;
        platform.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up);
        platform.transform.localScale = new Vector3(3f, 0.15f, 4f);
        ApplyColor(platform, woodColor);
        Object.DestroyImmediate(platform.GetComponent<Collider>());

        // Support posts under the ramp, each reaching down to the water
        for (int i = 1; i <= 3; i++)
        {
            float t = i / 4f;
            Vector3 topPoint = Vector3.Lerp(startTop, endAtWater, t);
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(post, "Create Dock Post");
            post.name = "DockPost_" + i;
            post.transform.SetParent(parent, false);
            float postHeight = Mathf.Max(0.3f, topPoint.y - waterY);
            post.transform.localPosition = new Vector3(topPoint.x, waterY + postHeight * 0.5f, topPoint.z);
            post.transform.localScale = new Vector3(0.15f, postHeight * 0.5f, 0.15f);
            ApplyColor(post, new Color(woodColor.r * 0.8f, woodColor.g * 0.8f, woodColor.b * 0.8f));
            Object.DestroyImmediate(post.GetComponent<Collider>());
        }

        var boat = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Undo.RegisterCreatedObjectUndo(boat, "Create Rowboat");
        boat.name = "Rowboat";
        boat.transform.SetParent(parent, false);
        boat.transform.localPosition = endAtWater + outward * 5f + new Vector3(0f, waterY - (endAtWater.y) + 0.05f, 0f);
        boat.transform.localRotation = Quaternion.LookRotation(outward, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
        boat.transform.localScale = new Vector3(1.1f, 2f, 0.6f);
        ApplyColor(boat, boatColor);
        Object.DestroyImmediate(boat.GetComponent<Collider>());
    }

    // Ripple pattern + WaterScroll.cs (already in the project, Assets/myassets/
    // effects/WaterScroll.cs) scrolling its UV each frame — a plain color
    // material has nothing to scroll, so this is what actually makes the
    // water read as flowing instead of static.
    private static void ApplyAnimatedWaterMaterial(GameObject go, Color tint, float tileLength)
    {
        bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        var shader = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");
        var mat = new Material(shader) { color = tint };
        mat.mainTexture = CreateRippleTexture();
        mat.mainTextureScale = new Vector2(1f, Mathf.Max(1f, tileLength));

        mat.SetFloat(isURP ? "_Smoothness" : "_Glossiness", 0.7f);
        if (isURP)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        else
        {
            mat.SetFloat("_Mode", 3f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
        }
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sharedMaterial = mat;

        var scroll = go.GetComponent<WaterScroll>();
        if (scroll == null) scroll = go.AddComponent<WaterScroll>();
        scroll.scrollSpeedX = 0f;
        scroll.scrollSpeedY = 0.35f;
    }

    private static Texture2D CreateRippleTexture()
    {
        const int width = 16, height = 64;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;

        var deep = new Color(0.22f, 0.5f, 0.8f);
        var light = new Color(0.6f, 0.85f, 0.98f);

        for (int y = 0; y < height; y++)
        {
            float wave = (Mathf.Sin(y / (float)height * Mathf.PI * 8f) + 1f) * 0.5f;
            var rowColor = Color.Lerp(deep, light, wave * 0.55f);
            for (int x = 0; x < width; x++)
                tex.SetPixel(x, y, rowColor);
        }

        tex.Apply();
        return tex;
    }

    private static void ApplyColor(GameObject go, Color color, bool transparent = false)
    {
        bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        var shader = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");
        var mat = new Material(shader) { color = color };

        if (transparent)
        {
            if (isURP)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                mat.SetFloat("_Mode", 3f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_ALPHABLEND_ON");
            }
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sharedMaterial = mat;
    }

    private static Transform FindOrCreateGroup(string groupName)
    {
        var go = GameObject.Find(groupName);
        if (go == null)
        {
            go = new GameObject(groupName);
            Undo.RegisterCreatedObjectUndo(go, "Create " + groupName);
        }
        return go.transform;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
    }
}
#endif
