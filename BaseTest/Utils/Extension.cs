using SharpOrm;

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

        public static DateTime SetKind(this DateTime date, DateTimeKind kind)
        {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, kind);
        }

        public static Row Union(this Row row, Row row2)
        {
            var cells = new List<Cell>();

            cells.AddRange(row.Cells);
            cells.AddRange(row2.Cells);

            return new Row(cells);
        }
    }
}
