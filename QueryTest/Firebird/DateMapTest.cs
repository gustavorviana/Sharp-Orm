using BaseTest.Utils;
using QueryTest.Interfaces;
using QueryTest.Utils;
using SharpOrm.Fb;
using static QueryTest.Builder.Expressions.ExpressionProcessorTests;

namespace QueryTest.Firebird
{
    public class DateMapTest : SqlMethodMapTest, ISqlDateMapTest
    {
        public DateMapTest() : base(new FbQueryConfig())
        {
        }

        [Fact]
        public void DateTimeNow()
        {
            var column = ParseColumn<SampleClass>(x => DateTime.Now).ToExpression(info);

            Assert.Equal("CURRENT_TIMESTAMP AS Now", column.ToString(info));
        }

        [Fact]
        public void DateTimeDateOnly()
        {
            var column = ParseColumn<SampleClass>(x => DateTime.Today).ToExpression(info);

            Assert.Equal("CURRENT_DATE AS Today", column.ToString(info));
        }

        [Fact]
        public void DateTimeUtcNow()
        {
            var column = ParseColumn<SampleClass>(x => DateTime.UtcNow).ToExpression(info);

            Assert.Equal("CURRENT_TIMESTAMP AS UtcNow", column.ToString(info));
        }

        [Fact]
        public void DateTimeDayOfYear()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.DayOfYear).ToExpression(info);

            Assert.Equal("EXTRACT(YEARDAY FROM Date) AS Date", column.ToString(info));
        }

        [Fact]
        public void DateTimeDayOfWeek()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.DayOfWeek).ToExpression(info);

            Assert.Equal("EXTRACT(WEEKDAY FROM Date) AS Date", column.ToString(info));
        }

        [Fact]
        public void DateTimeDay()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.TimeOfDay).ToExpression(info);

            Assert.Equal("CAST(Date AS TIME) AS Date", column.ToString(info));
        }

        [Fact]
        public void DateTimeMonth()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Month).ToExpression(info);

            Assert.Equal("EXTRACT(MONTH FROM Date) AS Date", column.ToString(info));
        }

        [Fact]
        public void DateTimeYear()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Year).ToExpression(info);

            Assert.Equal("EXTRACT(YEAR FROM Date) AS Date", column.ToString(info));
        }

        [Fact]
        public void DateTimeHour()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Hour).ToExpression(info);

            Assert.Equal("EXTRACT(HOUR FROM Date) AS Date", column.ToString(info));
        }

        [Fact]
        public void DateTimeMinute()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Minute).ToExpression(info);

            Assert.Equal("EXTRACT(MINUTE FROM Date) AS Date", column.ToString(info));
        }

        [Fact]
        public void DateTimeSecond()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Second).ToExpression(info);

            Assert.Equal("EXTRACT(SECOND FROM Date) AS Date", column.ToString(info));
        }

        [Fact]
        public void DateTimeMillisecond()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Millisecond).ToExpression(info);

            Assert.Equal("EXTRACT(MILLISECOND FROM Date) AS Date", column.ToString(info));
        }

        [Fact]
        public void DateTimeDate()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Date).ToExpression(info);

            Assert.Equal("CAST(Date AS DATE) AS Date", column.ToString(info));
        }

        [Fact]
        public void DateTimeTimeOfDay()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.TimeOfDay).ToExpression(info);

            Assert.Equal("CAST(Date AS TIME) AS Date", column.ToString(info));
        }

        [Fact]
        public void DateTimeFormat()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.ToString()).LoadDeferred(info, true);

            Assert.Equal("EXTRACT(YEAR FROEXTRACT(MONTEXTRACT(HOUR FROM Date) FROM Date)", column.ToString(info));
            Assert.Single(column.Parameters);

            var format = Assert.IsType<string>(column.Parameters[0]);

            Assert.Equal("%Y-%m-%d %H:%i:%s", format);
        }

        [Fact]
        public void TimeSpanFormat()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.TimeOfDay.ToString()).LoadDeferred(info, true);

            Assert.Equal("DATE_FORMAT(`Date`,?) AS `Date`", column.ToString(info));
            Assert.Single(column.Parameters);

            var format = Assert.IsType<string>(column.Parameters[0]);
            Assert.Equal("%H:%i:%s", format);
        }
    }
}
