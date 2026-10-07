using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>
    /// Placeable island props available in the CoC-style shop (from Resources).
    /// </summary>
    public static class IslandShopCatalog
    {
        public enum Category
        {
            Nature,
            Rocks,
            Trees
        }

        public struct Entry
        {
            public string id;
            public string label;
            public Category category;
            public string resourcePath; // Resources path, or null for procedural
            public float height;
            public Color swatch;
        }

        public static readonly Entry[] All =
        {
            new Entry
            {
                id = "grass", label = "Grass", category = Category.Nature,
                resourcePath = null, height = 0.35f,
                swatch = new Color(0.35f, 0.72f, 0.28f)
            },
            new Entry
            {
                id = "bush", label = "Bush", category = Category.Nature,
                resourcePath = BestAssets.Bush, height = 0.85f,
                swatch = new Color(0.28f, 0.62f, 0.30f)
            },
            new Entry
            {
                id = "rock1", label = "Rock", category = Category.Rocks,
                resourcePath = BestAssets.Rock1, height = 0.55f,
                swatch = new Color(0.55f, 0.52f, 0.48f)
            },
            new Entry
            {
                id = "rock2", label = "Boulder", category = Category.Rocks,
                resourcePath = BestAssets.Rock2, height = 0.7f,
                swatch = new Color(0.48f, 0.46f, 0.42f)
            },
            new Entry
            {
                id = "pine", label = "Pine", category = Category.Trees,
                resourcePath = BestAssets.ForestPine, height = 2.2f,
                swatch = new Color(0.18f, 0.45f, 0.28f)
            },
            new Entry
            {
                id = "green", label = "Green Tree", category = Category.Trees,
                resourcePath = BestAssets.ForestGreen, height = 2.0f,
                swatch = new Color(0.25f, 0.58f, 0.32f)
            },
            new Entry
            {
                id = "apple", label = "Apple Tree", category = Category.Trees,
                resourcePath = BestAssets.ForestApple, height = 2.0f,
                swatch = new Color(0.72f, 0.28f, 0.22f)
            },
            new Entry
            {
                id = "pear", label = "Pear Tree", category = Category.Trees,
                resourcePath = BestAssets.ForestPear, height = 2.0f,
                swatch = new Color(0.75f, 0.72f, 0.25f)
            },
            new Entry
            {
                id = "plum", label = "Plum Tree", category = Category.Trees,
                resourcePath = BestAssets.ForestPlum, height = 2.0f,
                swatch = new Color(0.55f, 0.28f, 0.58f)
            },
        };

        public static bool IsAvailable(Entry e)
        {
            if (string.IsNullOrEmpty(e.resourcePath)) return true; // procedural grass
            return Resources.Load<GameObject>(e.resourcePath) != null;
        }
    }
}
