using BaseTest.Utils;
using QueryTest.Interfaces;
using SharpOrm.Builder;
using static QueryTest.ExpressionProcessorTest;

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
            var column = ParseColumn<SampleClass>(x => DateTime.Now).ToExpression(info);

            Assert.Equal("GETDATE() AS [Now]", column.ToString());
        }

        [Fact]
        public void DateTimeDateOnly()
        {
            var column = ParseColumn<SampleClass>(x => DateTime.Today).ToExpression(info);

            Assert.Equal("CAST(GETDATE() AS Date) AS [Today]", column.ToString());
        }

        [Fact]
        public void DateTimeUtcNow()
        {
            var column = ParseColumn<SampleClass>(x => DateTime.UtcNow).ToExpression(info);

            Assert.Equal("GETUTCDATE() AS [UtcNow]", column.ToString());
        }

        [Fact]
        public void DateTimeDayOfYear()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.DayOfYear).ToExpression(info);

            Assert.Equal("DATEPART(DAYOFYEAR,[Date]) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeDayOfWeek()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.DayOfWeek).ToExpression(info);

            Assert.Equal("DATEPART(WEEKDAY,[Date]) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeOfDay()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.TimeOfDay).ToExpression(info);

            Assert.Equal("CAST([Date] AS TIME) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeMonth()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Month).ToExpression(info);

            Assert.Equal("MONTH([Date]) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeYear()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Year).ToExpression(info);

            Assert.Equal("YEAR([Date]) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeHour()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Hour).ToExpression(info);

            Assert.Equal("DATEPART(HOUR,[Date]) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeMinute()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Minute).ToExpression(info);

            Assert.Equal("DATEPART(MINUTE,[Date]) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeSecond()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Second).ToExpression(info);

            Assert.Equal("DATEPART(SECOND,[Date]) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeMillisecond()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Millisecond).ToExpression(info);

            Assert.Equal("DATEPART(MILLISECOND,[Date]) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeDate()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Date).ToExpression(info);

            Assert.Equal("CAST([Date] AS DATE) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeDay()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Day).ToExpression(info);

            Assert.Equal("DAY([Date]) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeTimeOfDay()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.TimeOfDay).ToExpression(info);

            Assert.Equal("CAST([Date] AS TIME) AS [Date]", column.ToString());
        }

        [Fact]
        public void DateTimeFormat()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.ToString()).ToExpression(info);

            Assert.Equal("FORMAT([Date],?) AS [Date]", column.ToString());
            Assert.Single(column.Parameters);

            var format = Assert.IsType<string>(column.Parameters[0]);

            Assert.Equal("yyyy-MM-dd HH:mm:ss", format);
        }

        [Fact]
        public void TimeSpanFormat()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.TimeOfDay.ToString()).ToExpression(info);

            Assert.Equal("FORMAT([Date],?) AS [Date]", column.ToString());
            Assert.Single(column.Parameters);

            var format = Assert.IsType<string>(column.Parameters[0]);
            Assert.Equal("HH:mm:ss", format);
        }
    }
}
