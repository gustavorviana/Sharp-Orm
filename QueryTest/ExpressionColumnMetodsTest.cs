using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
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
        public void ParseColumnTest()
        {
            var column = Column.Parse<Customer>(x => x.Id, info);

            Assert.Equal("[Id]", column.ToExpression(info).ToString());
        }

        [Fact]
        public void ParseColumnWithAliasTest()
        {
            var column = Column.Parse<Customer>(x => new { CliId = x.Id }, info);

            Assert.Equal("[Id] AS [CliId]", column.ToExpression(info).ToString());
        }

        [Fact]
        public void ParseColumn_NameLength_ReturnsCorrectExpression()
        {
            var column = Column.Parse<Customer>(x => x.Name.Length, info);

            Assert.Equal("LEN([Name]) AS [Name]", column.ToExpression(info).ToString());
        }

        [Fact]
        public void ParseColumnWithAlias_NameLength_ReturnsCorrectExpressionWithAlias()
        {
            var column = Column.Parse<Customer>(x => new { CliId = x.Name.Length }, info);

            Assert.Equal("LEN([Name]) AS [CliId]", column.ToExpression(info).ToString());
        }

        [Fact]
        public void SelectCancatWithInclude()
        {
            var memberInfo = typeof(Customer).GetProperty(nameof(Customer.Address));

            QueryInfo info = new(new SqlServerQueryConfig(), new DbName("Customer"));
            info.Joins.Add(new JoinQuery(info.Config, new DbName("Address")) { MemberInfo = memberInfo });

            var processor = new ExpressionProcessor<Customer>(info, ExpressionConfig.All);
            var columns = processor.ParseColumns(x => string.Concat(x.Name, x.Address.Street)).ToArray();

            Assert.Single(columns);
            Assert.Equal("CONCAT([Customer].[Name],[Address].[Street]) AS [Concat]", columns[0].ToExpression(info).ToString());
        }

        [Fact]
        public void SelectWithInclude()
        {
            var memberInfo = typeof(Customer).GetProperty(nameof(Customer.Address));

            QueryInfo info = new(new SqlServerQueryConfig(), new DbName("Customer"));
            info.Joins.Add(new JoinQuery(info.Config, new DbName("Address")) { MemberInfo = memberInfo });

            var processor = new ExpressionProcessor<Customer>(info, ExpressionConfig.All);
            var columns = processor.ParseColumns(x => new { x.Name, x.Address.Street }).ToArray();

            Assert.Equal(2, columns.Length);
            Assert.Equal("[Customer].[Name]", columns[0].ToExpression(info).ToString());
            Assert.Equal("[Address].[Street]", columns[1].ToExpression(info).ToString());
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
