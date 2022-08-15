namespace TodoTxtDaemon
{
    public sealed class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _Logger;

        private readonly IHostEnvironment _HostEnvironment;

        private readonly IHostApplicationLifetime _Lifetime;

        private readonly IWatcher _Watcher;

        private readonly IMover _Mover;

        public Worker(ILogger<Worker> logger, IHostEnvironment hostEnvironment,
            IHostApplicationLifetime lifetime, IWatcher watcher, IMover mover)
        {
            _Logger = logger;
            _HostEnvironment = hostEnvironment;
            _Lifetime = lifetime;
            _Watcher = watcher;
            _Mover = mover;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _Lifetime.ApplicationStopped.Register(OnStop);
            _Logger.LogInformation("Application started. Current working directory: {Path}", _HostEnvironment.ContentRootPath);
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

        private void OnStop() => _Logger.LogInformation("Monitoring stopped.");

        private bool IsCriticalException(Exception exception)
        {
            var isCritical = true;
            switch (exception)
            {
                case MoverException:
                    _Logger.LogError("{Message}", exception.Message);
                    isCritical = false;
                    break;
                case WatcherException:
                    _Logger.LogCritical("{Message}", exception.Message);
                    break;
                default:
                    _Logger.LogCritical(exception, "Unhandled exception.");
                    break;
            }

            return isCritical;
        }
    }
}
