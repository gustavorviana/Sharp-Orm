using BaseTest.Utils;
using SharpOrm.Builder;
using static QueryTest.ExpressionProcessorTest;

namespace QueryTest.Sqlite
{
    public class SqliteMapTest : SqlMethodMapTest
    {
        public SqliteMapTest() : base(new SqliteQueryConfig())
        {
        }

        [Fact]
        public void DateTimeNow()
        {
            var column = ParseColumn<SampleClass>(x => DateTime.Now).ToExpression(info);

            Assert.Equal("DATETIME() AS \"Now\"", column.ToString());
        }

        [Fact]
        public void DateTimeDateOnly()
        {
            var column = ParseColumn<SampleClass>(x => DateTime.Today).ToExpression(info);

            Assert.Equal("DATE('now') AS \"Today\"", column.ToString());
        }

        [Fact]
        public void DateTimeUtcNow()
        {
            var column = ParseColumn<SampleClass>(x => DateTime.UtcNow).ToExpression(info);

            Assert.Equal("CURRENT_TIMESTAMP AS \"UtcNow\"", column.ToString());
        }
        [Fact]
        public void DateTimeDayOfYear()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.DayOfYear).ToExpression(info);

            Assert.Equal("STRFTIME('%j',\"Date\") AS \"Date\"", column.ToString());
        }

        [Fact]
        public void DateTimeDayOfWeek()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.DayOfWeek).ToExpression(info);

            Assert.Equal("STRFTIME('%w',\"Date\")+1 AS \"Date\"", column.ToString());
        }

        [Fact]
        public void DateTimeDay()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.TimeOfDay).ToExpression(info);

            Assert.Equal("STRFTIME('%H:%M:%S',\"Date\") AS \"Date\"", column.ToString());
        }

        [Fact]
        public void DateTimeMonth()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Month).ToExpression(info);

            Assert.Equal("STRFTIME('%m',\"Date\") AS \"Date\"", column.ToString());
        }

        [Fact]
        public void DateTimeYear()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Year).ToExpression(info);

            Assert.Equal("STRFTIME('%Y',\"Date\") AS \"Date\"", column.ToString());
        }

        [Fact]
        public void DateTimeHour()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Hour).ToExpression(info);

            Assert.Equal("STRFTIME('%H',\"Date\") AS \"Date\"", column.ToString());
        }

        [Fact]
        public void DateTimeMinute()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Minute).ToExpression(info);

            Assert.Equal("STRFTIME('%M',\"Date\") AS \"Date\"", column.ToString());
        }

        [Fact]
        public void DateTimeSecond()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Second).ToExpression(info);

            Assert.Equal("STRFTIME('%S',\"Date\") AS \"Date\"", column.ToString());
        }

        [Fact]
        public void DateTimeMillisecond()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Millisecond).ToExpression(info);

            Assert.Equal("(STRFTIME('%f',\"Date\")-floor(STRFTIME('%f',\"Date\")))*1000 AS \"Date\"", column.ToString());
        }

        [Fact]
        public void DateTimeTimeOfDay()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.TimeOfDay).ToExpression(info);

            Assert.Equal("STRFTIME('%H:%M:%S',\"Date\") AS \"Date\"", column.ToString());
        }

        [Fact]
        public void DateTimeDate()
        {
            var column = ParseColumn<SampleClass>(x => x.Date.Date).ToExpression(info);

            Assert.Equal("STRFTIME('%Y-%m-%d',\"Date\") AS \"Date\"", column.ToString());
        }

        [Fact]
        public void Concat()
        {
            string value2 = "value2";
            var column = ParseColumn<SampleClass>(x => string.Concat("Value", value2, x.Name)).ToExpression(info)!;

            Assert.Equal("CONCAT(?,?,\"Name\") AS \"Concat\"", column.ToString());
        }

        [Fact]
        public void Substring()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.Substring(0, 10)).ToExpression(info);

            Assert.Equal("SUBSTR(\"Name\",0,10) AS \"Name\"", column.ToString());
        }

        [Fact]
        public void StringTrim()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.Trim()).ToExpression(info);

            Assert.Equal("TRIM(\"Name\") AS \"Name\"", column.ToString());
        }

        [Fact]
        public void StringTrimStart()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.TrimStart()).ToExpression(info);

            Assert.Equal("LTRIM(\"Name\") AS \"Name\"", column.ToString());
        }

        [Fact]
        public void StringTrimEnd()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.TrimEnd()).ToExpression(info);

            Assert.Equal("RTRIM(\"Name\") AS \"Name\"", column.ToString());
        }
    }
}
