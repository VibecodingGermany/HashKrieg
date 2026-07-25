namespace Nova.Core
{
    /// <summary>
    /// No-op implementation of INovaLogger for headless runs and unit tests.
    /// </summary>
    public sealed class NullNovaLogger : INovaLogger
    {
        public static readonly NullNovaLogger Instance = new NullNovaLogger();

        public bool IsEnabled(LogLevel level) => false;
        public void Log(LogLevel level, string message) { }
        public void LogTrace(string message) { }
        public void LogDebug(string message) { }
        public void LogInfo(string message) { }
        public void LogWarn(string message) { }
        public void LogError(string message) { }
    }
}
