using System.Text.Json;

namespace TodoTxtDaemon
{
    public interface IWatcher
    {
        bool IsTimeToRun();

        void MarkRun();
    }

    public class WatcherException : Exception
    {
        public WatcherException(string message) : base(message)
        {
        }
    }

    public class Watcher : IWatcher
    {
        private readonly DateTimeProvider _DateTimeProvider;

        private DateTime? _LastRun;

        public Watcher(DateTimeProvider dateTimeProvider)
        {
            _DateTimeProvider = dateTimeProvider;
        }

        public bool IsTimeToRun()
        {
            if (_LastRun == null)
            {
                _LastRun = File.Exists("state.json") ? GetLastRunDate() : DateTime.MinValue;
            }

            return _DateTimeProvider.Today > _LastRun;
        }

        public void MarkRun()
        {
            _LastRun = _DateTimeProvider.Today;
            var stateJson = JsonSerializer.Serialize(new State(_LastRun.Value));
            try
            {
                File.WriteAllText("state.json", stateJson);
            }
            catch (Exception)
            {
                throw new WatcherException("Encountered an unexpected error. " +
                    "Please make sure the daemon is running in the directory containing the executable.");
            }
        }

        private static DateTime GetLastRunDate()
        {
            string stateJson;
            try
            {
                stateJson = File.ReadAllText("state.json");
            }
            catch (Exception)
            {
                throw new WatcherException("Encountered an unexpected error. " +
                    "Please make sure the daemon is running in the directory containing the executable.");
            }
            var state = JsonSerializer.Deserialize<State>(stateJson);

            return state!.LastRun;
        }

        public record State(DateTime LastRun);
    }
}
