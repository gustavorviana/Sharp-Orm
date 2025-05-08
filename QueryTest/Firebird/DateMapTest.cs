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

            Assert.Equal("NOW() AS `Now`", column.ToString(info));
        }

        [Fact]
        public void DateTimeDateOnly()
        {
            var column = ParseColumn<SampleClass>(x => DateTime.Today).ToExpression(info);

            Assert.Equal("CURDATE() AS `Today`", column.ToString(info));
        }

        [Fact]
        public void DateTimeUtcNow()
        {
            var column = ParseColumn<SampleClass>(x => DateTime.UtcNow).ToExpression(info);

            Assert.Equal("UTC_TIMESTAMP() AS `UtcNow`", column.ToString(info));
        }

        [Fact]
        public void DateTimeDayOfYear()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.DayOfYear).ToExpression(info);

            Assert.Equal("DAYOFYEAR(`Date`) AS `Date`", column.ToString(info));
        }

        [Fact]
        public void DateTimeDayOfWeek()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.DayOfWeek).ToExpression(info);

            Assert.Equal("DAYOFWEEK(`Date`) AS `Date`", column.ToString(info));
        }

        [Fact]
        public void DateTimeDay()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.TimeOfDay).ToExpression(info);

            Assert.Equal("TIME(`Date`) AS `Date`", column.ToString(info));
        }

        [Fact]
        public void DateTimeMonth()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Month).ToExpression(info);

            Assert.Equal("MONTH(`Date`) AS `Date`", column.ToString(info));
        }

        [Fact]
        public void DateTimeYear()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Year).ToExpression(info);

            Assert.Equal("YEAR(`Date`) AS `Date`", column.ToString(info));
        }

        [Fact]
        public void DateTimeHour()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Hour).ToExpression(info);

            Assert.Equal("DATE_FORMAT(`Date`,'%H') AS `Date`", column.ToString(info));
        }

        [Fact]
        public void DateTimeMinute()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Minute).ToExpression(info);

            Assert.Equal("DATE_FORMAT(`Date`,'%i') AS `Date`", column.ToString(info));
        }

        [Fact]
        public void DateTimeSecond()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Second).ToExpression(info);

            Assert.Equal("DATE_FORMAT(`Date`,'%s') AS `Date`", column.ToString(info));
        }

        [Fact]
        public void DateTimeMillisecond()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Millisecond).ToExpression(info);

            Assert.Equal("MICROSECOND(`Date`)/1000 AS `Date`", column.ToString(info));
        }

        [Fact]
        public void DateTimeDate()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Date).ToExpression(info);

            Assert.Equal("DATE(`Date`) AS `Date`", column.ToString(info));
        }

        [Fact]
        public void DateTimeTimeOfDay()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.TimeOfDay).ToExpression(info);

            Assert.Equal("TIME(`Date`) AS `Date`", column.ToString(info));
        }

        [Fact]
        public void DateTimeFormat()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.ToString()).LoadDeferred(info, true);

            Assert.Equal("DATE_FORMAT(`Date`,?) AS `Date`", column.ToString(info));
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
