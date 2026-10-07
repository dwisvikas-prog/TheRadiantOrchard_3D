using UnityEngine;

/// <summary>
/// Procedurally generates a circular floating-island blockout mesh matching
/// The Radiant Orchard's diorama style (soil disc + irregular rim + water inset).
/// Attach to an empty GameObject and hit "Generate" in the context menu, or
/// call GenerateIsland() from a build/editor script.
/// Use this as your Week 2 placeholder while Meshy/Tripo assets are being sourced.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FloatingIslandGenerator : MonoBehaviour
{
    [Header("Island Shape")]
    [SerializeField] private int edgeCount = 16;       // low-poly = fewer, jagged edges
    [SerializeField] private float radius = 8f;
    [SerializeField] private float edgeJitter = 0.6f;  // irregular silhouette like your screenshot
    [SerializeField] private float depth = 2.5f;       // thickness of the soil block

    [Header("Water Rim Inset")]
    [SerializeField] private bool includeWaterRing = true;
    [SerializeField] private float waterRingRadius = 2.2f;
    [SerializeField] private float waterRingHeight = 0.05f;

    [ContextMenu("Generate Island")]
    public void GenerateIsland()
    {
        Mesh mesh = new Mesh { name = "FloatingIsland_Procedural" };

        int segments = edgeCount;
        Vector3[] topRing = new Vector3[segments];
        Vector3[] bottomRing = new Vector3[segments];

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            float jitter = 1f + Random.Range(-edgeJitter, edgeJitter) * 0.1f;
            float x = Mathf.Cos(angle) * radius * jitter;
            float z = Mathf.Sin(angle) * radius * jitter;

            topRing[i] = new Vector3(x, 0f, z);
            bottomRing[i] = new Vector3(x * 0.85f, -depth, z * 0.85f); // taper for chunky base
        }

        // Build vertices: top cap center + ring, bottom ring for side walls
        var verts = new System.Collections.Generic.List<Vector3>();
        var tris = new System.Collections.Generic.List<int>();

        int topCenterIndex = verts.Count;
        verts.Add(Vector3.zero);
        int topRingStart = verts.Count;
        verts.AddRange(topRing);

        // Top fan
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            tris.Add(topCenterIndex);
            tris.Add(topRingStart + next);
            tris.Add(topRingStart + i);
        }

        // Side walls (top ring -> bottom ring)
        int bottomRingStart = verts.Count;
        verts.AddRange(bottomRing);

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            int t0 = topRingStart + i, t1 = topRingStart + next;
            int b0 = bottomRingStart + i, b1 = bottomRingStart + next;

            tris.Add(t0); tris.Add(t1); tris.Add(b0);
            tris.Add(t1); tris.Add(b1); tris.Add(b0);
        }

        // Bottom cap
        int bottomCenterIndex = verts.Count;
        verts.Add(new Vector3(0f, -depth, 0f));
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            tris.Add(bottomCenterIndex);
            tris.Add(bottomRingStart + i);
            tris.Add(bottomRingStart + next);
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;

        if (includeWaterRing)
            BuildWaterRing();
    }

    private void BuildWaterRing()
    {
        // Simple flat disc slightly above the soil top, centered — matches the
        // teal water inset visible around your central tree cluster.
        GameObject ring = transform.Find("WaterRing")?.gameObject;
        if (ring == null)
        {
            ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "WaterRing";
            ring.transform.SetParent(transform, false);
            Destroy(ring.GetComponent<Collider>());
        }
        ring.transform.localPosition = new Vector3(0f, waterRingHeight, 0f);
        ring.transform.localScale = new Vector3(waterRingRadius, 0.01f, waterRingRadius);
    }
}
