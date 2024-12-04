using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Builder.Expressions;
using System.Linq.Expressions;
using static QueryTest.ExpressionProcessorTest;

namespace QueryTest
{
    public class ExpressionColumnMetodsTest
    {
        private readonly ReadonlyQueryInfo info;

        public ExpressionColumnMetodsTest()
        {
            info = new ReadonlyQueryInfo(new SqlServerQueryConfig(), new DbName());
        }

        [Fact]
        public void ParseColumn()
        {
            var column = ParseColumns<SampleClass>(x => x.Name).First();
            var exp = column.ToExpression(info);

            Assert.Equal("Name", exp.ToString());
        }

        [Fact]
        public void StrToLower()
        {
            var column = ParseColumns<SampleClass>(x => new { Name2 = x.Name!.ToLower() }).First();
            var exp = column.ToExpression(info);

            Assert.Equal("LOWER(Name) AS [Name2]", exp.ToString());
        }

        [Fact]
        public void StrToLowerWithoutAlias()
        {
            var column = ParseColumns<SampleClass>(x => new { Namee = x.Name!.ToLower() }).First();
            var exp = column.ToExpression(info, false);

            Assert.Equal("LOWER(Name)", exp.ToString());
        }

        [Fact]
        public void StrToUpper()
        {
            var column = ParseColumns<SampleClass>(x => x.Name!.ToUpper()).First();
            var exp = column.ToExpression(info);

            Assert.Equal("UPPER(Name) AS [Name]", exp.ToString());
        }

        private IEnumerable<Column> ParseColumns<T>(Expression<ColumnExpression<T>> expression, bool allowSubMembers = true)
        {
            var processor = new ExpressionProcessor(allowSubMembers);
            return processor.ParseColumns(info, expression);
        }
    }
}
