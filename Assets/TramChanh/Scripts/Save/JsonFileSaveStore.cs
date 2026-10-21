using System;
using System.IO;
using System.Text;
using TramChanh.Core;
using UnityEngine;

namespace TramChanh.Save
{
    /// <summary>
    /// Save file on disk. A write goes to a temp file first and then replaces the real file, so a crash
    /// in the middle never leaves a half-written save. Every method catches IO errors and reports them.
    /// </summary>
    public sealed class JsonFileSaveStore : ISaveStore
    {
        public const string DefaultFileName = "tramchanh_save.json";
        private readonly string _path;

        public JsonFileSaveStore(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) { throw new ArgumentException("A file path is required.", nameof(filePath)); }
            _path = filePath;
        }

        /// <summary>The save file in Application.persistentDataPath. Call from the main thread.</summary>
        public static JsonFileSaveStore CreateDefault()
        {
            return new JsonFileSaveStore(Path.Combine(Application.persistentDataPath, DefaultFileName));
        }

        public string FilePath => _path;

        public bool Exists()
        {
            try { return File.Exists(_path); }
            catch (Exception) { return false; }
        }

        public SaveReadResult Read()
        {
            try
            {
                if (!File.Exists(_path)) { return SaveReadResult.Of(SaveReadStatus.Missing); }
                string json = File.ReadAllText(_path, Encoding.UTF8);
                return SaveSerializer.Parse(json);
            }
            catch (Exception)
            {
                return SaveReadResult.Of(SaveReadStatus.IoError);
            }
        }

        public Result Write(SaveData data)
        {
            if (data == null) { return Result.Fail("save.write.null"); }
            string temp = _path + ".tmp";
            try
            {
                string dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir)) { Directory.CreateDirectory(dir); }
                File.WriteAllText(temp, SaveSerializer.ToJson(data), new UTF8Encoding(false));
                if (File.Exists(_path))
                {
                    File.Replace(temp, _path, null);
                }
                else
                {
                    File.Move(temp, _path);
                }
                return Result.Success();
            }
            catch (Exception)
            {
                TryDelete(temp);
                return Result.Fail("save.write.io");
            }
        }

        public Result Delete()
        {
            try
            {
                if (File.Exists(_path)) { File.Delete(_path); }
                return Result.Success();
            }
            catch (Exception)
            {
                return Result.Fail("save.delete.io");
            }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) { File.Delete(path); } }
            catch (Exception) { }
        }
    }
}
