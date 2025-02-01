using BaseTest.Utils;
using QueryTest.Interfaces;
using QueryTest.Utils;
using SharpOrm.Builder;
using static QueryTest.Builder.Expressions.ExpressionProcessorTests;

namespace QueryTest.Mysql
{
    public class StringMapTest : SqlMethodMapTest, ISqlStringMapTest
    {
        public StringMapTest() : base(new MysqlQueryConfig())
        {
        }

        [Fact]
        public void Concat()
        {
            string value2 = "value2";
            var column = ParseColumn<SampleClass>(x => string.Concat("Value", value2, x.Name)).LoadDeferred(info, true)!;

            Assert.Equal("CONCAT(?,?,`Name`) AS `Concat`", column.ToString());
        }

        [Fact]
        public void Substring()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.Substring(0, 10)).LoadDeferred(info, true);

            Assert.Equal("SUBSTRING(`Name`,0,10) AS `Name`", column.ToString());
        }

        [Fact]
        public void StringTrim()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.Trim()).LoadDeferred(info, true);

            Assert.Equal("TRIM(`Name`) AS `Name`", column.ToString());
        }

        [Fact]
        public void StringTrimStart()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.TrimStart()).LoadDeferred(info, true);

            Assert.Equal("LTRIM(`Name`) AS `Name`", column.ToString());
        }

        [Fact]
        public void StringTrimEnd()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.TrimEnd()).LoadDeferred(info, true);

            Assert.Equal("RTRIM(`Name`) AS `Name`", column.ToString());
        }

        [Fact]
        public void SubstringWithColumnIndex()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.Substring(x.StartIndex, 10)).LoadDeferred(info, true);

            Assert.Equal("SUBSTRING(`Name`,`StartIndex`,10) AS `Name`", column.ToString());
        }

        [Fact]
        public void SubstringByIndexColumns()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.Substring(x.StartIndex, x.EndIndex)).LoadDeferred(info, true);

            Assert.Equal("SUBSTRING(`Name`,`StartIndex`,`EndIndex`) AS `Name`", column.ToString());
        }
    }
}
