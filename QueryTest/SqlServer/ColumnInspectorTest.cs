using BaseTest.Fixtures;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Builder.Grammars.SqlServer;
using SharpOrm.Builder.Tables;
using Xunit.Abstractions;

namespace QueryTest.SqlServer
{
    public class ColumnInspectorTest : DbGrammarTestBase, IClassFixture<MockFixture<SqlServerQueryConfig>>
    {
        public ColumnInspectorTest(ITestOutputHelper output, MockFixture<SqlServerQueryConfig> connection)
            : base(output, connection)
        {
        }

        [Fact]
        public void CreateColumnInspector_ShouldReturnSqlServerColumnInspector()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());

            // Act
            var inspector = grammar.CreateColumnInspector();

            // Assert
            Assert.NotNull(inspector);
            Assert.IsType<SqlServerColumnInspector>(inspector);
        }

        [Fact]
        public void GetColumnsQuery_ShouldReturnValidSqlExpression()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            // Act
            var sql = inspector.GetColumnsQuery();

            // Assert
            Assert.NotNull(sql);
            Assert.Contains("INFORMATION_SCHEMA.COLUMNS", sql.ToString());
            Assert.Contains("TABLE_NAME = ?", sql.ToString());
            Assert.Contains("TestTable", sql.Parameters);
        }

        [Fact]
        public void GetColumnsQuery_WithSchema_ShouldIncludeSchemaFilter()
        {
            // Arrange
            var builder = new TableBuilder("dbo.TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            // Act
            var sql = inspector.GetColumnsQuery();

            // Assert
            Assert.NotNull(sql);
            Assert.Contains("TABLE_SCHEMA = ?", sql.ToString());
            Assert.Contains("TestTable", sql.Parameters);
            Assert.Contains("dbo", sql.Parameters);
        }

        [Fact]
        public void MapToColumnInfo_WithValidRow_ShouldMapAllProperties()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            var rows = new RowDataReader(new Row(
                new Cell("ColumnName", "Id"),
                new Cell("DataType", "int"),
                new Cell("MaxLength", null),
                new Cell("Precision", 10),
                new Cell("Scale", 0),
                new Cell("IsNullable", 0),
                new Cell("OrdinalPosition", 1),
                new Cell("DefaultValue", null),
                new Cell("Collation", null),
                new Cell("IsPrimaryKey", 1),
                new Cell("IsIdentity", 1),
                new Cell("IsComputed", 0),
                new Cell("Comment", null)
            ));

            // Act
            var columnInfo = inspector.MapToColumnInfo(rows).First();

            // Assert
            Assert.NotNull(columnInfo);
            Assert.Equal("Id", columnInfo.ColumnName);
            Assert.Equal("int", columnInfo.DataType);
            Assert.False(columnInfo.IsNullable);
            Assert.Equal(1, columnInfo.OrdinalPosition);
            Assert.Null(columnInfo.Precision);
            Assert.Equal(0, columnInfo.Scale);
            Assert.True(columnInfo.IsPrimaryKey);
            Assert.True(columnInfo.IsIdentity);
            Assert.False(columnInfo.IsComputed);
        }

        [Fact]
        public void MapToColumnInfo_WithVarcharColumn_ShouldMapMaxLength()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            var rows = new RowDataReader(new Row(
                new Cell("ColumnName", "Name"),
                new Cell("DataType", "varchar"),
                new Cell("MaxLength", 255),
                new Cell("Precision", null),
                new Cell("Scale", null),
                new Cell("IsNullable", 1),
                new Cell("OrdinalPosition", 2),
                new Cell("DefaultValue", null),
                new Cell("Collation", "Latin1_General_CI_AS"),
                new Cell("IsPrimaryKey", 0),
                new Cell("IsIdentity", 0),
                new Cell("IsComputed", 0),
                new Cell("Comment", null)
            ));

            // Act
            var columnInfo = inspector.MapToColumnInfo(rows).First();

            // Assert
            Assert.NotNull(columnInfo);
            Assert.Equal("Name", columnInfo.ColumnName);
            Assert.Equal("varchar", columnInfo.DataType);
            Assert.True(columnInfo.IsNullable);
            Assert.Equal(255, columnInfo.MaxLength);
            Assert.Equal("Latin1_General_CI_AS", columnInfo.Collation);
            Assert.False(columnInfo.IsPrimaryKey);
            Assert.False(columnInfo.IsIdentity);
        }

        [Fact]
        public void MapToColumnInfo_WithDecimalColumn_ShouldMapPrecisionAndScale()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            var rows = new RowDataReader(new Row(
                new Cell("ColumnName", "Price"),
                new Cell("DataType", "decimal"),
                new Cell("MaxLength", null),
                new Cell("Precision", 18),
                new Cell("Scale", 2),
                new Cell("IsNullable", 0),
                new Cell("OrdinalPosition", 3),
                new Cell("DefaultValue", "((0.00))"),
                new Cell("Collation", null),
                new Cell("IsPrimaryKey", 0),
                new Cell("IsIdentity", 0),
                new Cell("IsComputed", 0),
                new Cell("Comment", null)
            ));

            // Act
            var columnInfo = inspector.MapToColumnInfo(rows).First();

            // Assert
            Assert.NotNull(columnInfo);
            Assert.Equal("Price", columnInfo.ColumnName);
            Assert.Equal("decimal", columnInfo.DataType);
            Assert.False(columnInfo.IsNullable);
            Assert.Equal(18, columnInfo.Precision);
            Assert.Equal(2, columnInfo.Scale);
            Assert.Equal("((0.00))", columnInfo.DefaultValue);
        }

        [Fact]
        public void MapToColumnInfo_WithNullValues_ShouldHandleGracefully()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            var rows = new RowDataReader(new Row(
                new Cell("ColumnName", "TestColumn"),
                new Cell("DataType", "varchar"),
                new Cell("MaxLength", DBNull.Value),
                new Cell("Precision", DBNull.Value),
                new Cell("Scale", DBNull.Value),
                new Cell("IsNullable", 1),
                new Cell("OrdinalPosition", 1),
                new Cell("DefaultValue", DBNull.Value),
                new Cell("Collation", DBNull.Value),
                new Cell("IsPrimaryKey", 0),
                new Cell("IsIdentity", 0),
                new Cell("IsComputed", 0),
                new Cell("Comment", DBNull.Value)
            ));

            // Act
            var columnInfo = inspector.MapToColumnInfo(rows).First();

            // Assert
            Assert.NotNull(columnInfo);
            Assert.Equal("TestColumn", columnInfo.ColumnName);
            Assert.Null(columnInfo.MaxLength);
            Assert.Null(columnInfo.Precision);
            Assert.Null(columnInfo.Scale);
            Assert.Null(columnInfo.DefaultValue);
            Assert.Null(columnInfo.Collation);
            Assert.Null(columnInfo.Comment);
        }

        [Fact]
        public void GetColumnsQuery_ShouldOrderByOrdinalPosition()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            // Act
            var sql = inspector.GetColumnsQuery();

            // Assert
            Assert.Contains("ORDER BY c.ORDINAL_POSITION", sql.ToString());
        }

        [Fact]
        public void GetColumnsQuery_ShouldIncludePrimaryKeyDetection()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            // Act
            var sql = inspector.GetColumnsQuery();

            // Assert
            Assert.Contains("PRIMARY KEY", sql.ToString());
            Assert.Contains("KEY_COLUMN_USAGE", sql.ToString());
        }
    }
}
