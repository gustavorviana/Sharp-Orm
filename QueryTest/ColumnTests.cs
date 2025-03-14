using SharpOrm;

namespace QueryTest
{
    public class ColumnTests
    {
        [Theory]
        [InlineData("column")]
        [InlineData("column2")]
        [InlineData("my_column")]
        [InlineData("table.column")]
        public void TryParseName_Success(string name)
        {
            Assert.True(Column.TryParse(name, out var column));
            Assert.NotNull(column);
        }

        [Theory]
        [InlineData("my column")]
        [InlineData("table_column!")]
        [InlineData("//")]
        [InlineData("\"")]
        [InlineData("'")]
        public void TryParseName_Fail(string name)
        {
            Assert.False(Column.TryParse(name, out var column));
            Assert.Null(column);
        }

        [Theory]
        [InlineData("column", "alias")]
        [InlineData("column", "alias2")]
        [InlineData("column", "my_alias")]
        [InlineData("column", "my.alias")]
        public void TryParseNameAndAias_Success(string name, string alias)
        {
            Assert.True(Column.TryParse(name, alias, out var column));
            Assert.NotNull(column);
        }

        [Theory]
        [InlineData("column", "#my column")]
        [InlineData("column", "table_column!")]
        [InlineData("column", "//")]
        [InlineData("column", "\"")]
        [InlineData("column", "'")]
        public void TryParseNameAndAias_Fail(string name, string alias)
        {
            Assert.False(Column.TryParse(name, alias, out var column));
            Assert.Null(column);
        }
    }
}
