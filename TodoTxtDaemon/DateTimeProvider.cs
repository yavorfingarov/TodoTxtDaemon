namespace TodoTxtDaemon
{
    public class DateTimeProvider
    {
        public virtual DateTime Now => DateTime.Now;

        public virtual DateTime Today => Adjust(Now);

        public virtual DateTime Adjust(DateTime dateTime) => dateTime.AddHours(-3).Date;
    }
}
