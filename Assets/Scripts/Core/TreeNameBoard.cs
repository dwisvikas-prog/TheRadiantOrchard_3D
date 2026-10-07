using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>
    /// Wooden name board above fruit trees so players can tell them apart.
    /// </summary>
    public static class TreeNameBoard
    {
        public static void Ensure(Transform tree, string label, Color accent, FruitType fruit)
        {
            if (tree == null) return;
            EnsureTapTarget(tree, fruit);

            var existing = tree.Find("NameBoard");
            if (existing != null)
            {
                var tm = existing.GetComponentInChildren<TextMesh>();
                if (tm != null) tm.text = label;
                return;
            }

            // Remove old plain TreeName if present
            var old = tree.Find("TreeName");
            if (old != null) Object.Destroy(old.gameObject);

            var root = new GameObject("NameBoard");
            root.transform.SetParent(tree, false);
            float y = tree.name.IndexOf("Strawberry", System.StringComparison.OrdinalIgnoreCase) >= 0
                ? 1.55f : 2.45f;
            root.transform.localPosition = new Vector3(0f, y, 0f);

            var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plank.name = "Plank";
            plank.transform.SetParent(root.transform, false);
            plank.transform.localPosition = Vector3.zero;
            plank.transform.localScale = new Vector3(1.55f, 0.28f, 0.06f);
            Object.Destroy(plank.GetComponent<Collider>());
            var mr = plank.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            var wood = new Color(0.42f, 0.28f, 0.14f, 1f);
            mat.color = wood;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", wood);
            mr.sharedMaterial = mat;

            // Accent strip
            var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = "Accent";
            strip.transform.SetParent(root.transform, false);
            strip.transform.localPosition = new Vector3(0f, 0.14f, -0.01f);
            strip.transform.localScale = new Vector3(1.55f, 0.05f, 0.05f);
            Object.Destroy(strip.GetComponent<Collider>());
            var smr = strip.GetComponent<MeshRenderer>();
            var sm = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            sm.color = accent;
            if (sm.HasProperty("_BaseColor")) sm.SetColor("_BaseColor", accent);
            smr.sharedMaterial = sm;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0.02f, -0.05f);
            var tmNew = labelGo.AddComponent<TextMesh>();
            tmNew.text = label;
            tmNew.fontSize = 42;
            tmNew.characterSize = 0.045f;
            tmNew.anchor = TextAnchor.MiddleCenter;
            tmNew.alignment = TextAlignment.Center;
            tmNew.color = Color.white;
            tmNew.fontStyle = FontStyle.Bold;
            labelGo.AddComponent<TreeNameBillboard>();
            root.AddComponent<TreeNameBillboard>();
        }

        public static void RefreshAllTracked()
        {
            var tracker = Object.FindFirstObjectByType<OrchardTreeTracker>();
            if (tracker == null) return;
            foreach (var p in tracker.All)
            {
                if (p.root == null) continue;
                var pick = FruitTreeCatalog.Get(p.fruit);
                var vis = OrchardTreeTracker.ResolveTreeVisual(p.root) ?? p.root;
                Ensure(vis, pick.label, pick.fruitColor, p.fruit);
            }
        }

        static void EnsureTapTarget(Transform tree, FruitType fruit)
        {
            var target = tree.GetComponent<TreeInfoTarget>();
            if (target == null) target = tree.gameObject.AddComponent<TreeInfoTarget>();
            target.fruit = fruit;

            if (tree.GetComponent<Collider>() != null) return; // trunk/mesh already has one
            // Low, narrow — the lower trunk only. Fruit sits higher up
            // (y ~1.3-1.8, offset sideways) so this deliberately stays clear
            // of it; TreeInfoTapController also excludes taps near any fruit
            // as a second guard.
            var col = tree.gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.7f, 0f);
            col.height = 1.3f;
            col.radius = 0.4f;
        }
    }
}
