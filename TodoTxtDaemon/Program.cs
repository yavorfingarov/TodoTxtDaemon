using NLog.Extensions.Logging;

namespace TodoTxtDaemon
{
    public static class Program
    {
        public static void Main()
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddNLog();
                })
                .ConfigureServices(services =>
                {
                    services.AddHostedService<Worker>();
                    services.AddSingleton<DateTimeProvider>();
                    services.AddSingleton<IWatcher, Watcher>();
                    services.AddSingleton<IMover, Mover>();
                })
                .Build();

            host.Run();
        }
    }
}
