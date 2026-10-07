using UnityEngine;

namespace RadiantOrchard
{
    // Clean CoC village on the square pad — equal spacing, old island junk removed.
    // Called from MainSceneSquareGround.Apply() (Play + editor). No extra menu needed.
    public static class CoCVillageLayout
    {
        public const string VillageRoot = "CoC_Village";

        // Exactly 9 plots (one per fruit) on a 3×3 grid, pushed to the OUTER rim of the square.
        const int GridRadius = 1; // -1..+1 → 9 cells
        const float EdgePad = 1.5f; // almost flush with grass edge → max distance from center/trees
        const float PlotHalf = 4.2f;
        const float FenceExtra = 0.3f;
        const float HubHalf = 3.5f;
        const float StallHalfX = 2.8f;
        const float StallHalfZ = 2f;

        struct FruitSlot
        {
            public FruitType type;
            public Color color;
            public int gx, gz;
        }

        // One unique fruit per cell — no duplicates.
        static readonly FruitSlot[] NinePlots =
        {
            new FruitSlot { type = FruitType.Watermelon, gx = -1, gz =  1, color = new Color(0.18f, 0.48f, 0.14f) },
            new FruitSlot { type = FruitType.Pineapple,  gx =  0, gz =  1, color = new Color(0.95f, 0.75f, 0.15f) },
            new FruitSlot { type = FruitType.Strawberry, gx =  1, gz =  1, color = new Color(0.90f, 0.18f, 0.22f) },
            new FruitSlot { type = FruitType.Lemon,      gx = -1, gz =  0, color = new Color(0.95f, 0.88f, 0.20f) },
            new FruitSlot { type = FruitType.Cherry,     gx =  0, gz =  0, color = new Color(0.75f, 0.08f, 0.18f) },
            new FruitSlot { type = FruitType.Apple,      gx =  1, gz =  0, color = new Color(0.85f, 0.15f, 0.15f) },
            new FruitSlot { type = FruitType.Grapes,     gx = -1, gz = -1, color = new Color(0.45f, 0.20f, 0.65f) },
            new FruitSlot { type = FruitType.Peach,      gx =  0, gz = -1, color = new Color(1.00f, 0.60f, 0.40f) },
            new FruitSlot { type = FruitType.Banana,     gx =  1, gz = -1, color = new Color(0.95f, 0.85f, 0.20f) },
        };

        public static void Apply(Vector3 origin)
        {
            StripClutter();
            RebuildVillage(origin);
            Debug.Log("[CoCVillageLayout] 9 plots (one per fruit) applied.");
        }

        // Hide / kill anything that isn't the new square base + village.
        static void StripClutter()
        {
            string[] killExact =
            {
                "Fruits", "OrchardZones", "CentralMagicTree", "MagicTree",
                "PerimeterTrees", "Pathways", "FloatingRocks", "CloudWisps",
                "Environment_Step1", "Island", "FloatingIsland", "IslandMesh",
                "FloatingislandmainBASE", "IslandBase", "Well", "Dock", "Pond",
                "Shrine", "CrystalShrine", "LilyPads"
            };

            var toKill = new System.Collections.Generic.HashSet<GameObject>();

            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t == null) continue;
                string rootName = t.root != null ? t.root.name : t.name;
                if (rootName == VillageRoot || rootName == "CoC_BaseGround" || rootName == "SQUARE_GROUND") continue;
                if (rootName == "Main Camera" || rootName == "Directional Light" || rootName == "GameManagers") continue;
                if (rootName.Contains("Stickman") || rootName.Contains("Canvas") || rootName.Contains("HUD")) continue;
                if (rootName.Contains("Camera") || rootName.Contains("EventSystem")) continue;
                if (rootName.StartsWith("CoC_")) continue;

                string n = t.name;
                bool strip = false;
                for (int i = 0; i < killExact.Length; i++)
                    if (n == killExact[i]) { strip = true; break; }

