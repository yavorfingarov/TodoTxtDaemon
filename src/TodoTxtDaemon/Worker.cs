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
            _Logger.LogApplicationStarted();
            _Logger.LogMonitoring();
            do
            {
                try
                {
                    if (_Watcher.IsTimeToRun())
                    {
                        _Watcher.MarkRun();
                        _Mover.Run();
                        _Logger.LogMonitoring();
                    }
                }
                catch (MoverException ex)
                {
                    _Logger.LogErrorMessage(ex);
                    _Logger.LogMonitoring();
                }
                catch (Exception ex)
                {
                    _Logger.LogCriticalError(ex);
                    _Lifetime.StopApplication();
                }
            } while (await IsWaiting(stoppingToken));
            _Logger.LogApplicationStopped();
        }

        private static async Task<bool> IsWaiting(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }
    }

    internal static partial class LoggerExtensions
    {
        private static readonly Action<ILogger, string?, int, Exception?> _DaemonStarted = LoggerMessage.Define<string?, int>(
            LogLevel.Information, default,
            "Daemon started. Version: {Version} / Process Id: {ProcessId}");

        private static readonly Action<ILogger, Exception?> _Monitoring = LoggerMessage.Define(
            LogLevel.Information, default,
            "Monitoring...");

        private static readonly Action<ILogger, string, Exception?> _ErrorMessage = LoggerMessage.Define<string>(
            LogLevel.Error, default,
            "{Message}");

        private static readonly Action<ILogger, Exception> _CriticalError = LoggerMessage.Define(
            LogLevel.Critical, default,
            "Unhandled exception.");

        private static readonly Action<ILogger, int, Exception?> _DaemonStopped = LoggerMessage.Define<int>(
            LogLevel.Information, default,
            "Daemon stopped. Process Id: {ProcessId}");

        public static void LogApplicationStarted(this ILogger logger)
        {
            var version = typeof(LoggerExtensions).Assembly.GetName().Version?.ToString(3);
            _DaemonStarted(logger, version, Environment.ProcessId, null);
        }

        public static void LogMonitoring(this ILogger logger)
        {
            _Monitoring(logger, null);
        }

        public static void LogErrorMessage(this ILogger logger, MoverException exception)
        {
            _ErrorMessage(logger, exception.Message, null);
        }

        public static void LogCriticalError(this ILogger logger, Exception exception)
        {
            _CriticalError(logger, exception);
        }

        public static void LogApplicationStopped(this ILogger logger)
        {
            _DaemonStopped(logger, Environment.ProcessId, null);
        }
    }
}
