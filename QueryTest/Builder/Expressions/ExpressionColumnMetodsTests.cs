using BaseTest.Models;
using BaseTest.Utils;
using QueryTest.Utils;
using SharpOrm.Builder;
using static QueryTest.Builder.Expressions.ExpressionProcessorTests;

namespace QueryTest.Builder.Expressions
{
    public class ExpressionColumnMetodsTests : SqlMethodMapTest
    {
        private readonly int TestLength = 1;

        public ExpressionColumnMetodsTests() : base(new SqlServerQueryConfig())
        {

        }

        [Fact]
        public void ParseColumnWithNotEqualsPropertyNameTest()
        {
            var column = ParseColumn<Customer>(x => new { x.AddressId });
            QueryAssert.Equal(info, "[address_id]", column.ToExpression(info));
        }

        [Fact]
        public void ParseColumnTest()
        {
            var column = ParseColumn<Customer>(x => x.Id);

            QueryAssert.Equal(info, "[Id]", column.ToExpression(info));
        }

        [Fact]
        public void ParseColumnWithAliasTest()
        {
            var column = ParseColumn<Customer>(x => new { CliId = x.Id });

            QueryAssert.Equal(info, "[Id] AS [CliId]", column.ToExpression(info));
        }

        [Fact]
        public void ParseColumn_NameLength_ReturnsCorrectExpression()
        {
            var column = ParseColumn<Customer>(x => x.Name.Length);

            QueryAssert.Equal(info, "LEN([Name]) AS [Name]", column.ToExpression(info));
        }

        [Fact]
        public void ParseColumnWithAlias_NameLength_ReturnsCorrectExpressionWithAlias()
        {
            var column = ParseColumn<Customer>(x => new { CliId = x.Name.Length });

            QueryAssert.Equal(info, "LEN([Name]) AS [CliId]", column.ToExpression(info));
        }

        [Fact]
        public void ConcatStringAndHours()
        {
            var column = ParseColumns<SampleClass>(x => new { Name = string.Concat(x.Name, x.Date.TimeOfDay.Hours, TestLength) }).First();
            var exp = column.ToExpression(info);

            QueryAssert.Equal(info, "CONCAT([Name],DATEPART(HOUR,CAST([Date] AS TIME)),1) AS [Name]", exp);
        }

        [Fact]
        public void ParseSingleColumn()
        {
            var column = ParseColumns<SampleClass>(x => x.Name).First();
            var exp = column.ToExpression(info);

            QueryAssert.Equal(info, "[Name]", exp);
        }

        [Fact]
        public void StrToLower()
        {
            var column = ParseColumns<SampleClass>(x => new { Name2 = x.Name!.ToLower() }).First();
            var exp = column.ToExpression(info);

            QueryAssert.Equal(info, "LOWER([Name]) AS [Name2]", exp);
        }

        [Fact]
        public void StrToLowerWithoutAlias()
        {
            var column = ParseColumns<SampleClass>(x => new { Namee = x.Name!.ToLower() }).First();
            var exp = column.ToExpression(info, false);

            QueryAssert.Equal(info, "LOWER([Name])", exp);
        }

        [Fact]
        public void StrToUpper()
        {
            var column = ParseColumns<SampleClass>(x => x.Name!.ToUpper()).First();
            var exp = column.ToExpression(info);

            QueryAssert.Equal(info, "UPPER([Name]) AS [Name]", exp);
        }
    }
}
