using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class IslandChunk
{
    public string chunkName;
    public GameObject chunkObject;       // pre-placed in scene, scale 0 initially (except Level 1 zone)
    public int unlockAtLevel;
    public Renderer[] renderersToColor;  // must use the GreyToColorRestore shader
    public ParticleSystem growthVFX;
}

/// <summary>
/// Grows and colors in island zones as the player completes levels, giving
/// the "grey lonely rock -> thriving paradise" progression from the PRD.
/// Wire chunk list in the Inspector; call OnLevelCompleted from GameManager.
/// </summary>
public class IslandGrowthController : MonoBehaviour
{
    [SerializeField] List<IslandChunk> chunks = new List<IslandChunk>();
    [SerializeField] float growDuration = 1.5f;
    [SerializeField] AnimationCurve growCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    static readonly int ColorProgress = Shader.PropertyToID("_ColorRestorationProgress");

    void Start()
    {
        foreach (var chunk in chunks)
        {
            if (chunk.unlockAtLevel > 1)
                chunk.chunkObject.transform.localScale = Vector3.zero;
        }
    }

    public void OnLevelCompleted(int newLevel)
    {
        foreach (var chunk in chunks)
        {
            if (chunk.unlockAtLevel == newLevel)
                StartCoroutine(GrowChunk(chunk));
        }
    }

    IEnumerator GrowChunk(IslandChunk chunk)
    {
        chunk.chunkObject.SetActive(true);
        chunk.growthVFX?.Play();

        Vector3 targetScale = Vector3.one;
        float t = 0f;

        var mats = new List<Material>();
        foreach (var r in chunk.renderersToColor)
            mats.Add(r.material);

        while (t < growDuration)
        {
            t += Time.deltaTime;
            float progress = growCurve.Evaluate(t / growDuration);

            chunk.chunkObject.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, progress);
            foreach (var mat in mats)
                mat.SetFloat(ColorProgress, progress);

            yield return null;
        }

        chunk.chunkObject.transform.localScale = targetScale;
    }
}
