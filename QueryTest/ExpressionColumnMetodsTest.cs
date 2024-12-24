using BaseTest.Utils;
using SharpOrm.Builder;
using static QueryTest.ExpressionProcessorTest;

namespace QueryTest
{
    public class ExpressionColumnMetodsTest : SqlMethodMapTest
    {
        private readonly int TestLength = 1;

        public ExpressionColumnMetodsTest() : base(new SqlServerQueryConfig())
        {

        }

        [Fact]
        public void ConcatStringAndHours()
        {
            var column = ParseColumns<SampleClass>(x => new { Name = string.Concat(x.Name, x.Date.TimeOfDay.Hours, TestLength) }).First();
            var exp = column.ToExpression(info);

            Assert.Equal("CONCAT([Name],DATEPART(HOUR,CAST([Date] AS TIME)),1) AS [Name]", exp.ToString());
        }

        [Fact]
        public void ParseSingleColumn()
        {
            var column = ParseColumns<SampleClass>(x => x.Name).First();
            var exp = column.ToExpression(info);

            Assert.Equal("[Name]", exp.ToString());
        }

        [Fact]
        public void StrToLower()
        {
            var column = ParseColumns<SampleClass>(x => new { Name2 = x.Name!.ToLower() }).First();
            var exp = column.ToExpression(info);

            Assert.Equal("LOWER([Name]) AS [Name2]", exp.ToString());
        }

        [Fact]
        public void StrToLowerWithoutAlias()
        {
            var column = ParseColumns<SampleClass>(x => new { Namee = x.Name!.ToLower() }).First();
            var exp = column.ToExpression(info, false);

            Assert.Equal("LOWER([Name])", exp.ToString());
        }

        [Fact]
        public void StrToUpper()
        {
            var column = ParseColumns<SampleClass>(x => x.Name!.ToUpper()).First();
            var exp = column.ToExpression(info);

            Assert.Equal("UPPER([Name]) AS [Name]", exp.ToString());
        }
    }
}
