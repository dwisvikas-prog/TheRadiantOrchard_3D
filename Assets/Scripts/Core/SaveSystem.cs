using System;
using System.IO;
using UnityEngine;

namespace RadiantOrchard
{
    // Local JSON save at Application.persistentDataPath — works unchanged on
    // Android and iOS, and is the only persistence approach in this project
    // (audit found no existing save system to reuse).
    public static class SaveSystem
    {
        private const string FileName = "radiant_orchard_save.json";
        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static void Save(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveSystem: failed to save progress — {e.Message}");
            }
        }

        public static SaveData Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                string json = File.ReadAllText(FilePath);
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveSystem: failed to load save file, starting fresh — {e.Message}");
                return null;
            }
        }

        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveSystem: failed to delete save — {e.Message}");
            }
        }
    }
}
