using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>
    /// Maps each fruit to a REAL tree prefab that already exists in Resources/CoC_HQ.
    /// Never invents missing trees — only available assets (Apple / Pear / Plum / Green / Bush / Pine).
    /// </summary>
    public static class FruitTreeCatalog
    {
        public readonly struct TreePick
        {
            public readonly string resourcesPath;
            public readonly string label;       // shown on tree
            public readonly Color leafTint;     // tint so fruit type reads clearly
            public readonly Color fruitColor;
            public readonly PlaceholderShape fruitShape;

            public TreePick(string path, string label, Color leafTint, Color fruitColor,
                PlaceholderShape shape = PlaceholderShape.Sphere)
            {
                resourcesPath = path;
                this.label = label;
                this.leafTint = leafTint;
                this.fruitColor = fruitColor;
                fruitShape = shape;
            }
        }

        public static TreePick Get(FruitType fruit) => fruit switch
        {
            // Real Resources trees only — closest match per fruit name/feel
            FruitType.Apple => new TreePick(BestAssets.ForestApple, "APPLE TREE",
                new Color(0.35f, 0.75f, 0.25f), new Color(0.85f, 0.15f, 0.12f)),
            FruitType.Peach => new TreePick(BestAssets.ForestPear, "PEACH TREE",
                new Color(0.95f, 0.7f, 0.45f), new Color(1f, 0.65f, 0.45f)),
            FruitType.Cherry => new TreePick(BestAssets.ForestPlum, "CHERRY TREE",
                new Color(0.9f, 0.2f, 0.3f), new Color(0.85f, 0.1f, 0.2f)),
            FruitType.Grapes => new TreePick(BestAssets.ForestPlum, "GRAPE VINE TREE",
                new Color(0.55f, 0.25f, 0.7f), new Color(0.5f, 0.2f, 0.7f)),
            FruitType.Lemon => new TreePick(BestAssets.ForestGreen, "LEMON TREE",
                new Color(0.95f, 0.9f, 0.25f), new Color(0.95f, 0.88f, 0.15f)),
            FruitType.Banana => new TreePick(BestAssets.ForestGreen, "BANANA TREE",
                new Color(0.9f, 0.85f, 0.2f), new Color(0.95f, 0.85f, 0.15f), PlaceholderShape.Cube),
            FruitType.Pineapple => new TreePick(BestAssets.ForestPine, "PINEAPPLE PLANT",
                new Color(0.55f, 0.75f, 0.25f), new Color(1f, 0.78f, 0.15f), PlaceholderShape.Cube),
            FruitType.Watermelon => new TreePick(BestAssets.ForestGreen, "WATERMELON PATCH",
                new Color(0.2f, 0.7f, 0.35f), new Color(0.2f, 0.75f, 0.4f)),
            FruitType.Strawberry => new TreePick(BestAssets.Bush, "STRAWBERRY BUSH",
                new Color(0.95f, 0.3f, 0.35f), new Color(0.95f, 0.2f, 0.3f)),
            _ => new TreePick(BestAssets.ForestGreen, "FRUIT TREE",
                Color.green, Color.white)
        };

        public static GameObject LoadPrefab(FruitType fruit)
        {
            var pick = Get(fruit);
            var prefab = Resources.Load<GameObject>(pick.resourcesPath);
            // Fallbacks only among AVAILABLE Resources trees
            if (prefab == null) prefab = Resources.Load<GameObject>(BestAssets.ForestApple);
            if (prefab == null) prefab = Resources.Load<GameObject>(BestAssets.ForestGreen);
            if (prefab == null) prefab = Resources.Load<GameObject>(BestAssets.Bush);
            return prefab;
        }

        public static GameObject SpawnTree(FruitType fruit, Transform parent, Vector3 worldPos)
        {
            var pick = Get(fruit);
            var prefab = LoadPrefab(fruit);
            GameObject tree;
            if (prefab != null)
            {
                tree = Object.Instantiate(prefab, parent);
                tree.name = "FruitTree_" + fruit;
                tree.transform.position = worldPos;
                FitHeight(tree, fruit == FruitType.Strawberry ? 1.1f : 2.0f);
                TintRenderers(tree, pick.leafTint);
            }
            else
            {
                // Last resort procedural trunk (still tagged by fruit name)
                tree = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tree.name = "FruitTree_" + fruit;
                tree.transform.SetParent(parent, true);
                tree.transform.position = worldPos + Vector3.up * 0.9f;
                tree.transform.localScale = new Vector3(0.35f, 0.9f, 0.35f);
                Object.Destroy(tree.GetComponent<Collider>());
            }

            // TreeNameBoard's wooden plank already carries the name — the old
            // extra floating TextMesh here just duplicated it (and read huge).
            TreeNameBoard.Ensure(tree.transform, pick.label, pick.fruitColor, fruit);
            return tree;
        }

        public static FruitData MakeFruitData(FruitLessonBook.Lesson lesson)
        {
            var pick = Get(lesson.fruit);
            var health = FruitHealthMatrix.Get(lesson.fruit);

            // Prefer authored Resources asset when present
            var loaded = Resources.Load<FruitData>("FruitData_" + lesson.fruitName)
                         ?? Resources.Load<FruitData>(BestAssets.FruitStraw);
            FruitData data;
            if (loaded != null)
            {
                data = ScriptableObject.Instantiate(loaded);
            }
            else
            {
                data = ScriptableObject.CreateInstance<FruitData>();
                data.suitableConditions = new[] { NPCCondition.LowSpirit };
            }

            data.fruitType = lesson.fruit;
            data.displayName = lesson.fruitName;
            data.gestureType = lesson.gesture;
            data.placeholderColor = pick.fruitColor;
            data.placeholderShape = pick.fruitShape;
            data.symptomLabel = lesson.mood;
            data.healthHint = lesson.heals;
            data.healVfxColor = health.themeColor;
            data.spiritRestoreAmount = 0.35f;
            data.healingAmount = 0.15f;
            data.emotionalResponse = EmotionState.Grateful;
            return data;
        }

        static void FitHeight(GameObject tree, float targetH)
        {
            var rends = tree.GetComponentsInChildren<Renderer>();
            if (rends == null || rends.Length == 0) return;
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            float h = Mathf.Max(0.01f, b.size.y);
            tree.transform.localScale *= (targetH / h);
            // Re-seat on ground after scale
            var pos = tree.transform.position;
            pos.y = 0f;
            tree.transform.position = pos;
        }

        static void TintRenderers(GameObject tree, Color tint)
        {
            var rends = tree.GetComponentsInChildren<Renderer>();
            if (rends == null) return;
            for (int i = 0; i < rends.Length; i++)
            {
                var mats = rends[i].materials;
                for (int m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    if (mat == null) continue;
                    // Soft tint — keep bark-ish browns, push leaves toward fruit color
                    Color baseC = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
                    float greenness = baseC.g - (baseC.r + baseC.b) * 0.35f;
                    if (greenness > 0.05f || baseC.g > 0.35f)
                    {
                        Color mixed = Color.Lerp(baseC, tint, 0.55f);
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", mixed);
                        mat.color = mixed;
                    }
                }
                rends[i].materials = mats;
            }
        }

    }

    public class TreeNameBillboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}
