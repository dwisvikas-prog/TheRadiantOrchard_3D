using UnityEngine;
using UnityEngine.AI;
using System.Reflection;

namespace RadiantOrchard
{
    // Attach to ANY GameObject in scene → Press Play → read Console.
    // Tells you EXACTLY what is missing so the game doesn't work.
    public class SceneDiagnostic : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("===== SCENE DIAGNOSTIC =====");

            // GameState
            Check(GameState.Instance != null,
                "GameState",
                "NOT FOUND — add GameState component to GameManagers GameObject");

            // GestureManager
            var gm = FindAnyObjectByType<GestureManager>();
            Check(gm != null, "GestureManager",
                "NOT FOUND — add GestureManager to GameManagers");

            // Camera
            Check(Camera.main != null, "Main Camera",
                "NOT FOUND — camera must be tagged 'MainCamera'");

            // NavMesh
            var navTri = NavMesh.CalculateTriangulation();
            Check(navTri.vertices.Length > 0, "NavMesh",
                "NOT BAKED — go Window > AI > Navigation > Bake");

            // NPCSpawner
            var spawner = FindAnyObjectByType<NPCSpawner>();
            if (!Check(spawner != null, "NPCSpawner",
                "NOT FOUND — add NPCSpawner to GameManagers"))
                goto done;

            // NPCSpawner fields via reflection
            var T = spawner.GetType();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            var prefabVal = T.GetField("stickmanPrefab", flags)?.GetValue(spawner) as GameObject;
            Check(prefabVal != null, "NPCSpawner.stickmanPrefab",
                "NULL — drag GameData/Prefabs/Stickman.prefab into it");

            var poolVal = T.GetField("fruitPool", flags)?.GetValue(spawner) as FruitData[];
            if (Check(poolVal != null && poolVal.Length > 0, "NPCSpawner.fruitPool",
                "EMPTY — assign FruitData_Strawberry, Pineapple, Watermelon from GameData/FruitData/"))
            {
                string names = string.Join(", ",
                    System.Array.ConvertAll(poolVal,
                        x => x != null ? x.displayName : "NULL"));
                Debug.Log($"  OK  NPCSpawner fruits: {names}");
            }

            var ptsVal = T.GetField("spawnPoints", flags)?.GetValue(spawner) as Transform[];
            Check(ptsVal != null && ptsVal.Length > 0, "NPCSpawner.spawnPoints",
                "EMPTY — need at least 3 spawn point Transforms assigned");

            done:
            // LevelManager (warning only — game loop works without it)
            var lm = LevelManager.Instance;
            if (lm == null)
                Debug.LogWarning("  WARN  LevelManager not found — level complete won't trigger");
            else
                Debug.Log("  OK   LevelManager, level = " +
                          (lm.CurrentLevel != null ? lm.CurrentLevel.levelId : "null"));

            // Summary counts
            var sms = FindObjectsByType<StickmanController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var fhs = FindObjectsByType<FruitHarvester>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            Debug.Log($"  INFO  StickmanControllers in scene: {sms.Length}");
            Debug.Log($"  INFO  FruitHarvesters in scene: {fhs.Length}");
            Debug.Log("===== FIX ALL  [ERROR]  ITEMS ABOVE =====");
        }

        static bool Check(bool cond, string label, string failMsg)
        {
            if (cond)
                Debug.Log($"  OK   {label}");
            else
                Debug.LogError($"  ERROR  {label}: {failMsg}");
            return cond;
        }
    }
}
