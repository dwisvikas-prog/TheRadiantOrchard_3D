#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

// Same procedural-audio approach as GenerateAmbientAudio.cs (no .wav/.mp3/.ogg
// exists anywhere in the project), applied to actual gameplay feedback instead
// of ambience — a gesture-harvest pluck, a tonic-arrival pop, a heal chime, a
// quiz correct/incorrect cue, and a level-complete fanfare. All pure sine-tone
// synthesis with a simple attack/decay envelope; swap the .wav files under
// Assets/GameData/Audio/SFX for real sound design later — SfxPlayer.cs's call
// sites don't need to change.
public static class GenerateGameplaySFX
{
    const string AudioDir = "Assets/GameData/Audio/SFX";
    const int SampleRate = 44100;

    [MenuItem("Tools/Radiant Orchard/Generate Gameplay SFX")]
    private static void Generate()
    {
        Directory.CreateDirectory(AudioDir);

        WriteWav(AudioDir + "/SFX_GestureSuccess.wav", BuildGestureSuccess(), SampleRate);
        WriteWav(AudioDir + "/SFX_TonicPop.wav", BuildTonicPop(), SampleRate);
        WriteWav(AudioDir + "/SFX_Heal.wav", BuildHeal(), SampleRate);
        WriteWav(AudioDir + "/SFX_QuizCorrect.wav", BuildQuizCorrect(), SampleRate);
        WriteWav(AudioDir + "/SFX_QuizIncorrect.wav", BuildQuizIncorrect(), SampleRate);
        WriteWav(AudioDir + "/SFX_LevelComplete.wav", BuildLevelComplete(), SampleRate);

        AssetDatabase.Refresh();

        var gestureSuccess = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + "/SFX_GestureSuccess.wav");
        var tonicPop = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + "/SFX_TonicPop.wav");
        var heal = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + "/SFX_Heal.wav");
        var quizCorrect = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + "/SFX_QuizCorrect.wav");
        var quizIncorrect = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + "/SFX_QuizIncorrect.wav");
        var levelComplete = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + "/SFX_LevelComplete.wav");

        if (gestureSuccess == null || tonicPop == null || heal == null || quizCorrect == null || quizIncorrect == null || levelComplete == null)
        {
            Debug.LogError("Radiant Orchard: SFX clips failed to import — check AudioImporter settings on Assets/GameData/Audio/SFX/*.wav.");
            return;
        }

        Undo.SetCurrentGroupName("Generate Gameplay SFX");
        int undoGroup = Undo.GetCurrentGroup();

        var existingGO = GameObject.Find("SfxPlayer");
        GameObject go = existingGO;
        if (go == null)
        {
            go = new GameObject("SfxPlayer");
            Undo.RegisterCreatedObjectUndo(go, "Create SfxPlayer");
        }

        var source = go.GetComponent<AudioSource>();
        if (source == null) source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D — gameplay SFX, not positional

        var player = go.GetComponent<RadiantOrchard.SfxPlayer>();
        if (player == null) player = go.AddComponent<RadiantOrchard.SfxPlayer>();

        var so = new SerializedObject(player);
        so.FindProperty("source").objectReferenceValue = source;
        so.FindProperty("gestureSuccess").objectReferenceValue = gestureSuccess;
        so.FindProperty("tonicPop").objectReferenceValue = tonicPop;
        so.FindProperty("heal").objectReferenceValue = heal;
        so.FindProperty("quizCorrect").objectReferenceValue = quizCorrect;
        so.FindProperty("quizIncorrect").objectReferenceValue = quizIncorrect;
        so.FindProperty("levelComplete").objectReferenceValue = levelComplete;
        so.ApplyModifiedPropertiesWithoutUndo();

        Undo.CollapseUndoOperations(undoGroup);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("Radiant Orchard: SfxPlayer wired up with 6 placeholder SFX clips. Save the scene (Ctrl+S).");
    }

    // ---------- procedural tone synthesis ----------

    static float[] Tone(float freq, float durationSeconds, float attack, float decay, float amplitude)
    {
        int n = (int)(durationSeconds * SampleRate);
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float env = Envelope(t, durationSeconds, attack, decay);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * amplitude;
        }
        return data;
    }

    static float Envelope(float t, float duration, float attack, float decay)
    {
        if (t < attack) return t / attack;
        float releaseStart = duration - decay;
        if (t > releaseStart) return Mathf.Clamp01((duration - t) / decay);
        return 1f;
    }

    static float[] Sequence(params float[][] notes)
    {
        int total = 0;
        foreach (var n in notes) total += n.Length;
        var data = new float[total];
        int offset = 0;
        foreach (var n in notes)
        {
            System.Array.Copy(n, 0, data, offset, n.Length);
            offset += n.Length;
        }
        return data;
    }

    static float[] BuildGestureSuccess() => Tone(880f, 0.12f, 0.005f, 0.1f, 0.4f);

    static float[] BuildTonicPop()
    {
        // Quick descending pitch sweep instead of a fixed tone — reads as a "pop".
        const float duration = 0.15f;
        int n = (int)(duration * SampleRate);
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float freq = Mathf.Lerp(1400f, 500f, t / duration);
            float env = Envelope(t, duration, 0.002f, 0.12f);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.5f;
        }
        return data;
    }

    static float[] BuildHeal() => Sequence(
        Tone(523.25f, 0.15f, 0.005f, 0.12f, 0.35f), // C5
        Tone(659.25f, 0.15f, 0.005f, 0.12f, 0.35f), // E5
        Tone(783.99f, 0.22f, 0.005f, 0.18f, 0.40f)  // G5, held a little longer
    );

    static float[] BuildQuizCorrect() => Sequence(
        Tone(783.99f, 0.14f, 0.005f, 0.10f, 0.40f), // G5
        Tone(1046.50f, 0.22f, 0.005f, 0.18f, 0.45f) // C6
    );

    static float[] BuildQuizIncorrect() => Sequence(
        // Gentle descending pair, not harsh/punishing — this is a kids' virtue
        // game, "try again" not "you failed".
        Tone(659.25f, 0.16f, 0.01f, 0.12f, 0.30f), // E5
        Tone(523.25f, 0.20f, 0.01f, 0.16f, 0.28f)  // C5
    );

    static float[] BuildLevelComplete() => Sequence(
        Tone(523.25f, 0.14f, 0.005f, 0.10f, 0.35f), // C5
        Tone(659.25f, 0.14f, 0.005f, 0.10f, 0.35f), // E5
        Tone(783.99f, 0.14f, 0.005f, 0.10f, 0.40f), // G5
        Tone(1046.50f, 0.35f, 0.005f, 0.30f, 0.45f) // C6, held
    );

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