                if (!strip)
                {
                    if (n.IndexOf("Tree", System.StringComparison.OrdinalIgnoreCase) >= 0) strip = true;
                    else if (n.IndexOf("Bush", System.StringComparison.OrdinalIgnoreCase) >= 0) strip = true;
                    else if (n.IndexOf("Shrub", System.StringComparison.OrdinalIgnoreCase) >= 0) strip = true;
                    else if (n.IndexOf("Palm", System.StringComparison.OrdinalIgnoreCase) >= 0) strip = true;
                    else if (n.IndexOf("Pine", System.StringComparison.OrdinalIgnoreCase) >= 0) strip = true;
                    else if (n.IndexOf("Willow", System.StringComparison.OrdinalIgnoreCase) >= 0) strip = true;
                    else if (n.IndexOf("Oak", System.StringComparison.OrdinalIgnoreCase) >= 0) strip = true;
                    else if (n.StartsWith("PT_") &&
                             (n.IndexOf("Fruit", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                              n.IndexOf("Generic", System.StringComparison.OrdinalIgnoreCase) >= 0))
                        strip = true;
                    else if (n.StartsWith("Orchard_") || n.StartsWith("Plot_")) strip = true;
                }

                if (!strip) continue;

                // Kill highest meaningful object
                Transform killT = t;
                if (IsListedRoot(t.name, killExact) ||
                    t.name.StartsWith("Orchard_") || t.name.StartsWith("Plot_") ||
                    t.name.IndexOf("Tree", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    t.name.IndexOf("Bush", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    t.name.IndexOf("Shrub", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // climb to named root if child of a listed group
                    while (killT.parent != null &&
                           !IsListedRoot(killT.name, killExact) &&
                           !killT.name.StartsWith("Orchard_") &&
                           !killT.name.StartsWith("Plot_") &&
                           killT.name.IndexOf("Tree", System.StringComparison.OrdinalIgnoreCase) < 0)
                        killT = killT.parent;
                    toKill.Add(killT.gameObject);
                }
                else
                {
                    foreach (var r in t.GetComponentsInChildren<Renderer>(true))
                        if (r != null) r.enabled = false;
                }
            }

            foreach (var go in toKill)
                Kill(go);

            var old = GameObject.Find(VillageRoot);
            if (old != null) Kill(old);
        }

        static bool IsListedRoot(string name, string[] list)
        {
            for (int i = 0; i < list.Length; i++)
                if (name == list[i]) return true;
            return false;
        }

        static void Kill(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Object.Destroy(go);
            else Object.DestroyImmediate(go);
        }

        static void RebuildVillage(Vector3 origin)
        {
            float plotOuter = PlotHalf + FenceExtra;
            // Push ring plots to the OUTER edge of the big square (far from center trees).
            float pitch = (MainSceneSquareGround.GrassHalf - EdgePad - plotOuter) / GridRadius;
            float hub = Mathf.Min(HubHalf, PlotHalf * 0.75f);

            // Center Cherry stays near middle; the other 8 use full outward pitch.
            // Extra outward boost for non-center plots: sit even closer to the rim.
            float outerPitch = pitch;

            var root = new GameObject(VillageRoot);
            root.transform.position = origin;

            var plots = new GameObject("FruitPlots").transform;
            plots.SetParent(root.transform, false);

            for (int i = 0; i < NinePlots.Length; i++)
            {
                var slot = NinePlots[i];
                // Non-center plots use full outer pitch (bahar ki taraf).
                float px = slot.gx * outerPitch;
                float pz = slot.gz * outerPitch;
                // Center plot stays at origin (shrine + cherry), not mixed with outer ring.
                if (slot.gx == 0 && slot.gz == 0)
                {
                    px = 0f;
                    pz = 0f;
                }
                var pos = new Vector3(px, 0f, pz);
                var plot = BuildPlot(plots, slot.type, pos, slot.color, i, makeAlias: true);

                if (slot.gx == 0 && slot.gz == 0)
                    BuildHub(plot, hub);
            }

            // Well between center and north — still clear of outer plots
            float wellZ = outerPitch * 0.38f;
            BuildWell(root.transform, new Vector3(0f, 0f, wellZ));

            var stall = new Vector3(
                MainSceneSquareGround.GrassHalf - EdgePad - StallHalfX,
                0f,
                -(MainSceneSquareGround.GrassHalf - EdgePad - StallHalfZ));
            BuildStall(root.transform, stall);

            // Amenities midway center → outer plots (open grass, not on trees)
            float br = outerPitch * 0.45f;
            var am = new GameObject("Amenities").transform;
            am.SetParent(root.transform, false);
            foreach (var s in new[] {
                new Vector3(-br, 0f, br), new Vector3(br, 0f, br),
                new Vector3(-br, 0f, -br), new Vector3(br, 0f, -br)
            })
            {
                BuildBench(am, s, Mathf.Atan2(s.x, s.z) * Mathf.Rad2Deg + 90f);
                BuildLantern(am, s + s.normalized * 2f);
            }

            BuildDefenses(root.transform);
            EnsureVillageSystems(root.transform);
            CoCGuideUI.Ensure();

            Debug.Log($"[CoCVillageLayout] 9 plots + defenses, pitch={outerPitch:F1}");
        }

        // ── CoC defense props around the square rim ─────────────────────

        static void BuildDefenses(Transform root)
        {
            float half = MainSceneSquareGround.GrassHalf - 0.8f;
            var def = Child(root, "Defenses", Vector3.zero);
            var wood = new Color(0.40f, 0.26f, 0.14f);
            var dark = new Color(0.28f, 0.18f, 0.10f);
            var rock = new Color(0.45f, 0.42f, 0.38f);
            var cloth = new Color(0.85f, 0.15f, 0.12f);

            // Outer wooden wall ring (south side has a gate gap)
            const int segs = 10;
            for (int side = 0; side < 4; side++)
            {
                for (int i = 0; i < segs; i++)
                {
                    // Gate opening on south (side 0), middle 2 segments skipped
                    if (side == 0 && (i == segs / 2 - 1 || i == segs / 2)) continue;

                    float t0 = i / (float)segs;
                    float t1 = (i + 1f) / segs;
                    Vector3 a = PerimeterPoint(half, side, t0);
                    Vector3 b = PerimeterPoint(half, side, t1);
                    Vector3 mid = (a + b) * 0.5f;
                    float len = Vector3.Distance(a, b);
                    float yaw = side * 90f;

                    Prim(def, PrimitiveType.Cube, mid + new Vector3(0f, 1.0f, 0f),
                        new Vector3(len * 1.02f, 2.0f, 0.35f), wood)
                        .localRotation = Quaternion.Euler(0f, yaw, 0f);
                    // Top crenellation
                    Prim(def, PrimitiveType.Cube, mid + new Vector3(0f, 2.15f, 0f),
                        new Vector3(len * 0.35f, 0.35f, 0.4f), dark)
                        .localRotation = Quaternion.Euler(0f, yaw, 0f);
                }
            }

            // Gate posts + crossbar (south)
            float gateX = half * 0.12f;
            Prim(def, PrimitiveType.Cube, new Vector3(-gateX, 1.4f, -half), new Vector3(0.45f, 2.8f, 0.45f), dark);
            Prim(def, PrimitiveType.Cube, new Vector3(gateX, 1.4f, -half), new Vector3(0.45f, 2.8f, 0.45f), dark);
            Prim(def, PrimitiveType.Cube, new Vector3(0f, 2.9f, -half), new Vector3(gateX * 2.4f, 0.35f, 0.4f), wood);
            // Gate doors (slightly open)
            Prim(def, PrimitiveType.Cube, new Vector3(-gateX * 0.45f, 1.1f, -half + 0.15f),
                new Vector3(gateX * 0.85f, 2.2f, 0.12f), wood)
                .localRotation = Quaternion.Euler(0f, -25f, 0f);
            Prim(def, PrimitiveType.Cube, new Vector3(gateX * 0.45f, 1.1f, -half + 0.15f),
                new Vector3(gateX * 0.85f, 2.2f, 0.12f), wood)
                .localRotation = Quaternion.Euler(0f, 25f, 0f);

            // Watchtowers on 4 corners
            foreach (var c in new[] {
                new Vector3(-half, 0f, -half), new Vector3(half, 0f, -half),
                new Vector3(-half, 0f, half), new Vector3(half, 0f, half)
            })
                BuildWatchtower(def, c, wood, dark);

            // Barricade rocks near corners (inset)
            float rockIn = half - 4f;
            foreach (var c in new[] {
                new Vector3(-rockIn, 0f, -rockIn), new Vector3(rockIn, 0f, -rockIn),
                new Vector3(-rockIn, 0f, rockIn), new Vector3(rockIn, 0f, rockIn)
            })
            {
                Prim(def, PrimitiveType.Cube, c + new Vector3(0.6f, 0.45f, 0.2f), new Vector3(1.4f, 0.9f, 1.1f), rock);
                Prim(def, PrimitiveType.Cube, c + new Vector3(-0.5f, 0.3f, -0.4f), new Vector3(1.0f, 0.6f, 0.9f), rock);
            }

            // Flags / banners along north wall
            for (int i = 0; i < 5; i++)
            {
                float x = Mathf.Lerp(-half * 0.7f, half * 0.7f, i / 4f);
                BuildFlag(def, new Vector3(x, 0f, half - 0.2f), cloth, wood);
            }
            // Two flags at gate
            BuildFlag(def, new Vector3(-gateX - 0.8f, 0f, -half), cloth, wood);
            BuildFlag(def, new Vector3(gateX + 0.8f, 0f, -half), cloth, wood);
        }

        static Vector3 PerimeterPoint(float half, int side, float t)
        {
            if (side == 0) return new Vector3(Mathf.Lerp(-half, half, t), 0f, -half);
            if (side == 1) return new Vector3(half, 0f, Mathf.Lerp(-half, half, t));
            if (side == 2) return new Vector3(Mathf.Lerp(half, -half, t), 0f, half);
            return new Vector3(-half, 0f, Mathf.Lerp(half, -half, t));
        }

        static void BuildWatchtower(Transform parent, Vector3 corner, Color wood, Color dark)
        {
            var t = Child(parent, "Watchtower", corner);
            Prim(t, PrimitiveType.Cube, new Vector3(0f, 2.2f, 0f), new Vector3(2.2f, 4.4f, 2.2f), wood);
            Prim(t, PrimitiveType.Cube, new Vector3(0f, 4.6f, 0f), new Vector3(2.8f, 0.35f, 2.8f), dark);
            // Lookout posts
            foreach (var o in new[] {
                new Vector3(-1.1f, 5.3f, -1.1f), new Vector3(1.1f, 5.3f, -1.1f),
                new Vector3(-1.1f, 5.3f, 1.1f), new Vector3(1.1f, 5.3f, 1.1f)
            })
                Prim(t, PrimitiveType.Cube, o, new Vector3(0.25f, 1.2f, 0.25f), wood);
            Prim(t, PrimitiveType.Cube, new Vector3(0f, 6.0f, 0f), new Vector3(2.6f, 0.25f, 2.6f), dark);
        }

        static void BuildFlag(Transform parent, Vector3 pos, Color cloth, Color pole)
        {
            var f = Child(parent, "Flag", pos);
            Prim(f, PrimitiveType.Cylinder, new Vector3(0f, 2.2f, 0f), new Vector3(0.08f, 2.2f, 0.08f), pole);
            Prim(f, PrimitiveType.Cube, new Vector3(0.55f, 3.6f, 0f), new Vector3(1.1f, 0.7f, 0.06f), cloth);
        }

        static void EnsureVillageSystems(Transform villageRoot)
        {
            // Attach upgrade + edit wiring so Play Mode gets CoC loops without a new menu.
            var go = villageRoot.gameObject;
            if (go.GetComponent<CoCVillageController>() == null)
                go.AddComponent<CoCVillageController>();
        }

        static void BuildHub(Transform parent, float h)
        {
            var hall = Child(parent, "TownHall_Shrine", Vector3.zero);
            var stone = new Color(0.48f, 0.46f, 0.50f);
            var wood = new Color(0.42f, 0.28f, 0.16f);
            var crystal = new Color(0.55f, 0.85f, 1f);

            Prim(hall, PrimitiveType.Cube, new Vector3(0f, 0.2f, 0f), new Vector3(h * 2f, 0.4f, h * 2f), stone);
            Prim(hall, PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f), new Vector3(h * 1.4f, 0.3f, h * 1.4f), stone);
            float p = h * 0.32f;
            foreach (var c in new[] {
                new Vector3(-p, 1.7f, -p), new Vector3(p, 1.7f, -p),
                new Vector3(-p, 1.7f, p), new Vector3(p, 1.7f, p)
            })
                Prim(hall, PrimitiveType.Cube, c, new Vector3(0.4f, 2.2f, 0.4f), wood);
            Prim(hall, PrimitiveType.Cube, new Vector3(0f, 3f, 0f), new Vector3(h * 1.6f, 0.28f, h * 1.6f), wood);
            Prim(hall, PrimitiveType.Cube, new Vector3(0f, 4.4f, 0f), new Vector3(0.55f, 1.8f, 0.55f), crystal, true);
            Fence(hall, h * 0.85f, wood);
        }

        static void BuildCherry(Transform parent, float hub)
        {
            var go = Child(parent, "Orchard_Cherry", Vector3.zero);
            var c = new Color(0.75f, 0.08f, 0.18f);
            float d = hub * 0.65f;
            foreach (var p in new[] {
                new Vector3(-d, 0.35f, -d), new Vector3(d, 0.35f, -d),
                new Vector3(-d, 0.35f, d), new Vector3(d, 0.35f, d)
            })
                Prim(go, PrimitiveType.Sphere, p, Vector3.one * 0.4f, c, true);
        }

        static void BuildWell(Transform parent, Vector3 pos)
        {
            var w = Child(parent, "Well_Fountain", pos);
            var stone = new Color(0.5f, 0.48f, 0.45f);
            var wood = new Color(0.4f, 0.24f, 0.14f);
            var water = new Color(0.25f, 0.55f, 0.85f);
            Prim(w, PrimitiveType.Cylinder, new Vector3(0f, 0.4f, 0f), new Vector3(2f, 0.4f, 2f), stone);
            Prim(w, PrimitiveType.Cylinder, new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 0.06f, 1.4f), water, true);
            Prim(w, PrimitiveType.Cube, new Vector3(-1f, 1.3f, 0f), new Vector3(0.12f, 1.3f, 0.12f), wood);
            Prim(w, PrimitiveType.Cube, new Vector3(1f, 1.3f, 0f), new Vector3(0.12f, 1.3f, 0.12f), wood);
            Prim(w, PrimitiveType.Cube, new Vector3(0f, 2.05f, 0f), new Vector3(2.4f, 0.15f, 1.2f), wood);
        }

