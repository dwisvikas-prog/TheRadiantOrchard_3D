using System.Collections;
using UnityEngine;

namespace RadiantOrchard
{
    /// <summary>
    /// After a fruit is already taught — no full lesson bubble.
    /// Spawns fruit on the known tree + top alert; player finds it themselves.
    /// </summary>
    public class QuietFruitAssist : MonoBehaviour
    {
        FruitLessonBook.Lesson lesson;
        StickmanController visitor;
        FruitHarvester fruit;
        Coroutine harvestTimeoutCo;
        bool torndown;

        public static bool IsActive => FindFirstObjectByType<QuietFruitAssist>() != null;

        /// <summary>Visitor's patience ran out before harvest — tear this down if it's theirs.</summary>
        public static void CancelFor(StickmanController v)
        {
            if (v == null) return;
            var a = FindFirstObjectByType<QuietFruitAssist>();
            if (a != null && a.visitor == v)
                a.Teardown();
        }

        /// <summary>
        /// Idempotent cleanup shared by CancelFor() and the harvest-wait
        /// timeout fallback. Guarded by `torndown` so it is always safe to
        /// call twice. Does NOT heal or award Vibrancy — recovery only.
        /// </summary>
        void Teardown()
        {
            if (torndown) return;
            torndown = true;
            if (harvestTimeoutCo != null) StopCoroutine(harvestTimeoutCo);
            harvestTimeoutCo = null;
            FruitHarvester.OnAnyHarvested -= OnHarvested;
            EmptyIslandVisitorAlert.HideAll();
            IslandTapTrack.Hide();
            // Same reasoning as FruitPlantLessonFlow.CancelFor — without
            // this the fruit sits on the tree forever, unresponsive to
            // every future tap, since its stickman can't receive it.
            if (fruit != null) fruit.Despawn();
            Destroy(gameObject);
        }

        public static void StartFor(FruitLessonBook.Lesson lesson, StickmanController visitor)
        {
            if (!Application.isPlaying) return;
            if (FindFirstObjectByType<QuietFruitAssist>() != null) return;

            var go = new GameObject("QuietFruitAssist_" + lesson.fruitName);
            var a = go.AddComponent<QuietFruitAssist>();
            a.Begin(lesson, visitor);
        }

        void Begin(FruitLessonBook.Lesson L, StickmanController v)
        {
            lesson = L;
            visitor = v;
            ActiveFruitTracker.Set(L.fruit);

            if (FindFirstObjectByType<GestureManager>() == null)
                new GameObject("GestureManager").AddComponent<GestureManager>();

            var tracker = OrchardTreeTracker.Ensure();
            var planted = tracker.Find(L.fruit);
            Transform tree = planted != null
                ? (OrchardTreeTracker.ResolveTreeVisual(planted.root) ?? planted.root)
                : null;

            if (tree == null)
            {
                Destroy(gameObject);
                FruitTeachProgress.ClearTaught(L.fruit);
                FruitPlantLessonFlow.StartFor(L, v);
                return;
            }

            TreeNameBoard.Ensure(tree, FruitTreeCatalog.Get(L.fruit).label,
                FruitTreeCatalog.Get(L.fruit).fruitColor, L.fruit);

            EmptyIslandVisitorAlert.ShowArrival(L, v, tree);
            EmptyIslandCoachBar.GoIdleQuiet();

            var fruitData = FruitTreeCatalog.MakeFruitData(L);
            Vector3 fruitPos = tree.position + new Vector3(0.55f, 1.35f, 0.25f);
            if (L.fruit != FruitType.Strawberry)
                fruitPos = tree.position + new Vector3(0.65f, 1.75f, 0.3f);

            fruit = FruitHarvester.GetPooled(fruitData, fruitPos, 2.4f);
            if (visitor != null) fruit.Initialize(fruitData, visitor);
            else fruit.Initialize(fruitData, null);
            fruit.BindGestures();

            FruitHarvester.OnAnyHarvested -= OnHarvested;
            FruitHarvester.OnAnyHarvested += OnHarvested;

            // Conservative fallback (matches FirstPlantTutorial's 90s
            // harvest-wait convention): if the harvest event never arrives,
            // don't leave this already-taught fruit waiting forever — that
            // would permanently block FruitPlantLessonFlow.IsBusy for every
            // future visitor.
            if (harvestTimeoutCo != null) StopCoroutine(harvestTimeoutCo);
            harvestTimeoutCo = StartCoroutine(HarvestTimeoutFallback());

            // No automatic glow/label here — already-taught fruits only show
            // the top "Visitor!" + (i) banner. Tapping (i) → Show tree reveals
            // the glow/gesture marker on demand (EmptyIslandVisitorAlert.FocusTree).
        }

        IEnumerator HarvestTimeoutFallback()
        {
            yield return new WaitForSeconds(90f);
            if (torndown) yield break;
            Teardown();
        }

        void OnHarvested()
        {
            if (harvestTimeoutCo != null) StopCoroutine(harvestTimeoutCo);
            harvestTimeoutCo = null;
            FruitHarvester.OnAnyHarvested -= OnHarvested;
            EmptyIslandVisitorAlert.HideAll();
            IslandTapTrack.Hide();
            // Safety net: normally every fruit is already marked taught before
            // it ever reaches this quiet path, so this is a no-op — except on
            // a save where FruitTeachProgress was already all-9 from a prior
            // session but Level 1 completion was never fired this session.
            EmptyIslandLevelProgress.CheckLevel1Completion();
            StartCoroutine(EndSoon());
        }

        IEnumerator EndSoon()
        {
            yield return new WaitForSeconds(2.5f);
            EmptyIslandCoachBar.GoIdleQuiet();
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            FruitHarvester.OnAnyHarvested -= OnHarvested;
            ActiveFruitTracker.Clear();
        }
    }
}
