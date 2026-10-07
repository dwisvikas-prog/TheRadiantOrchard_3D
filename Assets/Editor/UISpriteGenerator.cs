#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// No icon/gradient art exists in the project (audit found none), so this
// bakes small placeholder sprites (a white circle to tint per fruit, and the
// pink->orange->green vibrancy gradient bar) as real saved PNG assets so UI
// references survive editor restarts, not just in-memory textures.
public static class UISpriteGenerator
{
    public static Sprite CreateOrLoadCircleSprite(string path, int size = 64)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                tex.SetPixel(x, y, dist <= radius ? Color.white : new Color(0f, 0f, 0f, 0f));
            }
        }

        tex.Apply();
        return SaveAsSpriteAsset(tex, path);
    }

    public static Sprite CreateOrLoadStarSprite(string path, int size = 64)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var center = new Vector2(size / 2f, size / 2f);
        float outerRadius = size / 2f - 1f;
        float innerRadius = outerRadius * 0.45f;

        var verts = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float angle = -Mathf.PI / 2f + i * Mathf.PI / 5f;
            float r = (i % 2 == 0) ? outerRadius : innerRadius;
            verts[i] = center + new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                tex.SetPixel(x, y, PointInPolygon(p, verts) ? Color.white : new Color(0f, 0f, 0f, 0f));
            }
        }

        tex.Apply();
        return SaveAsSpriteAsset(tex, path);
    }

    public static Sprite CreateOrLoadLeafSprite(string path, int size = 64)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size * 0.62f;
        var c1 = new Vector2(size * 0.5f - size * 0.28f, size * 0.5f);
        var c2 = new Vector2(size * 0.5f + size * 0.28f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                bool inside = Vector2.Distance(p, c1) <= r && Vector2.Distance(p, c2) <= r;
                tex.SetPixel(x, y, inside ? Color.white : new Color(0f, 0f, 0f, 0f));
            }
        }

        tex.Apply();
        return SaveAsSpriteAsset(tex, path);
    }

    private static bool PointInPolygon(Vector2 p, Vector2[] poly)
    {
        bool inside = false;
        int j = poly.Length - 1;
        for (int i = 0; i < poly.Length; i++)
        {
            if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
            {
                inside = !inside;
            }
            j = i;
        }
        return inside;
    }

    public static Sprite CreateOrLoadGradientSprite(string path, Color left, Color mid, Color right, int width = 128, int height = 16)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int x = 0; x < width; x++)
        {
            float t = x / (float)(width - 1);
            Color c = t < 0.5f ? Color.Lerp(left, mid, t / 0.5f) : Color.Lerp(mid, right, (t - 0.5f) / 0.5f);
            for (int y = 0; y < height; y++)
                tex.SetPixel(x, y, c);
        }

        tex.Apply();
        return SaveAsSpriteAsset(tex, path);
    }

    private static Sprite SaveAsSpriteAsset(Texture2D tex, string path)
    {
        var dir = path.Substring(0, path.LastIndexOf('/'));
        EnsureFolder(dir);

        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = path.Substring(0, path.LastIndexOf('/'));
        var folderName = path.Substring(path.LastIndexOf('/') + 1);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif
