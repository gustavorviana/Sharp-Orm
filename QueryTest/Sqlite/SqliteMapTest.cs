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

            Assert.Equal("datetime() AS \"Now\"", column.ToString());
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
