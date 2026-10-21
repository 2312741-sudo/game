using System;
using UnityEngine;

namespace TramChanh.Save
{
    /// <summary>JSON text to and from <see cref="SaveData"/>. Never throws.</summary>
    public static class SaveSerializer
    {
        public static string ToJson(SaveData data)
        {
            return JsonUtility.ToJson(data, true);
        }

        public static SaveReadResult Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) { return SaveReadResult.Of(SaveReadStatus.Corrupt); }
            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception)
            {
                return SaveReadResult.Of(SaveReadStatus.Corrupt);
            }
            if (data == null || data.entries == null) { return SaveReadResult.Of(SaveReadStatus.Corrupt); }
            if (data.version != SaveData.CurrentVersion) { return SaveReadResult.Of(SaveReadStatus.VersionMismatch); }
            return new SaveReadResult(SaveReadStatus.Ok, data);
        }
    }
}
