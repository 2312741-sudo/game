namespace TramChanh.Core
{
    public interface IGameClock
    {
        double Now { get; }
        double DeltaTime { get; }
        bool IsPaused { get; }
        void Pause();
        void Resume();
    }
}
