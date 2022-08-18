using System.Text.Json;
using Xunit;

namespace TodoTxtDaemon.IntegrationTests
{
    public sealed class ProgramTests : IDisposable
    {
        private readonly string _InitialAppSettingsIni;

        private Task? _Main;

        public ProgramTests()
        {
            _InitialAppSettingsIni = File.ReadAllText("appsettings.ini");
        }

        [Fact]
        public void Main_Runs_WithInitialState()
        {
            RunMain();

            AssertLogAndStatus(TaskStatus.Running);
        }

        [Fact]
        public void Main_MovesTasks_WithInitialState()
        {
            File.WriteAllLines("appsettings.ini", new[] { "TodoTxtPath=todo.txt", "DoneTxtPath=done.txt" });
            File.WriteAllLines("todo.txt", new[] { "task 1", "x task 2", "x task 3", "task 4" });
            File.WriteAllLines("done.txt", new[] { "0000-00-00 task 5", "0000-00-00 task 6" });

            RunMain();

            AssertLogAndStatus(TaskStatus.Running);
            Assert.Equal(new[] { "task 1", "task 4" }, File.ReadAllLines("todo.txt"));
            var doneTasks = File.ReadAllLines("done.txt");
            Assert.Equal(4, doneTasks.Length);
            Assert.Matches(@"^[0-9]{4}-[0-9]{2}-[0-9]{2} task 2$", doneTasks[0]);
            Assert.Matches(@"^[0-9]{4}-[0-9]{2}-[0-9]{2} task 3$", doneTasks[1]);
            Assert.Equal("0000-00-00 task 5", doneTasks[2]);
            Assert.Equal("0000-00-00 task 6", doneTasks[3]);
        }

        [Fact]
        public void Main_MovesTasks_WhenLastRunIsThreeDaysAgo()
        {
            File.WriteAllLines("appsettings.ini", new[] { "TodoTxtPath=todo.txt", "DoneTxtPath=done.txt" });
            File.WriteAllText("state.json", JsonSerializer.Serialize(new Watcher.State(DateTime.Today.AddDays(-3))));
            File.WriteAllLines("todo.txt", new[] { "task 1", "x task 2", "x task 3", "task 4" });
            File.WriteAllLines("done.txt", new[] { "0000-00-00 task 5", "0000-00-00 task 6" });

            RunMain();

            AssertLogAndStatus(TaskStatus.Running);
            Assert.Equal(new[] { "task 1", "task 4" }, File.ReadAllLines("todo.txt"));
            var doneTasks = File.ReadAllLines("done.txt");
            Assert.Equal(4, doneTasks.Length);
            Assert.Matches(@"^[0-9]{4}-[0-9]{2}-[0-9]{2} task 2$", doneTasks[0]);
            Assert.Matches(@"^[0-9]{4}-[0-9]{2}-[0-9]{2} task 3$", doneTasks[1]);
            Assert.Equal("0000-00-00 task 5", doneTasks[2]);
            Assert.Equal("0000-00-00 task 6", doneTasks[3]);
        }

        [Fact]
        public void Main_DoesNotMoveTasks_WhenLastRunIsToday()
        {
            File.WriteAllLines("appsettings.ini", new[] { "TodoTxtPath=todo.txt", "DoneTxtPath=done.txt" });
            File.WriteAllText("state.json", JsonSerializer.Serialize(new Watcher.State(DateTime.Today)));
            var todoTasks = new[] { "task 1", "x task 2", "x task 3", "task 4" };
            var doneTasks = new[] { "0000-00-00 task 5", "0000-00-00 task 6" };
            File.WriteAllLines("todo.txt", todoTasks);
            File.WriteAllLines("done.txt", doneTasks);

            RunMain();

            AssertLogAndStatus(TaskStatus.Running);
            Assert.Equal(todoTasks, File.ReadAllLines("todo.txt"));
            Assert.Equal(doneTasks, File.ReadAllLines("done.txt"));
        }

        [Fact]
        public void Main_Stops_WhenStateJsonIsLocked()
        {
            using var fileStream = File.Open("state.json", FileMode.OpenOrCreate);

            RunMain();

            AssertLogAndStatus(TaskStatus.RanToCompletion);
        }

        public void Dispose()
        {
            File.WriteAllText("appsettings.ini", _InitialAppSettingsIni);
            foreach (var filePath in new[] { "todo.txt", "done.txt", "app.log", "state.json" })
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        private void RunMain()
        {
            _Main = Task.Run(Program.Main);
            Thread.Sleep(2000);
        }

        private void AssertLogAndStatus(TaskStatus taskStatus)
        {
            Assert.Equal(taskStatus, _Main?.Status);
            Assert.True(File.Exists("app.log"));
            Assert.NotEqual(0, new FileInfo("app.log").Length);
        }
    }
}
