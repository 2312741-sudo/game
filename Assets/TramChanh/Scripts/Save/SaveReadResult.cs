namespace TramChanh.Save
{
    public enum SaveReadStatus
    {
        Ok = 0,
        Missing = 1,
        Corrupt = 2,
        VersionMismatch = 3,
        IoError = 4,
    }

    /// <summary>Outcome of reading a save. <see cref="Data"/> is set only when <see cref="Status"/> is Ok.</summary>
    public readonly struct SaveReadResult
    {
        public SaveReadResult(SaveReadStatus status, SaveData data)
        {
            Status = status;
            Data = data;
        }

        public SaveReadStatus Status { get; }
        public SaveData Data { get; }
        public bool IsOk => Status == SaveReadStatus.Ok && Data != null;

        public static SaveReadResult Of(SaveReadStatus status) { return new SaveReadResult(status, null); }
    }
}