        static void BuildStall(Transform parent, Vector3 pos)
        {
            var s = Child(parent, "MarketStall", pos);
            var wood = new Color(0.45f, 0.3f, 0.16f);
            var cloth = new Color(0.9f, 0.35f, 0.2f);
            Prim(s, PrimitiveType.Cube, new Vector3(0f, 0.12f, 0f), new Vector3(StallHalfX * 2f, 0.24f, StallHalfZ * 2f), wood);
            Prim(s, PrimitiveType.Cube, new Vector3(0f, 0.75f, 0.7f), new Vector3(StallHalfX * 1.7f, 0.55f, 1f), wood);
            Prim(s, PrimitiveType.Cube, new Vector3(0f, 2.7f, 0f), new Vector3(StallHalfX * 2.1f, 0.1f, StallHalfZ * 2.1f), cloth);
        }

        static Transform BuildPlot(Transform parent, FruitType type, Vector3 pos, Color fruit, int index, bool makeAlias)
        {
            var plot = Child(parent, "Plot_" + type + "_" + index, pos);
            if (makeAlias)
                Child(plot, "Orchard_" + type, Vector3.zero);
            var soil = new Color(0.36f, 0.24f, 0.14f);
            Prim(plot, PrimitiveType.Cube, new Vector3(0f, 0.06f, 0f),
                new Vector3(PlotHalf * 2f, 0.12f, PlotHalf * 2f), soil);
            Fence(plot, PlotHalf + FenceExtra * 0.5f, new Color(0.42f, 0.3f, 0.18f));

            // Sign
            float z = -(PlotHalf + 0.5f);
            Prim(plot, PrimitiveType.Cube, new Vector3(0f, 0.65f, z), new Vector3(0.1f, 1.1f, 0.1f), new Color(0.4f, 0.28f, 0.16f));
            Prim(plot, PrimitiveType.Cube, new Vector3(0f, 1.25f, z), new Vector3(1.4f, 0.4f, 0.07f), new Color(0.55f, 0.38f, 0.22f));
            var textGO = new GameObject("SignText");
            textGO.transform.SetParent(plot, false);
            textGO.transform.localPosition = new Vector3(0f, 1.25f, z - 0.05f);
            textGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var tm = textGO.AddComponent<TextMesh>();
            tm.text = type.ToString();
            tm.characterSize = 0.16f;
            tm.fontSize = 48;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;

            float span = PlotHalf * 1.15f;
            for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
            {
                float x = Mathf.Lerp(-span * 0.5f, span * 0.5f, c / 2f);
                float zz = Mathf.Lerp(-span * 0.5f, span * 0.5f, r / 2f);
                Prim(plot, PrimitiveType.Sphere, new Vector3(x, 0.4f, zz), Vector3.one * 0.48f, fruit, true);
            }

            var building = plot.gameObject.AddComponent<FruitPlotBuilding>();
            building.Setup(type, fruit);
            return plot;
        }

