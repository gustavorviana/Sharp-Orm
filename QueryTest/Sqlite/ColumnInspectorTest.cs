using BaseTest.Fixtures;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Builder.Grammars.Sqlite;
using SharpOrm.Builder.Tables;
using Xunit.Abstractions;

namespace QueryTest.Sqlite
{
    public class ColumnInspectorTest : DbGrammarTestBase, IClassFixture<MockFixture<SqliteQueryConfig>>
    {
        public ColumnInspectorTest(ITestOutputHelper output, MockFixture<SqliteQueryConfig> connection)
            : base(output, connection)
        {
            MakeUnsafe();
        }

        [Fact]
        public void CreateColumnInspector_ShouldReturnSqliteColumnInspector()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());

            // Act
            var inspector = grammar.CreateColumnInspector();

            // Assert
            Assert.NotNull(inspector);
            Assert.IsType<SqliteColumnInspector>(inspector);
        }

        [Fact]
        public void GetColumnsQuery_ShouldUsePragmaTableInfo()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            // Act
            var sql = inspector.GetColumnsQuery();

            // Assert
            Assert.NotNull(sql);
            Assert.Contains("PRAGMA table_info", sql.ToString());
            Assert.Contains("TestTable", sql.ToString());
        }

        [Fact]
        public void MapToColumnInfo_WithValidRow_ShouldMapAllProperties()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            var rows = new RowDataReader(new Row(
                new Cell("cid", 0L),
                new Cell("name", "Id"),
                new Cell("type", "INTEGER"),
                new Cell("notnull", 1L),
                new Cell("dflt_value", null),
                new Cell("pk", 1L)
            ));

            // Act
            var columnInfo = inspector.MapToColumnInfo(rows).First();

            // Assert
            Assert.NotNull(columnInfo);
            Assert.Equal("Id", columnInfo.ColumnName);
            Assert.Equal("INTEGER", columnInfo.DataType);
            Assert.False(columnInfo.IsNullable);
            Assert.Equal(1, columnInfo.OrdinalPosition); // cid 0 becomes ordinal 1
            Assert.True(columnInfo.IsPrimaryKey);
        }

        [Fact]
        public void MapToColumnInfo_WithVarcharColumn_ShouldParseMaxLength()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            var rows = new RowDataReader(new Row(
                new Cell("cid", 1L),
                new Cell("name", "Name"),
                new Cell("type", "VARCHAR(255)"),
                new Cell("notnull", 0L),
                new Cell("dflt_value", null),
                new Cell("pk", 0L)
            ));

            // Act
            var columnInfo = inspector.MapToColumnInfo(rows).First();

            // Assert
            Assert.NotNull(columnInfo);
            Assert.Equal("Name", columnInfo.ColumnName);
            Assert.Equal("VARCHAR", columnInfo.DataType);
            Assert.True(columnInfo.IsNullable);
            Assert.Equal(255, columnInfo.MaxLength);
            Assert.Null(columnInfo.Precision);
            Assert.Null(columnInfo.Scale);
            Assert.False(columnInfo.IsPrimaryKey);
        }

        [Fact]
        public void MapToColumnInfo_WithDecimalColumn_ShouldParsePrecisionAndScale()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            var rows = new RowDataReader(new Row(
                new Cell("cid", 2L),
                new Cell("name", "Price"),
                new Cell("type", "DECIMAL(10,2)"),
                new Cell("notnull", 1L),
                new Cell("dflt_value", "0.00"),
                new Cell("pk", 0L)
            ));

            // Act
            var columnInfo = inspector.MapToColumnInfo(rows).First();

            // Assert
            Assert.NotNull(columnInfo);
            Assert.Equal("Price", columnInfo.ColumnName);
            Assert.Equal("DECIMAL", columnInfo.DataType);
            Assert.False(columnInfo.IsNullable);
            Assert.Equal(10, columnInfo.Precision);
            Assert.Equal(2, columnInfo.Scale);
            Assert.Equal("0.00", columnInfo.DefaultValue);
        }

        [Fact]
        public void MapToColumnInfo_WithNumericPrecision_ShouldParsePrecisionOnly()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            var rows = new RowDataReader(new Row(
                new Cell("cid", 3L),
                new Cell("name", "Score"),
                new Cell("type", "NUMERIC(5)"),
                new Cell("notnull", 0L),
                new Cell("dflt_value", null),
                new Cell("pk", 0L)
            ));

            // Act
            var columnInfo = inspector.MapToColumnInfo(rows).First();

            // Assert
            Assert.NotNull(columnInfo);
            Assert.Equal("NUMERIC", columnInfo.DataType);
            Assert.Equal(5, columnInfo.Precision);
            Assert.Null(columnInfo.Scale);
        }

        [Fact]
        public void MapToColumnInfo_WithTextColumn_ShouldNotParseSize()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            var rows = new RowDataReader(new Row(
                new Cell("cid", 4L),
                new Cell("name", "Description"),
                new Cell("type", "TEXT"),
                new Cell("notnull", 0L),
                new Cell("dflt_value", null),
                new Cell("pk", 0L)
            ));

            // Act
            var columnInfo = inspector.MapToColumnInfo(rows).First();

            // Assert
            Assert.NotNull(columnInfo);
            Assert.Equal("TEXT", columnInfo.DataType);
            Assert.Null(columnInfo.MaxLength);
            Assert.Null(columnInfo.Precision);
            Assert.Null(columnInfo.Scale);
        }

        [Fact]
        public void MapToColumnInfo_CidToOrdinalPosition_ShouldConvertCorrectly()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            var row1 = new RowDataReader(new Row(
                new Cell("cid", 0L),
                new Cell("name", "First"),
                new Cell("type", "TEXT"),
                new Cell("notnull", 0L),
                new Cell("dflt_value", null),
                new Cell("pk", 0L)
            ));

            var row2 = new RowDataReader(new Row(
                new Cell("cid", 5L),
                new Cell("name", "Sixth"),
                new Cell("type", "TEXT"),
                new Cell("notnull", 0L),
                new Cell("dflt_value", null),
                new Cell("pk", 0L)
            ));

            // Act
            var col1 = inspector.MapToColumnInfo(row1).First();
            var col2 = inspector.MapToColumnInfo(row2).First();

            // Assert
            Assert.Equal(1, col1.OrdinalPosition); // cid 0 -> ordinal 1
            Assert.Equal(6, col2.OrdinalPosition); // cid 5 -> ordinal 6
        }

        [Fact]
        public void MapToColumnInfo_WithNullableColumn_ShouldDetectNullable()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            var notNullRow = new RowDataReader(new Row(
                new Cell("cid", 0L),
                new Cell("name", "NotNull"),
                new Cell("type", "TEXT"),
                new Cell("notnull", 1L),
                new Cell("dflt_value", null),
                new Cell("pk", 0L)
            ));

            var nullableRow = new RowDataReader(new Row(
                new Cell("cid", 1L),
                new Cell("name", "Nullable"),
                new Cell("type", "TEXT"),
                new Cell("notnull", 0L),
                new Cell("dflt_value", null),
                new Cell("pk", 0L)
            ));

            // Act
            var notNull = inspector.MapToColumnInfo(notNullRow).First();
            var nullable = inspector.MapToColumnInfo(nullableRow).First();

            // Assert
            Assert.False(notNull.IsNullable);
            Assert.True(nullable.IsNullable);
        }

        [Fact]
        public void MapToColumnInfo_WithEmptyType_ShouldHandleGracefully()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            var rows = new RowDataReader(new Row(
                new Cell("cid", 0L),
                new Cell("name", "NoType"),
                new Cell("type", ""),
                new Cell("notnull", 0L),
                new Cell("dflt_value", null),
                new Cell("pk", 0L)
            ));

            // Assert
            Assert.Throws<ArgumentException>(() => inspector.MapToColumnInfo(rows));
        }
    }
}
