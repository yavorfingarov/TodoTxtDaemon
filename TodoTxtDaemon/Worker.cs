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
            _Logger.LogInformation("TodoTxtDaemon {Version} started. Process Id: {ProcessId}",
                GetType().Assembly.GetName().Version?.ToString(3), Environment.ProcessId);
            _Logger.LogInformation("Monitoring...");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_Watcher.IsTimeToRun())
                    {
                        _Watcher.MarkRun();
                        _Mover.Run();
                        _Logger.LogInformation("Monitoring...");
                    }
                }
                catch (Exception ex)
                {
                    if (IsCriticalException(ex))
                    {
                        break;
                    }
                    _Logger.LogInformation("Monitoring...");
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
                    _Logger.LogError("{Message}", exception.Message);
                    break;
                default:
                    _Logger.LogCritical(exception, "Unhandled exception.");
                    isCritical = true;
                    break;
            }

            return isCritical;
        }
    }
}