        static void Fence(Transform parent, float half, Color wood)
        {
            const int n = 3;
            for (int side = 0; side < 4; side++)
            {
                for (int i = 0; i < n; i++)
                {
                    float t0 = i / (float)n;
                    float t1 = (i + 1) / (float)n;
                    Vector3 a = SidePoint(half, side, t0);
                    Vector3 b = SidePoint(half, side, t1);
                    Vector3 mid = (a + b) * 0.5f;
                    float yaw = side * 90f;
                    Prim(parent, PrimitiveType.Cube, a + new Vector3(0f, 0.4f, 0f), new Vector3(0.12f, 0.8f, 0.12f), wood)
                        .localRotation = Quaternion.Euler(0f, yaw, 0f);
                    float len = Vector3.Distance(a, b);
                    foreach (float y in new[] { 0.3f, 0.55f })
                        Prim(parent, PrimitiveType.Cube, mid + new Vector3(0f, y, 0f), new Vector3(len, 0.1f, 0.06f), wood)
                            .localRotation = Quaternion.Euler(0f, yaw, 0f);
                }
            }
        }

        static Vector3 SidePoint(float half, int side, float t)
        {
            if (side == 0) return new Vector3(Mathf.Lerp(-half, half, t), 0f, -half);
            if (side == 1) return new Vector3(half, 0f, Mathf.Lerp(-half, half, t));
            if (side == 2) return new Vector3(Mathf.Lerp(half, -half, t), 0f, half);
            return new Vector3(-half, 0f, Mathf.Lerp(half, -half, t));
        }

