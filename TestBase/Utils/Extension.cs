namespace BaseTest.Utils
{
    public static class Extension
    {
        public static DateTimeOffset RemoveMiliseconds(this DateTimeOffset date)
        {
            return new DateTimeOffset(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Offset);
        }
        public static DateTime RemoveMiliseconds(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second);
        }
    }
}
