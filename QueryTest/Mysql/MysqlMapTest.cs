using BaseTest.Utils;
using SharpOrm.Builder;
using static QueryTest.ExpressionProcessorTest;

namespace QueryTest.Mysql
{
    public class MysqlMapTest : SqlMethodMapTest
    {
        public MysqlMapTest() : base(new MysqlQueryConfig())
        {
        }

        [Fact]
        public void DateTimeNow()
        {
            var column = ParseColumn<SampleClass>(x => DateTime.Now).ToExpression(info);

            Assert.Equal("NOW() AS `Now`", column.ToString());
        }

        [Fact]
        public void DateTimeDateOnly()
        {
            var column = ParseColumn<SampleClass>(x => DateTime.Today).ToExpression(info);

            Assert.Equal("CURDATE() AS `Today`", column.ToString());
        }

        [Fact]
        public void DateTimeUtcNow()
        {
            var column = ParseColumn<SampleClass>(x => DateTime.UtcNow).ToExpression(info);

            Assert.Equal("UTC_TIMESTAMP() AS `UtcNow`", column.ToString());
        }

        [Fact]
        public void DateTimeDayOfYear()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.DayOfYear).ToExpression(info);

            Assert.Equal("DAYOFYEAR(`Date`) AS `Date`", column.ToString());
        }

        [Fact]
        public void DateTimeDayOfWeek()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.DayOfWeek).ToExpression(info);

            Assert.Equal("DAYOFWEEK(`Date`) AS `Date`", column.ToString());
        }

        [Fact]
        public void DateTimeDay()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.TimeOfDay).ToExpression(info);

            Assert.Equal("TIME(`Date`) AS `Date`", column.ToString());
        }

        [Fact]
        public void DateTimeMonth()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Month).ToExpression(info);

            Assert.Equal("MONTH(`Date`) AS `Date`", column.ToString());
        }

        [Fact]
        public void DateTimeYear()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Year).ToExpression(info);

            Assert.Equal("YEAR(`Date`) AS `Date`", column.ToString());
        }

        [Fact]
        public void DateTimeHour()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Hour).ToExpression(info);

            Assert.Equal("DATE_FORMAT(`Date`,'%H') AS `Date`", column.ToString());
        }

        [Fact]
        public void DateTimeMinute()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Minute).ToExpression(info);

            Assert.Equal("DATE_FORMAT(`Date`,'%i') AS `Date`", column.ToString());
        }

        [Fact]
        public void DateTimeSecond()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Second).ToExpression(info);

            Assert.Equal("DATE_FORMAT(`Date`,'%s') AS `Date`", column.ToString());
        }

        [Fact]
        public void DateTimeMillisecond()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Millisecond).ToExpression(info);

            Assert.Equal("MICROSECOND(`Date`)/1000 AS `Date`", column.ToString());
        }

        [Fact]
        public void DateTimeTimeOfDay()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.TimeOfDay).ToExpression(info);

            Assert.Equal("TIME(`Date`) AS `Date`", column.ToString());
        }

        [Fact]
        public void DateTimeDate()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Date).ToExpression(info);

            Assert.Equal("DATE(`Date`) AS `Date`", column.ToString());
        }

        [Fact]
        public void Concat()
        {
            string value2 = "value2";
            var column = ParseColumn<SampleClass>(x => string.Concat("Value", value2, x.Name)).ToExpression(info)!;

            Assert.Equal("CONCAT(?,?,`Name`) AS `Concat`", column.ToString());
        }

        [Fact]
        public void Substring()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.Substring(0, 10)).ToExpression(info);

            Assert.Equal("SUBSTRING(`Name`,0,10) AS `Name`", column.ToString());
        }

        [Fact]
        public void SubstringByIndexColumns()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.Substring(x.StartIndex, x.EndIndex)).ToExpression(info);

            Assert.Equal("SUBSTRING(`Name`,`StartIndex`,`EndIndex`) AS `Name`", column.ToString());
        }

        [Fact]
        public void StringTrim()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.Trim()).ToExpression(info);

            Assert.Equal("TRIM(`Name`) AS `Name`", column.ToString());
        }

        [Fact]
        public void StringTrimStart()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.TrimStart()).ToExpression(info);

            Assert.Equal("LTRIM(`Name`) AS `Name`", column.ToString());
        }

        [Fact]
        public void StringTrimEnd()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.TrimEnd()).ToExpression(info);

            Assert.Equal("RTRIM(`Name`) AS `Name`", column.ToString());
        }
    }
}
