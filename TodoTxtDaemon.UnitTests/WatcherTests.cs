using System.Text.Json;

namespace TodoTxtDaemon.UnitTests
{
    public sealed class WatcherTests : IDisposable
    {
        private readonly Mock<DateTimeProvider> _DateTimeProviderMock;

        private readonly IWatcher _Watcher;

        private readonly DateTime _Today;

        public WatcherTests()
        {
            _DateTimeProviderMock = new Mock<DateTimeProvider>(MockBehavior.Strict);
            _Watcher = new Watcher(_DateTimeProviderMock.Object);
            _Today = DateTime.Today;
            _DateTimeProviderMock.Setup(d => d.Today).Returns(_Today);
        }

        [Fact]
        public void IsTimeToRun_Throws_WhenStateJsonIsLocked()
        {
            using var fileStream = File.Open("state.json", FileMode.OpenOrCreate);

            var exception = Assert.Throws<WatcherException>(() => _Watcher.IsTimeToRun());
            Assert.Equal("Encountered an unexpected error. " +
                    "Please make sure the daemon is running in the directory containing the executable.",
                    exception.Message);
            VerifyNoOtherCalls();
        }

        [Fact]
        public void IsTimeToRun_ReturnsTrue_WhenStateJsonIsMissing()
        {
            Assert.True(_Watcher.IsTimeToRun());
            _DateTimeProviderMock.Verify(d => d.Today, Times.Once);
            VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(-2)]
        [InlineData(-5)]
        public void IsTimeToRun_ReturnsTrue_WhenLastRunWasMoreThanADayAgo(int lastRunDayOffset)
        {
            var lastRun = _Today.AddDays(lastRunDayOffset);
            Write(new Watcher.State(lastRun));

            Assert.True(_Watcher.IsTimeToRun());
            CheckCachedState();
            VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(5)]
        public void IsTimeToRun_ReturnsFalse_WhenLastRunIsLessThanADayAgo(int lastRunDayOffset)
        {
            var lastRun = _Today.AddDays(lastRunDayOffset);
            Write(new Watcher.State(lastRun));

            Assert.False(_Watcher.IsTimeToRun());
            CheckCachedState();
            VerifyNoOtherCalls();
        }

        [Fact]
        public void MarkRun_Throws_WhenStateJsonIsLocked()
        {
            using var fileStream = File.Open("state.json", FileMode.OpenOrCreate);

            var exception = Assert.Throws<WatcherException>(_Watcher.MarkRun);
            Assert.Equal("Encountered an unexpected error. " +
                    "Please make sure the daemon is running in the directory containing the executable.",
                    exception.Message);
            _DateTimeProviderMock.Verify(d => d.Today, Times.Once);
            VerifyNoOtherCalls();
        }

        [Fact]
        public void MarkRun_UpdatesStateJson()
        {
            _Watcher.MarkRun();

            var state = JsonSerializer.Deserialize<Watcher.State>(File.ReadAllText("state.json"));
            Assert.Equal(_Today, state?.LastRun);
            _DateTimeProviderMock.Verify(d => d.Today, Times.Once);
            VerifyNoOtherCalls();
        }

        public void Dispose()
        {
            if (File.Exists("state.json"))
            {
                File.Delete("state.json");
            }
        }

        private void CheckCachedState()
        {
            Thread.Sleep(500);
            _Watcher.IsTimeToRun();
            var lastAccessTime = File.GetLastAccessTime("state.json");
            Assert.InRange(lastAccessTime, DateTime.Now.AddSeconds(-1), DateTime.Now.AddMilliseconds(-500));
            _DateTimeProviderMock.Verify(d => d.Today, Times.Exactly(2));
        }

        private static void Write(Watcher.State state)
        {
            File.WriteAllText("state.json", JsonSerializer.Serialize(state));
        }

        private void VerifyNoOtherCalls()
        {
            _DateTimeProviderMock.VerifyNoOtherCalls();
        }
    }
}
