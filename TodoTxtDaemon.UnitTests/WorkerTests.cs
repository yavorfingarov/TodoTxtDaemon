using Microsoft.Extensions.Hosting;

namespace TodoTxtDaemon.UnitTests
{
    public sealed class WorkerTests : IDisposable
    {
        private readonly Mock<ILogger<Worker>> _LoggerMock;

        private readonly Mock<IHostApplicationLifetime> _LifetimeMock;

        private readonly Mock<IWatcher> _WatcherMock;

        private readonly Mock<IMover> _MoverMock;

        private readonly Worker _Worker;

        public WorkerTests()
        {
            _LoggerMock = new Mock<ILogger<Worker>>(MockBehavior.Strict);
            _LoggerMock.Setup(LogLevel.Information);
            _LoggerMock.Setup(LogLevel.Error);
            _LoggerMock.Setup(LogLevel.Critical);
            _LifetimeMock = new Mock<IHostApplicationLifetime>(MockBehavior.Strict);
            _LifetimeMock.Setup(l => l.StopApplication());
            _WatcherMock = new Mock<IWatcher>(MockBehavior.Strict);
            _WatcherMock.Setup(w => w.IsTimeToRun()).Returns(true);
            _WatcherMock.Setup(w => w.MarkRun());
            _MoverMock = new Mock<IMover>(MockBehavior.Strict);
            _MoverMock.Setup(m => m.Run());
            _Worker = new Worker(_LoggerMock.Object, _LifetimeMock.Object, _WatcherMock.Object, _MoverMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_DoesNotCallMover_WhenItIsNotTimeToRun()
        {
            _WatcherMock.Setup(w => w.IsTimeToRun()).Returns(false);

            await ExecuteAsync();

            VerifyCommonInvocations();
            VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExecuteAsync_CallsMover_WhenItIsTimeToRun()
        {
            await ExecuteAsync();

            VerifyCommonInvocations(monitoringLogCalls: 2);
            _WatcherMock.Verify(w => w.MarkRun(), Times.Once);
            _MoverMock.Verify(m => m.Run(), Times.Once);
            VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExecuteAsync_Continues_WhenRunThrowsMoverException()
        {
            _MoverMock.Setup(m => m.Run()).Throws(new MoverException("test message"));

            await ExecuteAsync();

            VerifyCommonInvocations(monitoringLogCalls: 2);
            _LoggerMock.Verify(LogLevel.Error, "test message");
            _WatcherMock.Verify(w => w.MarkRun(), Times.Once);
            _MoverMock.Verify(m => m.Run(), Times.Once);
            VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExecuteAsync_Stops_WhenIsTimeToRunThrowsUnhandledException()
        {
            var exception = new Exception("test message");
            _WatcherMock.Setup(m => m.IsTimeToRun()).Throws(exception);

            await ExecuteAsync();

            VerifyCommonInvocations();
            _LoggerMock.Verify(LogLevel.Critical, "Unhandled exception.", exception);
            _LifetimeMock.Verify(l => l.StopApplication(), Times.Once);
            VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExecuteAsync_Stops_WhenMarkRunThrowsUnhandledException()
        {
            var exception = new Exception("test message");
            _WatcherMock.Setup(m => m.MarkRun()).Throws(exception);

            await ExecuteAsync();

            VerifyCommonInvocations();
            _LoggerMock.Verify(LogLevel.Critical, "Unhandled exception.", exception);
            _LifetimeMock.Verify(l => l.StopApplication(), Times.Once);
            _WatcherMock.Verify(w => w.MarkRun(), Times.Once);
            VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ExecuteAsync_Stops_WhenRunThrowsUnhandledException()
        {
            var exception = new Exception("test message");
            _MoverMock.Setup(m => m.Run()).Throws(exception);

            await ExecuteAsync();

            VerifyCommonInvocations();
            _LoggerMock.Verify(LogLevel.Critical, "Unhandled exception.", exception);
            _LifetimeMock.Verify(l => l.StopApplication(), Times.Once);
            _WatcherMock.Verify(w => w.MarkRun(), Times.Once);
            _MoverMock.Verify(m => m.Run(), Times.Once);
            VerifyNoOtherCalls();
        }

        public void Dispose()
        {
            _Worker.Dispose();
        }

        private async Task ExecuteAsync()
        {
            await _Worker.StartAsync(CancellationToken.None);
            await Task.Delay(500);
            await _Worker.StopAsync(CancellationToken.None);
        }

        private void VerifyCommonInvocations(int monitoringLogCalls = 1)
        {
            _LoggerMock.Verify(LogLevel.Information, "Monitoring...", Times.Exactly(monitoringLogCalls));
            var version = typeof(Program).Assembly.GetName().Version?.ToString(3);
            _LoggerMock.Verify(LogLevel.Information, $"TodoTxtDaemon {version} started. Process Id: ");
            _WatcherMock.Verify(w => w.IsTimeToRun(), Times.Once);
        }

        private void VerifyNoOtherCalls()
        {
            _LoggerMock.VerifyNoOtherCalls();
            _LifetimeMock.VerifyNoOtherCalls();
            _WatcherMock.VerifyNoOtherCalls();
            _MoverMock.VerifyNoOtherCalls();
        }
    }
}
