using NLog.Extensions.Logging;

namespace TodoTxtDaemon
{
    public static class Program
    {
        public static void Main()
        {
            var host = Host.CreateDefaultBuilder()
                .UseContentRoot(Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration(configuration =>
                {
                    configuration.Sources.Clear();
                    configuration.AddIniFile("appsettings.ini", optional: true, reloadOnChange: true);
                })
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
