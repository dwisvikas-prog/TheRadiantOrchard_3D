using UnityEngine;

namespace RadiantOrchard
{
    // CoC village runtime: edit mode (E), upgrade (U), collect (C), optional harvest waves.
    // Grid-snaps plot moves while Edit Mode is on.
    public class CoCVillageController : MonoBehaviour
    {
        [SerializeField] float gridSize = 2f;
        [SerializeField] float waveInterval = 45f;
        [SerializeField] bool enableHarvestWaves = true;

        EditModeManager editMode;
        float waveTimer;
        FruitPlotBuilding[] plots;

        void Start()
        {
            EnsureEditMode();
            RefreshPlots();
            RegisterPlotsForEdit();
            waveTimer = waveInterval;
            Debug.Log("[CoCVillage] E=Edit  U=Upgrade nearest  C=Collect nearest  (waves optional)");
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.E)) ToggleEdit();
            if (Input.GetKeyDown(KeyCode.U)) UpgradeNearest();
            if (Input.GetKeyDown(KeyCode.C)) CollectNearest();

            if (editMode != null && editMode.EditModeActive)
                SnapDraggingToGrid();

            if (enableHarvestWaves)
            {
                waveTimer -= Time.deltaTime;
                if (waveTimer <= 0f)
                {
                    waveTimer = waveInterval;
                    TriggerHarvestWave();
                }
            }
        }

        void EnsureEditMode()
        {
            editMode = FindFirstObjectByType<EditModeManager>();
            if (editMode == null)
            {
                var go = GameObject.Find("GameManagers") ?? new GameObject("GameManagers");
                editMode = go.AddComponent<EditModeManager>();
            }
            editMode.SetGridSnap(gridSize, MainSceneSquareGround.GrassHalf - 2f);
        }

        void RefreshPlots()
        {
            plots = GetComponentsInChildren<FruitPlotBuilding>(true);
        }

        void RegisterPlotsForEdit()
        {
            if (editMode == null) return;
            RefreshPlots();
            foreach (var p in plots)
                if (p != null) editMode.RegisterDecoration(p.transform);
        }

        void ToggleEdit()
        {
            if (editMode == null) return;
            if (editMode.EditModeActive) editMode.ExitEditMode();
            else
            {
                RegisterPlotsForEdit();
                editMode.EnterEditMode();
            }
            Debug.Log(editMode.EditModeActive
                ? "[CoCVillage] EDIT MODE ON — drag plots (grid snap)"
                : "[CoCVillage] EDIT MODE OFF");
        }

        void SnapDraggingToGrid()
        {
            // EditModeManager applies snap internally when grid configured.
        }

        void UpgradeNearest()
        {
            var p = FindNearestPlot();
            if (p != null) p.TryUpgrade();
        }

        void CollectNearest()
        {
            var p = FindNearestPlot();
            if (p != null) p.Collect();
        }

        FruitPlotBuilding FindNearestPlot()
        {
            RefreshPlots();
            var cam = Camera.main;
            if (cam == null || plots == null || plots.Length == 0) return null;

            Vector2 screen = Input.mousePosition;
            FruitPlotBuilding best = null;
            float bestD = 140f;
            foreach (var p in plots)
            {
                if (p == null) continue;
                float d = Vector2.Distance(cam.WorldToScreenPoint(p.transform.position), screen);
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        // Optional “attack-style” wave: nudge vibrancy + log — hooks existing loop.
        void TriggerHarvestWave()
        {
            RefreshPlots();
            if (plots == null || plots.Length == 0) return;
            var p = plots[Random.Range(0, plots.Length)];
            if (p == null) return;
            p.Collect();
            Debug.Log($"[CoCVillage] Harvest wave → {p.FruitType}");
        }
    }
}
