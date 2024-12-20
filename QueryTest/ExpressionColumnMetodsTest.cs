using BaseTest.Utils;
using SharpOrm.Builder;
using static QueryTest.ExpressionProcessorTest;

namespace QueryTest
{
    public class ExpressionColumnMetodsTest : SqlMethodMapTest
    {
        public ExpressionColumnMetodsTest() : base(new SqlServerQueryConfig())
        {

        }

        [Fact]
        public void ThrowStaticMethodNotSupported()
        {
            Assert.Throws<NotSupportedException>(() => ParseColumns<SampleClass>(x => string.Concat("Name: ", x.Name)).First());
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
