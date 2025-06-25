using BaseTest.Utils;
using QueryTest.Interfaces;
using QueryTest.Utils;
using SharpOrm.Fb;
using static QueryTest.Builder.Expressions.ExpressionProcessorTests;

namespace QueryTest.Firebird
{
    public class StringMapTest : SqlMethodMapTest, ISqlStringMapTest
    {
        public StringMapTest() : base(new FbQueryConfig())
        {
        }

        [Fact]
        public void Concat()
        {
            string value2 = "value2";
            var column = ParseColumn<SampleClass>(x => string.Concat("Value", value2, x.Name)).LoadDeferred(info, true)!;

            Assert.Equal("?||?||\"Name\" AS \"Concat\"", column.ToString());
        }

        [Fact]
        public void Substring()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.Substring(0, 10)).LoadDeferred(info, true);

            Assert.Equal("SUBSTRING(\"Name\" FROM 0 FOR 10) AS \"Name\"", column.ToString());
        }

        [Fact]
        public void StringTrim()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.Trim()).LoadDeferred(info, true);

            Assert.Equal("TRIM(\"Name\") AS \"Name\"", column.ToString());
        }

        [Fact]
        public void StringTrimStart()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.TrimStart()).LoadDeferred(info, true);

            Assert.Equal("TRIM(LEADING FROM \"Name\") AS \"Name\"", column.ToString());
        }

        [Fact]
        public void StringTrimEnd()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.TrimEnd()).LoadDeferred(info, true);

            Assert.Equal("TRIM(TRAILING FROM \"Name\") AS \"Name\"", column.ToString());
        }

        [Fact]
        public void StringLength()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.Length).LoadDeferred(info, true);

            Assert.Equal("CHAR_LENGTH(\"Name\") AS \"Name\"", column.ToString());
        }

        [Fact]
        public void StringToLower()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.ToLower()).LoadDeferred(info, true);

            Assert.Equal("LOWER(\"Name\") AS \"Name\"", column.ToString());
        }

        [Fact]
        public void StringToUpper()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.ToUpper()).LoadDeferred(info, true);

            Assert.Equal("UPPER(\"Name\") AS \"Name\"", column.ToString());
        }

        [Fact]
        public void SubstringWithColumnIndex()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.Substring(x.StartIndex, 10)).LoadDeferred(info, true);

            Assert.Equal("SUBSTRING(\"Name\" FROM \"StartIndex\" FOR 10) AS \"Name\"", column.ToString());
        }

        [Fact]
        public void SubstringByIndexColumns()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.Substring(x.StartIndex, x.EndIndex)).LoadDeferred(info, true);

            Assert.Equal("SUBSTRING(\"Name\" FROM \"StartIndex\" FOR \"EndIndex\") AS \"Name\"", column.ToString());
        }

        [Fact]
        public void StringToString()
        {
            var column = ParseColumn<SampleClass>(x => x.Name!.ToString()).LoadDeferred(info, true);

            Assert.Equal("\"Name\" AS \"Name\"", column.ToString());
        }
    }
}