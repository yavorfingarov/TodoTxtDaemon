namespace TodoTxtDaemon.UnitTests
{
    public sealed class MoverTests : IDisposable
    {
        private readonly Mock<ILogger<Mover>> _LoggerMock;

        private readonly Mock<IConfiguration> _ConfigurationMock;

        private readonly Mock<DateTimeProvider> _DateTimeProviderMock;

        private readonly Mover _Mover;

        private readonly HashSet<string> _FilePaths;

        private readonly DateTime _Today;

        public MoverTests()
        {
            _LoggerMock = new Mock<ILogger<Mover>>(MockBehavior.Strict);
            _ConfigurationMock = new Mock<IConfiguration>(MockBehavior.Strict);
            _DateTimeProviderMock = new Mock<DateTimeProvider>(MockBehavior.Strict);
            _Mover = new Mover(_LoggerMock.Object, _ConfigurationMock.Object, _DateTimeProviderMock.Object);
            _FilePaths = new HashSet<string>();
            _LoggerMock.Setup(LogLevel.Information);
            _ConfigurationMock.Setup(c => c["TodoTxtPath"]).Returns("todo.txt");
            _ConfigurationMock.Setup(c => c["DoneTxtPath"]).Returns("done.txt");
            _Today = DateTime.Today;
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Run_Throws_WhenTodoTxtPathIsEmpty(string path)
        {
            _ConfigurationMock.Setup(c => c["TodoTxtPath"]).Returns(path);

            var exception = Assert.Throws<MoverException>(_Mover.Run);
            Assert.Equal("TodoTxtPath cannot be empty.", exception.Message);
            _ConfigurationMock.Verify("TodoTxtPath");
            VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Run_Throws_WhenDoneTxtPathIsEmpty(string path)
        {
            _ConfigurationMock.Setup(c => c["DoneTxtPath"]).Returns(path);

            var exception = Assert.Throws<MoverException>(_Mover.Run);
            Assert.Equal("DoneTxtPath cannot be empty.", exception.Message);
            _ConfigurationMock.Verify("TodoTxtPath", "DoneTxtPath");
            VerifyNoOtherCalls();
        }

        [Fact]
        public void Run_Throws_WhenTodoTxtDoesNotExist()
        {
            var exception = Assert.Throws<MoverException>(_Mover.Run);
            Assert.Contains("todo.txt", exception.Message);
            _ConfigurationMock.Verify("TodoTxtPath", "DoneTxtPath");
            VerifyNoOtherCalls();
        }

        [Fact]
        public void Run_Throws_WhenDoneTxtDoesNotExist()
        {
            var tasks = new[] { "task 1", "x task 2", "task 3", "  x task 4  ", "task x test", "task 5" };
            Write("todo.txt", tasks, _Today.AddDays(-2));
            _DateTimeProviderMock.Setup(d => d.Adjust(_Today.AddDays(-2))).Returns(_Today.AddDays(-4));

            var exception = Assert.Throws<MoverException>(_Mover.Run);
            Assert.Contains("done.txt", exception.Message);
            _DateTimeProviderMock.Verify(d => d.Adjust(It.IsAny<DateTime>()), Times.Once);
            _ConfigurationMock.Verify("TodoTxtPath", "DoneTxtPath");
            VerifyNoOtherCalls();
        }

        [Fact]
        public void Run_DoesNothing_WhenThereIsNothingToMove()
        {
            var tasks = new[] { "task 1", "task 2", "task 3", "task 4", "task x test", "task 5" };
            var lastWriteTime = _Today.AddDays(-3);
            Write("todo.txt", tasks, lastWriteTime);
            Write("done.txt", Enumerable.Empty<string>(), lastWriteTime);

            _Mover.Run();

            Assert.Equal(lastWriteTime, File.GetLastWriteTime("todo.txt"));
            Assert.Equal(tasks, File.ReadAllLines("todo.txt"));
            Assert.Equal(lastWriteTime, File.GetLastWriteTime("done.txt"));
            Assert.Equal(Enumerable.Empty<string>(), File.ReadAllLines("done.txt"));
            _ConfigurationMock.Verify("TodoTxtPath", "DoneTxtPath");
            _LoggerMock.Verify(LogLevel.Information, "No tasks to move.");
            VerifyNoOtherCalls();
        }

        [Fact]
        public void Run_Throws_WhenDoneTxtIsLocked()
        {
            var tasks = new[] { "task 1", "x task 2", "task 3", "  x task 4  ", "task x test", "task 5" };
            var todoTxtLastWriteTime = _Today.Date.AddDays(-3).AddHours(19);
            var doneTxtLastWriteTime = todoTxtLastWriteTime.AddDays(-1);
            Write("todo.txt", tasks, todoTxtLastWriteTime);
            Write("done.txt", Enumerable.Empty<string>(), doneTxtLastWriteTime);
            _DateTimeProviderMock.Setup(d => d.Adjust(todoTxtLastWriteTime)).Returns(_Today.AddDays(-2));
            using var fileStream = File.Open("done.txt", FileMode.OpenOrCreate);

            var exception = Assert.Throws<MoverException>(_Mover.Run);
            Assert.Contains("done.txt", exception.Message);
            Assert.Equal(todoTxtLastWriteTime, File.GetLastWriteTime("todo.txt"));
            Assert.Equal(tasks, File.ReadAllLines("todo.txt"));
            Assert.Equal(doneTxtLastWriteTime, File.GetLastWriteTime("done.txt"));
            _DateTimeProviderMock.Verify(d => d.Adjust(It.IsAny<DateTime>()), Times.Once);
            _ConfigurationMock.Verify("TodoTxtPath", "DoneTxtPath");
            VerifyNoOtherCalls();
        }

        [Fact]
        public void Run_MovesTasks_WhenDoneTxtIsEmpty()
        {
            var tasks = new[] { "task 1", "x task 2", "task 3", "  x task 4  ", "task x test", "task 5" };
            var todoTxtLastWriteTime = _Today.Date.AddDays(-3).AddHours(19);
            var doneTxtLastWriteTime = todoTxtLastWriteTime.AddDays(-1);
            Write("todo.txt", tasks, todoTxtLastWriteTime);
            Write("done.txt", Enumerable.Empty<string>(), doneTxtLastWriteTime);
            _DateTimeProviderMock.Setup(d => d.Adjust(todoTxtLastWriteTime)).Returns(_Today.AddDays(-2));

            _Mover.Run();

            Assert.True(DateTime.Now - File.GetLastWriteTime("todo.txt") < TimeSpan.FromSeconds(2));
            Assert.Equal(new[] { "task 1", "task 3", "task x test", "task 5" }, File.ReadAllLines("todo.txt"));
            Assert.True(DateTime.Now - File.GetLastWriteTime("done.txt") < TimeSpan.FromSeconds(2));
            var timestamp = _Today.AddDays(-2).ToString("yyyy-MM-dd");
            Assert.Equal(new[] { $"{timestamp} task 2", $"{timestamp} task 4" }, File.ReadAllLines("done.txt"));
            _DateTimeProviderMock.Verify(d => d.Adjust(It.IsAny<DateTime>()), Times.Once);
            _ConfigurationMock.Verify("TodoTxtPath", "DoneTxtPath");
            _LoggerMock.Verify(LogLevel.Information, "Moved 2 tasks.");
            VerifyNoOtherCalls();
        }

        [Fact]
        public void Run_MovesTasks_WhenDoneTxtIsNotEmpty()
        {
            var tasks = new[] { "task 1", "x task 2", "task 3", "  x task 4  ", "task x test", "task 5" };
            var todoTxtLastWriteTime = _Today.AddDays(-3).AddHours(19);
            var doneTxtLastWriteTime = todoTxtLastWriteTime.AddDays(-1);
            var oldTimestamp = doneTxtLastWriteTime.ToString("yyyy-MM-dd");
            Write("todo.txt", tasks, todoTxtLastWriteTime);
            Write("done.txt", new[] { $"{oldTimestamp} task 9", $"{oldTimestamp} task 7" }, doneTxtLastWriteTime);
            _DateTimeProviderMock.Setup(d => d.Adjust(todoTxtLastWriteTime)).Returns(_Today.AddDays(-2));

            _Mover.Run();

            Assert.True(DateTime.Now - File.GetLastWriteTime("todo.txt") < TimeSpan.FromSeconds(2));
            Assert.Equal(new[] { "task 1", "task 3", "task x test", "task 5" }, File.ReadAllLines("todo.txt"));
            Assert.True(DateTime.Now - File.GetLastWriteTime("done.txt") < TimeSpan.FromSeconds(2));
            var newTimestamp = _Today.AddDays(-2).ToString("yyyy-MM-dd");
            Assert.Equal(new[] { $"{newTimestamp} task 2", $"{newTimestamp} task 4", $"{oldTimestamp} task 9", $"{oldTimestamp} task 7" },
                File.ReadAllLines("done.txt"));
            _DateTimeProviderMock.Verify(d => d.Adjust(It.IsAny<DateTime>()), Times.Once);
            _ConfigurationMock.Verify("TodoTxtPath", "DoneTxtPath");
            _LoggerMock.Verify(LogLevel.Information, "Moved 2 tasks.");
            VerifyNoOtherCalls();
        }

        public void Dispose()
        {
            foreach (var filePath in _FilePaths)
            {
                File.Delete(filePath);
            }
        }

        private void Write(string filePath, IEnumerable<string> contents, DateTime lastWriteTime)
        {
            File.WriteAllLines(filePath, contents);
            File.SetLastWriteTime(filePath, lastWriteTime);
            _FilePaths.Add(filePath);
        }

        private void VerifyNoOtherCalls()
        {
            _LoggerMock.VerifyNoOtherCalls();
            _ConfigurationMock.VerifyNoOtherCalls();
            _DateTimeProviderMock.VerifyNoOtherCalls();
        }
    }
}
