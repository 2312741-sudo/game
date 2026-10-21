using TramChanh.Core;

namespace TramChanh.Save
{
    /// <summary>Where the save lives. Implementations never throw to the caller.</summary>
    public interface ISaveStore
    {
        bool Exists();
        SaveReadResult Read();
        Result Write(SaveData data);
        Result Delete();
    }
}
