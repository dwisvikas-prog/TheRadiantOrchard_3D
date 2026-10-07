using System;
using System.Collections.Generic;
using UnityEngine;

namespace RadiantOrchard
{
    [Serializable]
    public class StringEntry
    {
        public string key;
        public string value;
    }

    [Serializable]
    public class IntEntry
    {
        public string key;
        public int value;
    }

    [Serializable]
    public class DecorationEntry
    {
        public int decorationIndex;
        public Vector3 position;
    }

    [Serializable]
    public class SaveData
    {
        // Bump when a field is added/removed/repurposed below, and branch on
        // it in GameState.LoadGame() if an old save needs migrating.
        public int schemaVersion = 1;
        public int currentVibrancy;
        public int maxVibrancy = 100;
        public string currentLevelId;
        public List<string> completedLevels = new List<string>();
        public List<string> completedVirtues = new List<string>();
        public List<StringEntry> npcEmotionStates = new List<StringEntry>();
        public List<IntEntry> objectiveProgress = new List<IntEntry>();
        public List<StringEntry> fruitZoneStates = new List<StringEntry>();
        public List<DecorationEntry> unlockedDecorations = new List<DecorationEntry>();
    }
}