        static void BuildBench(Transform parent, Vector3 pos, float yaw)
        {
            var b = Child(parent, "Bench", pos);
            b.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var wood = new Color(0.42f, 0.28f, 0.16f);
            Prim(b, PrimitiveType.Cube, new Vector3(0f, 0.4f, 0f), new Vector3(1.5f, 0.1f, 0.45f), wood);
            Prim(b, PrimitiveType.Cube, new Vector3(0f, 0.7f, -0.18f), new Vector3(1.5f, 0.45f, 0.08f), wood);
        }

        static void BuildLantern(Transform parent, Vector3 pos)
        {
            var l = Child(parent, "Lantern", pos);
            Prim(l, PrimitiveType.Cylinder, new Vector3(0f, 1f, 0f), new Vector3(0.08f, 1f, 0.08f), new Color(0.35f, 0.35f, 0.38f));
            Prim(l, PrimitiveType.Cube, new Vector3(0f, 2.15f, 0f), Vector3.one * 0.3f, new Color(1f, 0.85f, 0.45f), true);
        }

        static Transform Child(Transform parent, string name, Vector3 local)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            return go.transform;
        }

        static Transform Prim(Transform parent, PrimitiveType type, Vector3 local, Vector3 scale, Color color, bool glossy = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Object.Destroy(col);
                else Object.DestroyImmediate(col);
            }
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Unlit/Color");
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null && shader != null)
            {
                var mat = new Material(shader);
                mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                float s = glossy ? 0.7f : 0.06f;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", s);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", s);
                mr.sharedMaterial = mat;
            }
            return go.transform;
        }
    }
}
