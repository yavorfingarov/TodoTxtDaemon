﻿using System.Globalization;

namespace TodoTxtDaemon
{
    public interface IMover
    {
        void Run();
    }

    public class MoverException : Exception
    {
        public MoverException(string message) : base(message)
        {
        }
    }

    public class Mover : IMover
    {
        private readonly ILogger<Mover> _Logger;

        private readonly IConfiguration _Configuration;

        private readonly DateTimeProvider _DateTimeProvider;

        public Mover(ILogger<Mover> logger, IConfiguration configuration, DateTimeProvider dateTimeProvider)
        {
            _Logger = logger;
            _Configuration = configuration;
            _DateTimeProvider = dateTimeProvider;
        }

        public void Run()
        {
            var todoTxtPath = GetConfigurationValue("TodoTxtPath");
            var doneTxtPath = GetConfigurationValue("DoneTxtPath");
            var tasks = ReadAllLines(todoTxtPath)
                .Select(t => t.Trim());
            var tasksToMove = tasks
                .Where(t => t.StartsWith("x ", StringComparison.InvariantCulture))
                .ToList();
            if (!tasksToMove.Any())
            {
                _Logger.LogNoTasksToMove();

                return;
            }
            var lastWriteTime = GetLastWriteTime(todoTxtPath);
            var timestamp = _DateTimeProvider.Adjust(lastWriteTime).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var doneTasks = tasksToMove
                .Select(t => $"{timestamp} {t[2..].Trim()}")
                .Concat(ReadAllLines(doneTxtPath));
            WriteAllLines(doneTxtPath, doneTasks);
            WriteAllLines(todoTxtPath, tasks.Where(t => !t.StartsWith("x ", StringComparison.InvariantCulture)));
            _Logger.LogMovedTasks(tasksToMove.Count);
        }

        private string GetConfigurationValue(string key)
        {
            var value = _Configuration[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new MoverException($"{key} cannot be empty.");
            }

            return value;
        }

        private static string[] ReadAllLines(string todoTxtPath)
        {
            try
            {
                return File.ReadAllLines(todoTxtPath);
            }
            catch (Exception ex)
            {
                throw new MoverException(ex.Message);
            }
        }

        private static DateTime GetLastWriteTime(string todoTxtPath)
        {
            try
            {
                return File.GetLastWriteTime(todoTxtPath);
            }
            catch (Exception ex)
            {
                throw new MoverException(ex.Message);
            }
        }

        private static void WriteAllLines(string todoTxtPath, IEnumerable<string> contents)
        {
            try
            {
                File.WriteAllLines(todoTxtPath, contents);
            }
            catch (Exception ex)
            {
                throw new MoverException(ex.Message);
            }
        }
    }

    internal static partial class LoggerExtensions
    {
        private static readonly Action<ILogger, Exception?> _NoTasksToMove = LoggerMessage.Define(
            LogLevel.Information, default,
            "No tasks to move.");

        private static readonly Action<ILogger, int, Exception?> _MovedTasks = LoggerMessage.Define<int>(
            LogLevel.Information, default,
            "Moved tasks. Count: {TaskCount}");

        public static void LogNoTasksToMove(this ILogger logger)
        {
            _NoTasksToMove(logger, null);
        }

        public static void LogMovedTasks(this ILogger logger, int taskCount)
        {
            _MovedTasks(logger, taskCount, null);
        }
    }
}
