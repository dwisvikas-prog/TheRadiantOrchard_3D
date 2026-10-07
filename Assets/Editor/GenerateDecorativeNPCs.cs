#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Simple grey capsule mannequins (same minimalist style as
// IslandSceneGenerator's own fallback stickman) posed walking along paths,
// standing near trees, and sitting on a rock by the stream — pure decoration
// for the nature-scene composition, separate from Radiant Orchard's one
// interactive NPC in "Characters".
public static class GenerateDecorativeNPCs
{
    private struct Pose
    {
        public Vector3 position;
        public float facingDeg;
        public bool sitting;
    }

    private static readonly Pose[] Poses =
    {
        // walking along the path rings (radius 14 / 34), facing along the ring
        new Pose { position = new Vector3(0f, 0f, 14f), facingDeg = 90f },
        new Pose { position = new Vector3(0f, 0f, -14f), facingDeg = 90f },
        new Pose { position = new Vector3(34f * 0.98f, 0f, 34f * 0.17f), facingDeg = 260f },
        new Pose { position = new Vector3(-6f, 0f, 33.5f), facingDeg = 190f },

        // standing near jungle trees
        new Pose { position = new Vector3(9f, 0f, 23f), facingDeg = 300f },
        new Pose { position = new Vector3(-16f, 0f, -9f), facingDeg = 60f },
        new Pose { position = new Vector3(29f, 0f, -6f), facingDeg = 170f },

        // sitting on a rock near the stream/pond (pond center ~ (2,0,7))
        new Pose { position = new Vector3(4.5f, 0f, 9.5f), facingDeg = 220f, sitting = true },
    };

    [MenuItem("Tools/Radiant Orchard/Generate Decorative NPCs")]
    private static void Generate()
    {
        Undo.SetCurrentGroupName("Generate Decorative NPCs");
        int undoGroup = Undo.GetCurrentGroup();

        var root = FindOrCreateGroup("DecorativeNPCs");
        ClearChildren(root);

        var skinColor = new Color(0.75f, 0.75f, 0.75f);

        for (int i = 0; i < Poses.Length; i++)
        {
            var pose = Poses[i];

            if (pose.sitting)
            {
                BuildSittingMannequin(root, pose, skinColor, i);
            }
            else
            {
                BuildStandingMannequin(root, pose, skinColor, i);
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: 8 decorative grey mannequin NPCs placed. Save the scene (Ctrl+S).");
    }

    private static void BuildStandingMannequin(Transform parent, Pose pose, Color color, int index)
    {
        var mannequin = new GameObject("Mannequin_" + index);
        Undo.RegisterCreatedObjectUndo(mannequin, "Create Mannequin");
        mannequin.transform.SetParent(parent, false);
        mannequin.transform.localPosition = pose.position;
        mannequin.transform.localRotation = Quaternion.Euler(0f, pose.facingDeg, 0f);

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Undo.RegisterCreatedObjectUndo(body, "Create Mannequin Body");
        body.name = "Body";
        body.transform.SetParent(mannequin.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        body.transform.localScale = new Vector3(0.42f, 0.9f, 0.42f);
        ApplyColor(body, color);
        Object.DestroyImmediate(body.GetComponent<Collider>());

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Undo.RegisterCreatedObjectUndo(head, "Create Mannequin Head");
        head.name = "Head";
        head.transform.SetParent(mannequin.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.85f, 0f);
        head.transform.localScale = Vector3.one * 0.38f;
        ApplyColor(head, color);
        Object.DestroyImmediate(head.GetComponent<Collider>());
    }

    private static void BuildSittingMannequin(Transform parent, Pose pose, Color color, int index)
    {
        var mannequin = new GameObject("Mannequin_Sitting_" + index);
        Undo.RegisterCreatedObjectUndo(mannequin, "Create Sitting Mannequin");
        mannequin.transform.SetParent(parent, false);
        mannequin.transform.localPosition = pose.position;
        mannequin.transform.localRotation = Quaternion.Euler(0f, pose.facingDeg, 0f);

        var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Undo.RegisterCreatedObjectUndo(rock, "Create Sitting Rock");
        rock.name = "Rock";
        rock.transform.SetParent(mannequin.transform, false);
        rock.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        rock.transform.localScale = new Vector3(1.3f, 0.7f, 1.1f);
        ApplyColor(rock, new Color(0.42f, 0.4f, 0.38f));
        Object.DestroyImmediate(rock.GetComponent<Collider>());

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Undo.RegisterCreatedObjectUndo(body, "Create Sitting Body");
        body.name = "Body";
        body.transform.SetParent(mannequin.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        body.transform.localRotation = Quaternion.Euler(80f, 0f, 0f);
        body.transform.localScale = new Vector3(0.4f, 0.6f, 0.4f);
        ApplyColor(body, color);
        Object.DestroyImmediate(body.GetComponent<Collider>());

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Undo.RegisterCreatedObjectUndo(head, "Create Sitting Head");
        head.name = "Head";
        head.transform.SetParent(mannequin.transform, false);
        head.transform.localPosition = new Vector3(0f, 1.15f, 0.1f);
        head.transform.localScale = Vector3.one * 0.36f;
        ApplyColor(head, color);
        Object.DestroyImmediate(head.GetComponent<Collider>());
    }

    private static void ApplyColor(GameObject go, Color color)
    {
        bool isURP = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        var shader = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");
        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sharedMaterial = new Material(shader) { color = color };
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
