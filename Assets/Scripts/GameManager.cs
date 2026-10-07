using UnityEngine;
using System.Collections.Generic;
using RadiantOrchard;

/// <summary>
/// Level 1 coordinator. The gesture -> harvest -> tonic -> heal -> vibrancy
/// loop is fully self-driven per-object by RadiantOrchard's GestureManager,
/// FruitHarvester, TonicFlight and StickmanController/GameState — this no
/// longer duplicates that pipeline (it used to, via a second flat-namespace
/// gesture/tonic system that never got wired up and only caused a
/// competing-systems NullReferenceException). This class just tracks the
/// active RadiantOrchard.StickmanController roster for anything else
/// (UI, tutorial, save-adjacent systems) that wants to query "who's active"
/// without re-deriving it.
/// </summary>
public class GameManager : MonoBehaviour
{
    readonly List<StickmanController> activeStickmen = new List<StickmanController>();
    public IReadOnlyList<StickmanController> ActiveStickmen => activeStickmen;

    void OnEnable()
    {
        RefreshActiveStickmen();
        if (GameState.Instance != null)
            GameState.Instance.StickmanHealed += RefreshActiveStickmen;
    }

    void OnDisable()
    {
        if (GameState.Instance != null)
            GameState.Instance.StickmanHealed -= RefreshActiveStickmen;
    }

    void RefreshActiveStickmen()
    {
        activeStickmen.Clear();
        activeStickmen.AddRange(FindObjectsByType<StickmanController>(FindObjectsSortMode.None));
    }
}
