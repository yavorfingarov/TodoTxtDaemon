namespace TodoTxtDaemon.UnitTests
{
    public static class ExtensionMethods
    {
        public static void Setup<T>(this Mock<ILogger<T>> logger, LogLevel logLevel)
        {
            logger.Setup(l => l.Log(logLevel, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
        }

        public static void Verify<T>(this Mock<ILogger<T>> logger, LogLevel logLevel, string messagePart,
            Exception? exception = null)
        {
            logger.Verify(l => l.Log(logLevel, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(messagePart)),
                exception, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        public static void Verify<T>(this Mock<ILogger<T>> logger, LogLevel logLevel, string messagePart, Times times)
        {
            logger.Verify(l => l.Log(logLevel, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(messagePart)),
                null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), times);
        }

        public static void Verify(this Mock<IConfiguration> configurationMock, params string[] keys)
        {
            foreach (var key in keys)
            {
                configurationMock.Verify(c => c[key], Times.Once);
            }
        }
    }
}
