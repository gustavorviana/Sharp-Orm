using BaseTest.Utils;
using QueryTest.Interfaces;
using QueryTest.Utils;
using SharpOrm.Builder;
using static QueryTest.ExpressionProcessorTest;

namespace QueryTest.Sqlite
{
    public class DateMapTest : SqlMethodMapTest, ISqlDateMapTest
    {
        public DateMapTest() : base(new SqliteQueryConfig())
        {
        }

        [Fact]
        public void DateTimeNow()
        {
            var column = ParseColumn<SampleClass>(x => DateTime.Now).LoadDeferred(info, true);

            Assert.Equal("DATETIME() AS \"Now\"", column.ToString());
        }

        [Fact]
        public void DateTimeDateOnly()
        {
            var column = ParseColumn<SampleClass>(x => DateTime.Today).LoadDeferred(info, true);

            Assert.Equal("DATE('now') AS \"Today\"", column.ToString());
        }

        [Fact]
        public void DateTimeUtcNow()
        {
            var column = ParseColumn<SampleClass>(x => DateTime.UtcNow).LoadDeferred(info, true);

            Assert.Equal("CURRENT_TIMESTAMP AS \"UtcNow\"", column.ToString());
        }
        [Fact]
        public void DateTimeDayOfYear()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.DayOfYear).LoadDeferred(info, true);

            Assert.Equal("STRFTIME('%j',\"Date\") AS \"Date\"", column.ToString());
        }

        [Fact]
        public void DateTimeDayOfWeek()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.DayOfWeek).LoadDeferred(info, true);

            Assert.Equal("STRFTIME('%w',\"Date\")+1 AS \"Date\"", column.ToString());
        }

        [Fact]
        public void DateTimeDay()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.TimeOfDay).LoadDeferred(info, true);

            Assert.Equal("STRFTIME('%H:%M:%S',\"Date\") AS \"Date\"", column.ToString());
        }

        [Fact]
        public void DateTimeMonth()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Month).LoadDeferred(info, true);

            Assert.Equal("STRFTIME('%m',\"Date\") AS \"Date\"", column.ToString());
        }

        [Fact]
        public void DateTimeYear()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Year).LoadDeferred(info, true);

            Assert.Equal("STRFTIME('%Y',\"Date\") AS \"Date\"", column.ToString());
        }

        [Fact]
        public void DateTimeHour()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Hour).LoadDeferred(info, true);

            Assert.Equal("STRFTIME('%H',\"Date\") AS \"Date\"", column.ToString());
        }

        [Fact]
        public void DateTimeMinute()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Minute).LoadDeferred(info, true);

            Assert.Equal("STRFTIME('%M',\"Date\") AS \"Date\"", column.ToString());
        }

        [Fact]
        public void DateTimeSecond()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Second).LoadDeferred(info, true);

            Assert.Equal("STRFTIME('%S',\"Date\") AS \"Date\"", column.ToString());
        }

        [Fact]
        public void DateTimeMillisecond()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Millisecond).LoadDeferred(info, true);

            Assert.Equal("(STRFTIME('%f',\"Date\")-floor(STRFTIME('%f',\"Date\")))*1000 AS \"Date\"", column.ToString());
        }

        [Fact]
        public void DateTimeDate()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Date).LoadDeferred(info, true);

            Assert.Equal("STRFTIME('%Y-%m-%d',\"Date\") AS \"Date\"", column.ToString());
        }

        [Fact]
        public void DateTimeTimeOfDay()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.TimeOfDay).LoadDeferred(info, true);

            Assert.Equal("STRFTIME('%H:%M:%S',\"Date\") AS \"Date\"", column.ToString());
        }

        [Fact]
        public void DateTimeFormat()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.ToString()).LoadDeferred(info, true);

            Assert.Equal("strftime(?, \"Date\") AS \"Date\"", column.ToString());
            Assert.Single(column.Parameters);

            var format = Assert.IsType<string>(column.Parameters[0]);

            Assert.Equal("%Y-%m-%d %H:%M:%S", format);
        }

        [Fact]
        public void TimeSpanFormat()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.TimeOfDay.ToString()).LoadDeferred(info, true);

            Assert.Equal("strftime(?, \"Date\") AS \"Date\"", column.ToString());
            Assert.Single(column.Parameters);

            var format = Assert.IsType<string>(column.Parameters[0]);
            Assert.Equal("%H:%M:%S", format);
        }
    }
}
