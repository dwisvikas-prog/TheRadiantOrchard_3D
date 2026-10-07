#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using RadiantOrchard;
using System.Collections.Generic;

// Swaps the procedural glossy-primitive fruit stand-ins (built by
// GenerateOrchardZones.BuildFruitShape) for real meshes from the
// "PolyOne/Free Fruits" pack the user imported, wherever a matching fruit
// exists in that pack. 6 of the 9 types have an exact match. Lemon and Peach
// have no dedicated mesh in the pack either, but Orange is close enough in
// silhouette (round citrus/stone fruit) that it's reused for both, tinted
// per-instance via MaterialPropertyBlock toward each fruit's own configured
// color (see TintRealFruit) rather than left as a literal orange for all
// three. Strawberry still has no reasonable geometric match in owned assets
// (no berry-shaped mesh anywhere) — its procedural shape stays.
public static class SwapInRealFruitModels
{
    static readonly Dictionary<FruitType, string[]> RealFruitPrefabs = new Dictionary<FruitType, string[]>
    {
        { FruitType.Apple, new[] { "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Apple_R1.prefab" } },
        { FruitType.Pineapple, new[] { "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Pineapple_1.prefab" } },
        { FruitType.Watermelon, new[] { "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Watermelon_1.prefab", "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Watermelon_2.prefab" } },
        { FruitType.Grapes, new[] { "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Grape_P2.prefab" } },
        { FruitType.Banana, new[] { "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Banana_Y1.prefab" } },
        { FruitType.Cherry, new[] { "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Cherries_2.prefab" } },
        { FruitType.Lemon, new[] { "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Orange_1.prefab", "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Orange_3.prefab" } },
        { FruitType.Peach, new[] { "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Orange_1.prefab", "Assets/PolyOne/Free Fruits/Prefabs/SM_Fruits_Orange_3.prefab" } },
    };

    // Only these two need a tint — every other mapped type already has its
    // own correctly-colored mesh/texture.
    static readonly HashSet<FruitType> NeedsTint = new HashSet<FruitType> { FruitType.Lemon, FruitType.Peach };

    static readonly HashSet<FruitType> BushHeightTypes = new HashSet<FruitType> { FruitType.Strawberry, FruitType.Pineapple, FruitType.Watermelon, FruitType.Grapes };

    [MenuItem("Tools/Radiant Orchard/Swap In Real Fruit Models (PolyOne)")]
    private static void Swap()
    {
        if (Application.isPlaying) { Debug.LogError("Stop Play mode first."); return; }

        var fruitsGroup = GameObject.Find("Fruits");
        if (fruitsGroup == null) { Debug.LogError("Fruits group not found — run the orchard zone generator first."); return; }

        Undo.SetCurrentGroupName("Swap In Real Fruit Models");
        int undoGroup = Undo.GetCurrentGroup();
        Random.InitState(4242);

        int swappedTypes = 0;
        var missing = new List<string>();

        foreach (FruitType type in System.Enum.GetValues(typeof(FruitType)))
        {
            var zone = fruitsGroup.transform.Find("Orchard_" + type);
            if (zone == null) continue;

            if (!RealFruitPrefabs.TryGetValue(type, out var prefabPaths))
            {
                missing.Add(type.ToString());
                continue;
            }

            var prefabs = new List<GameObject>();
            foreach (var p in prefabPaths)
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (pf != null) prefabs.Add(pf);
            }
            if (prefabs.Count == 0) { missing.Add(type + " (prefab load failed)"); continue; }

            // Remove only the old procedural fruit-shape children (names always
            // start with the FruitType string) — leaves fence, sign, and host
            // plant, which all have unrelated names, untouched.
            string prefix = type.ToString();
            for (int i = zone.childCount - 1; i >= 0; i--)
            {
                var child = zone.GetChild(i);
                if (child.name.StartsWith(prefix))
                    Undo.DestroyObjectImmediate(child.gameObject);
            }

            bool isBush = BushHeightTypes.Contains(type);
            float height = isBush ? 0.75f : 2.1f;
            float radius = isBush ? 0.9f : 1.3f;
            int count = 5;

            Color? tint = null;
            if (NeedsTint.Contains(type))
            {
                var def = AssetDatabase.LoadAssetAtPath<FruitZoneDefinition>($"Assets/GameData/Fruits/Fruit_{type}.asset");
                if (def != null) tint = def.fruitColor;
            }

            for (int i = 0; i < count; i++)
            {
                float angle = i * (360f / count) * Mathf.Deg2Rad;
                var localPos = new Vector3(Mathf.Cos(angle) * radius, height + Mathf.Sin(i * 1.7f) * 0.3f, Mathf.Sin(angle) * radius);

                var prefab = prefabs[i % prefabs.Count];
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, zone);
                Undo.RegisterCreatedObjectUndo(inst, "Create Real Fruit");
                inst.name = type + "_Real_" + i;
                inst.transform.localPosition = localPos;
                inst.transform.localRotation = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-15f, 15f));
                inst.transform.localScale = Vector3.one * Random.Range(2.3f, 2.7f);

                // Orange's mesh/texture is orange either way — a per-instance
                // property block tint (not touching the shared material asset)
                // is what actually makes the Lemon and Peach zones read as
                // different fruit instead of both looking like stray oranges.
                if (tint.HasValue)
                {
                    var rend = inst.GetComponentInChildren<Renderer>();
                    if (rend != null)
                    {
                        var mpb = new MaterialPropertyBlock();
                        mpb.SetColor("_Color", tint.Value);
                        mpb.SetColor("_BaseColor", tint.Value);
                        rend.SetPropertyBlock(mpb);
                    }
                }
            }

            swappedTypes++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        string msg = $"Radiant Orchard: swapped {swappedTypes} fruit types to real PolyOne models.";
        if (missing.Count > 0) msg += " No real model available for: " + string.Join(", ", missing) + " (kept procedural shapes there).";
        Debug.Log(msg);
    }
}
#endif
