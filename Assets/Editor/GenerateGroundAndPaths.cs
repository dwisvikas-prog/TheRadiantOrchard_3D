#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// A proper square grass ground under the whole island (the path-ring system
// alone had nothing solid beneath it before this), plus recolors the
// existing dirt/stone path pieces to a worn dirt-brown so they read as a
// trail cutting through grass rather than a paved plaza.
public static class GenerateGroundAndPaths
{
    [MenuItem("Tools/Radiant Orchard/Generate Grass Ground + Dirt Paths")]
    private static void Generate()
    {
        Undo.SetCurrentGroupName("Generate Grass Ground");
        int undoGroup = Undo.GetCurrentGroup();

        bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        var shader = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");

        var existing = GameObject.Find("GrassGround");
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(ground, "Create Grass Ground");
        ground.name = "GrassGround";
        // Cube is 1x1x1 at scale 1. Top surface sits at position.y + scale.y/2 —
        // keep that below the path pieces' lowest point (~y=0.02).
        ground.transform.position = new Vector3(0f, -0.31f, 0f);
        // Side length 84 matches the old disc diameter (islandRadius 42 * 2).
        // Height 0.6 matches the old cylinder (height = 2 * scale.y with y=0.3).
        ground.transform.localScale = new Vector3(84f, 0.6f, 84f);

        // Alpine-meadow look: UNS_Terrain_Grass.png turned out to be a flat
        // solid color (no blade detail at all — that's why the ground read as
        // plastic/candy-green), so this uses the Free Japanese Garden pack's
        // real photographed grass instead (olive/muted blades, not lush
        // lowland green) plus its Normal + AO maps for actual surface detail,
        // with a near-neutral tint instead of the old pale pastel multiply.
        const string fjgGrass = "Assets/Waldemarst/FreeJapaneseGarden/Textures/Terrain/Ground_Grass_01/T_FJG_Ground_Grass_01_";
        var grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>(fjgGrass + "Albedo.png");
        var grassNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(fjgGrass + "Normal.png");
        var grassAO = AssetDatabase.LoadAssetAtPath<Texture2D>(fjgGrass + "AO.png");

        // FIX: Dark forest green (#2D5A27 = 0.176, 0.353, 0.153) instead of
        // near-white tint — the old value multiplied the photo texture's own
        // green by ~1, leaving the candy-bright result unchanged.
        var grassMat = new Material(shader) { color = new Color(0.176f, 0.353f, 0.153f) };
        if (grassTex != null)
        {
            grassMat.mainTexture = grassTex;
            grassMat.mainTextureScale = new Vector2(24f, 24f);
        }
        if (grassNormal != null)
        {
            grassMat.SetTexture("_BumpMap", grassNormal);
            grassMat.EnableKeyword("_NORMALMAP");
        }
        if (grassAO != null)
        {
            grassMat.SetTexture("_OcclusionMap", grassAO);
            grassMat.EnableKeyword("_OCCLUSIONMAP");
        }
        grassMat.SetFloat(isURP ? "_Smoothness" : "_Glossiness", 0.08f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = grassMat;
        Object.DestroyImmediate(ground.GetComponent<Collider>());

        var dirtTex = AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/InnerverseInteractive/Ultimate Nature – Starter/Environment/Terrain/Textures/UNS_Terrain_Dirt.png");
        var dirtMat = new Material(shader) { color = new Color(0.9f, 0.85f, 0.75f) };
        if (dirtTex != null)
        {
            dirtMat.mainTexture = dirtTex;
            dirtMat.mainTextureScale = new Vector2(4f, 4f);
        }
        dirtMat.SetFloat(isURP ? "_Smoothness" : "_Glossiness", 0.05f);

        var pathways = GameObject.Find("Pathways");
        int recolored = 0;
        if (pathways != null)
        {
            foreach (var r in pathways.GetComponentsInChildren<MeshRenderer>(true))
            {
                Undo.RecordObject(r, "Recolor Path");
                r.sharedMaterial = dirtMat;
                recolored++;
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Radiant Orchard: grass ground added, {recolored} path pieces recolored to dirt-brown. Save the scene (Ctrl+S).");
    }
}
#endif
