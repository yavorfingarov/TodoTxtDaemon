namespace TodoTxtDaemon
{
    public sealed class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _Logger;

        private readonly IHostApplicationLifetime _Lifetime;

        private readonly IWatcher _Watcher;

        private readonly IMover _Mover;

        public Worker(ILogger<Worker> logger, IHostApplicationLifetime lifetime, IWatcher watcher, IMover mover)
        {
            _Logger = logger;
            _Lifetime = lifetime;
            _Watcher = watcher;
            _Mover = mover;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _Logger.LogApplicationStarted(GetType().Assembly.GetName().Version?.ToString(3), Environment.ProcessId);
            _Logger.LogMonitoringStatus();
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_Watcher.IsTimeToRun())
                    {
                        _Watcher.MarkRun();
                        _Mover.Run();
                        _Logger.LogMonitoringStatus();
                    }
                }
                catch (Exception ex)
                {
                    if (IsCriticalException(ex))
                    {
                        break;
                    }
                    _Logger.LogMonitoringStatus();
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            _Lifetime.StopApplication();
        }

        private bool IsCriticalException(Exception exception)
        {
            var isCritical = false;
            switch (exception)
            {
                case MoverException:
                    _Logger.LogErrorMessage(exception.Message);
                    break;
                default:
                    _Logger.LogCriticalError(exception);
                    isCritical = true;
                    break;
            }

            return isCritical;
        }
    }

    internal static partial class LoggerExtensions
    {
        private static readonly Action<ILogger, string?, int, Exception?> _ApplicationStarted = LoggerMessage.Define<string?, int>(
            LogLevel.Information, default,
            "TodoTxtDaemon {Version} started. Process Id: {ProcessId}");

        private static readonly Action<ILogger, Exception?> _MonitoringStatus = LoggerMessage.Define(
            LogLevel.Information, default,
            "Monitoring...");

        private static readonly Action<ILogger, string, Exception?> _ErrorMessage = LoggerMessage.Define<string>(
            LogLevel.Error, default,
            "{Message}");

        private static readonly Action<ILogger, Exception> _CriticalError = LoggerMessage.Define(
            LogLevel.Critical, default,
            "Unhandled exception.");

        public static void LogApplicationStarted(this ILogger logger, string? version, int processId)
        {
            _ApplicationStarted(logger, version, processId, null);
        }

        public static void LogMonitoringStatus(this ILogger logger)
        {
            _MonitoringStatus(logger, null);
        }

        public static void LogErrorMessage(this ILogger logger, string message)
        {
            _ErrorMessage(logger, message, null);
        }

        public static void LogCriticalError(this ILogger logger, Exception exception)
        {
            _CriticalError(logger, exception);
        }
    }
}
