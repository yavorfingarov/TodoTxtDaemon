namespace TodoTxtDaemon.UnitTests
{
    public class DateTimeProviderTests
    {
        [Fact]
        public void Now_ReturnsCurrentTime()
        {
            var dateTimeProvider = new DateTimeProvider();

            Assert.InRange(dateTimeProvider.Now, DateTime.Now.AddMilliseconds(-50), DateTime.Now.AddMilliseconds(50));
            Thread.Sleep(500);
            Assert.InRange(dateTimeProvider.Now, DateTime.Now.AddMilliseconds(-50), DateTime.Now.AddMilliseconds(50));
        }

        [Fact]
        public void Today_ReturnsAdjustedDate()
        {
            var time = DateTime.Today.AddDays(-4);
            var dateTimeProviderMock = new Mock<DateTimeProvider>(MockBehavior.Strict);
            dateTimeProviderMock.Setup(d => d.Now).Returns(time);
            dateTimeProviderMock.Setup(d => d.Adjust(time)).Returns(time.AddDays(2));
            dateTimeProviderMock.Setup(d => d.Today).CallBase();

            Assert.Equal(time.AddDays(2), dateTimeProviderMock.Object.Today);
            dateTimeProviderMock.Verify(d => d.Today, Times.Once);
            dateTimeProviderMock.Verify(d => d.Now, Times.Once);
            dateTimeProviderMock.Verify(d => d.Adjust(It.IsAny<DateTime>()), Times.Once);
            dateTimeProviderMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(-36, -2)]
        [InlineData(-22, -2)]
        [InlineData(-12, -1)]
        [InlineData(-6, -1)]
        [InlineData(0, -1)]
        [InlineData(1, -1)]
        [InlineData(2, -1)]
        [InlineData(3, 0)]
        [InlineData(8, 0)]
        [InlineData(16, 0)]
        [InlineData(26, 0)]
        [InlineData(27, 1)]
        [InlineData(36, 1)]
        public void Adjust_ReturnsAdjustedDate(int todayHourOffset, int expectedDayOffset)
        {
            var today = DateTime.Today;
            var dateTimeProvider = new DateTimeProvider();

            var result = dateTimeProvider.Adjust(today.AddHours(todayHourOffset));

            Assert.Equal(today.AddDays(expectedDayOffset), result);
        }
    }
}
