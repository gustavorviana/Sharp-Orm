using BaseTest.Fixtures;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Builder.Grammars.Mysql;
using SharpOrm.Builder.Tables;
using Xunit.Abstractions;

namespace QueryTest.Mysql
{
    public class ColumnInspectorTest : DbGrammarTestBase, IClassFixture<MockFixture<MysqlQueryConfig>>
    {
        public ColumnInspectorTest(ITestOutputHelper output, MockFixture<MysqlQueryConfig> connection)
            : base(output, connection)
        {
        }

        [Fact]
        public void CreateColumnInspector_ShouldReturnMysqlColumnInspector()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());

            // Act
            var inspector = grammar.CreateColumnInspector();

            // Assert
            Assert.NotNull(inspector);
            Assert.IsType<MysqlColumnInspector>(inspector);
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
        public void GetColumnsQuery_WithoutSchema_ShouldUseDATABASE()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            // Act
            var sql = inspector.GetColumnsQuery();

            // Assert
            Assert.Contains("TABLE_SCHEMA = DATABASE()", sql.ToString());
        }

        [Fact]
        public void GetColumnsQuery_WithSchema_ShouldIncludeSchemaFilter()
        {
            // Arrange
            var builder = new TableBuilder("mydb.TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            // Act
            var sql = inspector.GetColumnsQuery();

            // Assert
            Assert.NotNull(sql);
            Assert.Contains("TABLE_SCHEMA = ?", sql.ToString());
            Assert.Contains("TestTable", sql.Parameters);
            Assert.Contains("mydb", sql.Parameters);
        }

        [Fact]
        public void MapToColumnInfo_WithValidRow_ShouldMapAllProperties()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            var row = new Row(
                new Cell("ColumnName", "id"),
                new Cell("DataType", "int"),
                new Cell("MaxLength", null),
                new Cell("Precision", 10),
                new Cell("Scale", 0),
                new Cell("IsNullable", 0),
                new Cell("OrdinalPosition", 1),
                new Cell("DefaultValue", null),
                new Cell("Collation", "utf8mb4_general_ci"),
                new Cell("IsPrimaryKey", 1),
                new Cell("IsIdentity", 1),
                new Cell("IsComputed", 0),
                new Cell("Comment", "Primary key")
            );

            // Act
            var rowCollection = new RowDataReader(new[] { row });
            var columns = inspector.MapToColumnInfo(rowCollection);

            // Assert
            Assert.NotNull(columns);
            Assert.Single(columns);
            var columnInfo = columns[0];
            Assert.Equal("id", columnInfo.ColumnName);
            Assert.Equal("int", columnInfo.DataType);
            Assert.False(columnInfo.IsNullable);
            Assert.Equal(1, columnInfo.OrdinalPosition);
            Assert.Equal(10, columnInfo.Precision);
            Assert.Equal(0, columnInfo.Scale);
            Assert.True(columnInfo.IsPrimaryKey);
            Assert.True(columnInfo.IsIdentity);
            Assert.False(columnInfo.IsComputed);
            Assert.Equal("Primary key", columnInfo.Comment);
        }

        [Fact]
        public void MapToColumnInfo_WithVarcharColumn_ShouldMapMaxLength()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            var row = new Row(
                new Cell("ColumnName", "name"),
                new Cell("DataType", "varchar"),
                new Cell("MaxLength", 255L),
                new Cell("Precision", null),
                new Cell("Scale", null),
                new Cell("IsNullable", 1),
                new Cell("OrdinalPosition", 2),
                new Cell("DefaultValue", null),
                new Cell("Collation", "utf8mb4_general_ci"),
                new Cell("IsPrimaryKey", 0),
                new Cell("IsIdentity", 0),
                new Cell("IsComputed", 0),
                new Cell("Comment", "")
            );

            // Act
            var rowCollection = new RowDataReader(new[] { row });
            var columns = inspector.MapToColumnInfo(rowCollection);

            // Assert
            Assert.NotNull(columns);
            Assert.Single(columns);
            var columnInfo = columns[0];
            Assert.Equal("name", columnInfo.ColumnName);
            Assert.Equal("varchar", columnInfo.DataType);
            Assert.True(columnInfo.IsNullable);
            Assert.Equal(255, columnInfo.MaxLength);
            Assert.Equal("utf8mb4_general_ci", columnInfo.Collation);
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

            var row = new Row(
                new Cell("ColumnName", "price"),
                new Cell("DataType", "decimal"),
                new Cell("MaxLength", null),
                new Cell("Precision", 10L),
                new Cell("Scale", 2L),
                new Cell("IsNullable", 0),
                new Cell("OrdinalPosition", 3),
                new Cell("DefaultValue", "0.00"),
                new Cell("Collation", null),
                new Cell("IsPrimaryKey", 0),
                new Cell("IsIdentity", 0),
                new Cell("IsComputed", 0),
                new Cell("Comment", "")
            );

            // Act
            var rowCollection = new RowDataReader(new[] { row });
            var columns = inspector.MapToColumnInfo(rowCollection);

            // Assert
            Assert.NotNull(columns);
            Assert.Single(columns);
            var columnInfo = columns[0];
            Assert.Equal("price", columnInfo.ColumnName);
            Assert.Equal("decimal", columnInfo.DataType);
            Assert.False(columnInfo.IsNullable);
            Assert.Equal(10, columnInfo.Precision);
            Assert.Equal(2, columnInfo.Scale);
            Assert.Equal("0.00", columnInfo.DefaultValue);
        }

        [Fact]
        public void GetColumnsQuery_ShouldDetectAutoIncrement()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            // Act
            var sql = inspector.GetColumnsQuery();

            // Assert
            Assert.Contains("auto_increment", sql.ToString());
            Assert.Contains("EXTRA", sql.ToString());
        }

        [Fact]
        public void GetColumnsQuery_ShouldDetectGeneratedColumns()
        {
            // Arrange
            var builder = new TableBuilder("TestTable", false);
            var grammar = GetTableGrammar(builder.GetSchema());
            var inspector = grammar.CreateColumnInspector();

            // Act
            var sql = inspector.GetColumnsQuery();

            // Assert
            Assert.Contains("GENERATION_EXPRESSION", sql.ToString());
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
    }
}
