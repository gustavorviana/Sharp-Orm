using BaseTest.Utils;
using QueryTest.Interfaces;
using QueryTest.Utils;
using SharpOrm.Builder;
using static QueryTest.Builder.Expressions.ExpressionProcessorTests;

namespace QueryTest.SqlServer
{
    public class DateMapTest : SqlMethodMapTest, ISqlDateMapTest
    {
        public DateMapTest() : base(new SqlServerQueryConfig())
        {
        }

        [Fact]
        public void DateTimeNow()
        {
            var column = ParseColumn<SampleClass>(x => DateTime.Now).LoadDeferred(info, true);

            Assert.Equal("GETDATE() AS [Now]", column.ToString());
        }

        [Fact]
        public void DateTimeDateOnly()
        {
            var column = ParseColumn<SampleClass>(x => DateTime.Today).LoadDeferred(info, true);

            Assert.Equal("CAST(GETDATE() AS Date) AS [Today]", column.ToString());
        }

        [Fact]
        public void DateTimeUtcNow()
        {
            var column = ParseColumn<SampleClass>(x => DateTime.UtcNow).LoadDeferred(info, true);

            Assert.Equal("GETUTCDATE() AS [UtcNow]", column.ToString());
        }

        [Fact]
        public void DateTimeDayOfYear()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.DayOfYear).LoadDeferred(info, true);

            Assert.Equal("DATEPART(DAYOFYEAR,[Date]) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeDayOfWeek()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.DayOfWeek).LoadDeferred(info, true);

            Assert.Equal("DATEPART(WEEKDAY,[Date]) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeOfDay()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.TimeOfDay).LoadDeferred(info, true);

            Assert.Equal("CAST([Date] AS TIME) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeMonth()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Month).LoadDeferred(info, true);

            Assert.Equal("MONTH([Date]) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeYear()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Year).LoadDeferred(info, true);

            Assert.Equal("YEAR([Date]) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeHour()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Hour).LoadDeferred(info, true);

            Assert.Equal("DATEPART(HOUR,[Date]) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeMinute()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Minute).LoadDeferred(info, true);

            Assert.Equal("DATEPART(MINUTE,[Date]) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeSecond()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Second).LoadDeferred(info, true);

            Assert.Equal("DATEPART(SECOND,[Date]) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeMillisecond()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Millisecond).LoadDeferred(info, true);

            Assert.Equal("DATEPART(MILLISECOND,[Date]) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeDate()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Date).LoadDeferred(info, true);

            Assert.Equal("CAST([Date] AS DATE) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeDay()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Day).LoadDeferred(info, true);

            Assert.Equal("DAY([Date]) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeTimeOfDay()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.TimeOfDay).LoadDeferred(info, true);

            Assert.Equal("CAST([Date] AS TIME) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeFormat()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.ToString()).LoadDeferred(info, true);

            Assert.Equal("FORMAT([Date],?) AS [Date]", column.ToString());
            Assert.Single(column.Parameters);

            var format = Assert.IsType<string>(column.Parameters[0]);

            Assert.Equal("yyyy-MM-dd HH:mm:ss", format);
        }

        [Fact]
        public void TimeSpanFormat()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.TimeOfDay.ToString()).LoadDeferred(info, true);

            Assert.Equal("FORMAT([Date],?) AS [Date]", column.ToString());
            Assert.Single(column.Parameters);

            var format = Assert.IsType<string>(column.Parameters[0]);
            Assert.Equal("HH:mm:ss", format);
        }
    }
}
