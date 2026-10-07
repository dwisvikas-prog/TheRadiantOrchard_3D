#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.IO;

// No .wav/.mp3/.ogg exists anywhere in the project (audited via full-project
// file search before writing this). Rather than leave the scene silent,
// this synthesizes simple procedural placeholder loops (filtered/shaped
// noise — brown-noise rush for water, sparse tone blips for birds) and
// writes them as real imported .wav assets, then wires up AudioSources
// exactly per the original spec (3D/2D blend, loop, playOnAwake, distance
// falloff). Swap Assets/GameData/Audio/*.wav for real recordings later —
// nothing else needs to change, the AudioSource setup stays valid.
public static class GenerateAmbientAudio
{
    const string AudioDir = "Assets/GameData/Audio";
    const int SampleRate = 44100;

    [MenuItem("Tools/Radiant Orchard/Generate Ambient Audio")]
    private static void Generate()
    {
        Directory.CreateDirectory(AudioDir);

        WriteWav(AudioDir + "/Placeholder_Waterfall.wav", BuildWaterfallNoise(4f), SampleRate);
        WriteWav(AudioDir + "/Placeholder_River.wav", BuildRiverNoise(4f), SampleRate);
        WriteWav(AudioDir + "/Placeholder_GardenAmbient.wav", BuildGardenAmbient(8f), SampleRate);

        AssetDatabase.Refresh();

        var waterfallClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + "/Placeholder_Waterfall.wav");
        var riverClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + "/Placeholder_River.wav");
        var ambientClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + "/Placeholder_GardenAmbient.wav");

        if (waterfallClip == null || riverClip == null || ambientClip == null)
        {
            Debug.LogError("Radiant Orchard: audio clips failed to import — check the AudioImporter settings on Assets/GameData/Audio/*.wav.");
            return;
        }

        Undo.SetCurrentGroupName("Generate Ambient Audio");
        int undoGroup = Undo.GetCurrentGroup();

        int waterfallCount = 0;
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (t.name != "Waterfall") continue;
            ConfigureSource(t.gameObject, waterfallClip, volume: 0.85f, spatialBlend: 1f, minDist: 5f, maxDist: 30f);
            waterfallCount++;
        }

        var pond = GameObject.Find("Pond");
        // WaterFeatures/Pond is the hand-placed river/pond feature (WestZone_Pond's
        // Pond is a separate procedural-generator child) — GameObject.Find returns
        // the first match in hierarchy order, so verify parent before trusting it.
        var waterFeatures = GameObject.Find("WaterFeatures");
        if (waterFeatures != null)
        {
            foreach (Transform c in waterFeatures.transform)
            {
                if (c.name == "Pond")
                {
                    ConfigureSource(c.gameObject, riverClip, volume: 0.4f, spatialBlend: 1f, minDist: 5f, maxDist: 30f);
                    break;
                }
            }
        }

        var atmosphere = GameObject.Find("IslandAtmosphere");
        GameObject ambientGO = null;
        if (atmosphere != null)
        {
            var existing = atmosphere.transform.Find("AmbientAudio_Garden");
            if (existing != null) ambientGO = existing.gameObject;
        }
        if (ambientGO == null)
        {
            ambientGO = new GameObject("AmbientAudio_Garden");
            Undo.RegisterCreatedObjectUndo(ambientGO, "Create Ambient Audio");
            if (atmosphere != null) ambientGO.transform.SetParent(atmosphere.transform, false);
        }
        ConfigureSource(ambientGO, ambientClip, volume: 0.22f, spatialBlend: 0f, minDist: 1f, maxDist: 1f);

        Undo.CollapseUndoOperations(undoGroup);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log($"Radiant Orchard: ambient audio wired up on {waterfallCount} waterfall(s), pond, and garden ambience. Save the scene (Ctrl+S).");
    }

    static void ConfigureSource(GameObject go, AudioClip clip, float volume, float spatialBlend, float minDist, float maxDist)
    {
        var existing = go.GetComponent<AudioSource>();
        var src = existing != null ? existing : go.AddComponent<AudioSource>();
        if (existing == null) Undo.RegisterCreatedObjectUndo(src, "Add AudioSource");
        else Undo.RegisterCompleteObjectUndo(src, "Configure AudioSource");

        src.clip = clip;
        src.loop = true;
        src.playOnAwake = true;
        src.spatialBlend = spatialBlend;
        src.volume = volume;
        if (spatialBlend > 0f)
        {
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = minDist;
            src.maxDistance = maxDist;
        }
    }

    // ---------- procedural noise synthesis ----------

    static float[] BuildWaterfallNoise(float seconds)
    {
        int n = (int)(seconds * SampleRate);
        var data = new float[n];
        var rand = new System.Random(1);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float white = (float)(rand.NextDouble() * 2.0 - 1.0);
            lp += (white - lp) * 0.35f; // light filtering — keeps a hissy "rushing" edge
            float turbulence = 1f + 0.15f * Mathf.Sin(2f * Mathf.PI * 0.6f * i / SampleRate);
            data[i] = Mathf.Clamp(lp * turbulence * 0.9f, -1f, 1f);
        }
        FadeEdges(data, 0.05f);
        return data;
    }

    static float[] BuildRiverNoise(float seconds)
    {
        int n = (int)(seconds * SampleRate);
        var data = new float[n];
        var rand = new System.Random(2);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float white = (float)(rand.NextDouble() * 2.0 - 1.0);
            lp += (white - lp) * 0.12f; // heavier filtering — softer, gentler burble
            float ripple = 1f + 0.1f * Mathf.Sin(2f * Mathf.PI * 0.25f * i / SampleRate);
            data[i] = Mathf.Clamp(lp * ripple * 0.6f, -1f, 1f);
        }
        FadeEdges(data, 0.05f);
        return data;
    }

    static float[] BuildGardenAmbient(float seconds)
    {
        int n = (int)(seconds * SampleRate);
        var data = new float[n];
        var rand = new System.Random(3);
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float white = (float)(rand.NextDouble() * 2.0 - 1.0);
            lp += (white - lp) * 0.06f; // heavy filtering — soft wind bed
            data[i] = lp * 0.35f;
        }

        // Sparse synthesized "chirp" blips scattered through the loop.
        int chirpCount = 5;
        for (int c = 0; c < chirpCount; c++)
        {
            int start = rand.Next((int)(0.3f * SampleRate), n - (int)(0.3f * SampleRate));
            int len = (int)(SampleRate * UnityEngine.Random.Range(0.08f, 0.16f));
            float freq = UnityEngine.Random.Range(1800f, 3200f);
            for (int i = 0; i < len && start + i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Sin(Mathf.PI * i / len); // quick attack/decay
                data[start + i] += Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.25f;
            }
        }

        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
        FadeEdges(data, 0.08f);
        return data;
    }

    static void FadeEdges(float[] data, float fadeSeconds)
    {
        int fadeSamples = (int)(fadeSeconds * SampleRate);
        for (int i = 0; i < fadeSamples && i < data.Length; i++)
        {
            float t = i / (float)fadeSamples;
            data[i] *= t;
            data[data.Length - 1 - i] *= t;
        }
    }

    static void WriteWav(string path, float[] samples, int sampleRate)
    {
        short[] pcm = new short[samples.Length];
        for (int i = 0; i < samples.Length; i++)
            pcm[i] = (short)Mathf.Clamp(samples[i] * short.MaxValue, short.MinValue, short.MaxValue);

        using (var fs = new FileStream(path, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            int byteRate = sampleRate * 2;
            int dataSize = pcm.Length * 2;

            bw.Write(new[] { 'R', 'I', 'F', 'F' });
            bw.Write(36 + dataSize);
            bw.Write(new[] { 'W', 'A', 'V', 'E' });
            bw.Write(new[] { 'f', 'm', 't', ' ' });
            bw.Write(16);
            bw.Write((short)1);      // PCM
            bw.Write((short)1);      // mono
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write((short)2);      // block align
            bw.Write((short)16);     // bits per sample
            bw.Write(new[] { 'd', 'a', 't', 'a' });
            bw.Write(dataSize);
            foreach (var s in pcm) bw.Write(s);
        }
    }
}
#endif
