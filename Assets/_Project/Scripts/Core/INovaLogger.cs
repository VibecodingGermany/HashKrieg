namespace Nova.Core
{
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4
    }

    /// <summary>
    /// Engine-decoupled logging interface for simulation and core systems.
    /// Injected into SimulationKernel and ISimSystems to prevent static UnityEngine calls.
    /// </summary>
    public interface INovaLogger
    {
        bool IsEnabled(LogLevel level);
        void Log(LogLevel level, string message);
        void LogTrace(string message);
        void LogDebug(string message);
        void LogInfo(string message);
        void LogWarn(string message);
        void LogError(string message);
    }
}
