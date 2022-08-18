using System.Text.Json;

namespace TodoTxtDaemon
{
    public interface IWatcher
    {
        bool IsTimeToRun();

        void MarkRun();
    }

    public class Watcher : IWatcher
    {
        private readonly DateTimeProvider _DateTimeProvider;

        private readonly string _StateJsonPath;

        private DateTime? _LastRun;

        public Watcher(IHostEnvironment environment, DateTimeProvider dateTimeProvider)
        {
            _DateTimeProvider = dateTimeProvider;
            _StateJsonPath = Path.Combine(environment.ContentRootPath, "state.json");
        }

        public bool IsTimeToRun()
        {
            if (_LastRun == null)
            {
                if (File.Exists(_StateJsonPath))
                {
                    var stateJson = File.ReadAllText(_StateJsonPath);
                    var state = JsonSerializer.Deserialize<State>(stateJson);
                    _LastRun = state!.LastRun;
                }
                else
                {
                    _LastRun = DateTime.MinValue;
                }
            }

            return _DateTimeProvider.Today > _LastRun;
        }

        public void MarkRun()
        {
            _LastRun = _DateTimeProvider.Today;
            var stateJson = JsonSerializer.Serialize(new State(_LastRun.Value));
            File.WriteAllText(_StateJsonPath, stateJson);
        }

        public record State(DateTime LastRun);
    }
}
