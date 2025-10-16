using BaseTest.Utils;
using DbRunTest.Fixtures;
using Microsoft.Data.Sqlite;
using SharpOrm.Builder;
using SharpOrm.Builder.Tables;
using SharpOrm.SqlMethods;
using Xunit.Abstractions;

namespace DbRunTest.Sqlite
{
    public class SqliteColumnInspectorTest : DbTestBase, IClassFixture<UnsafeDbFixture<SqliteConnection>>
    {
        public SqliteColumnInspectorTest(ITestOutputHelper output, UnsafeDbFixture<SqliteConnection> connection)
            : base(output, connection)
        {
            connection.Manager.Management = SharpOrm.Connection.ConnectionManagement.CloseOnManagerDispose;
        }

        [Fact]
        public void GetColumns_ShouldReturnAllColumns()
        {
            // Arrange
            using var table = CreateTestTable();

            // Act
            var columns = table.GetColumns();

            // Assert
            Assert.NotNull(columns);
            Assert.NotEmpty(columns);
            Assert.Equal(4, columns.Length);

            var idColumn = columns.First(c => c.ColumnName == "Id");
            Assert.Equal("INTEGER", idColumn.DataType);
            Assert.True(idColumn.IsPrimaryKey);
            Assert.False(idColumn.IsNullable);

            var nameColumn = columns.First(c => c.ColumnName == "Name");
            Assert.Equal("TEXT", nameColumn.DataType);
            Assert.False(nameColumn.IsNullable);

            var descriptionColumn = columns.First(c => c.ColumnName == "Description");
            Assert.Equal("TEXT", descriptionColumn.DataType);
            Assert.True(descriptionColumn.IsNullable);

            var priceColumn = columns.First(c => c.ColumnName == "Price");
            Assert.Equal("REAL", priceColumn.DataType);
        }

        [Fact]
        public async Task GetColumnsAsync_ShouldReturnAllColumns()
        {
            // Arrange
            using var table = CreateTestTable();

            // Act
            var columns = await table.GetColumnsAsync();

            // Assert
            Assert.NotNull(columns);
            Assert.NotEmpty(columns);
            Assert.Equal(4, columns.Length);
        }

        [Fact]
        public void GetColumns_ShouldReturnColumnsInOrder()
        {
            // Arrange
            using var table = CreateTestTable();

            // Act
            var columns = table.GetColumns();

            // Assert
            for (int i = 0; i < columns.Length; i++)
            {
                Assert.Equal(i + 1, columns[i].OrdinalPosition);
            }
        }

        [Fact]
        public void GetColumns_WithTypedColumn_ShouldParseTypeCorrectly()
        {
            // Arrange - create a table with typed columns
            var builder = new TableBuilder("TypedTable", true);
            builder.AddColumn<int>("Id");
            builder.AddColumn<string>("Code").HasColumnType("VARCHAR(50)");
            builder.AddColumn<decimal>("Amount").HasColumnType("DECIMAL(10,2)");

            builder.HasKey("Id");

            using var table = DbTable.Create(builder.GetSchema(), Manager);

            // Act
            var columns = table.GetColumns();

            // Assert
            var codeColumn = columns.First(c => c.ColumnName == "Code");
            Assert.Equal("VARCHAR", codeColumn.DataType);
            Assert.Equal(50, codeColumn.MaxLength);

            var amountColumn = columns.First(c => c.ColumnName == "Amount");
            Assert.Equal("DECIMAL", amountColumn.DataType);
            Assert.Equal(10, amountColumn.Precision);
            Assert.Equal(2, amountColumn.Scale);
        }

        [Fact]
        public void GetColumns_ColumnInfo_ShouldHaveCorrectProperties()
        {
            // Arrange
            using var table = CreateTestTable();

            // Act
            var columns = table.GetColumns();
            var idColumn = columns.First(c => c.ColumnName == "Id");

            // Assert
            Assert.Equal("Id", idColumn.ColumnName);
            Assert.Equal("INTEGER", idColumn.DataType);
            Assert.Equal("INTEGER", idColumn.GetFullTypeDefinition());
            Assert.True(idColumn.ColumnName.EqualsIgnoreCase("id"));
            Assert.True(idColumn.ColumnName.EqualsIgnoreCase("ID"));
            Assert.False(idColumn.ColumnName.EqualsIgnoreCase("Name"));
        }

        [Fact]
        public void GetColumns_ToString_ShouldFormatCorrectly()
        {
            // Arrange
            using var table = CreateTestTable();

            // Act
            var columns = table.GetColumns();
            var idColumn = columns.First(c => c.ColumnName == "Id");
            var nameColumn = columns.First(c => c.ColumnName == "Name");

            // Assert
            Assert.Contains("Id", idColumn.ToString());
            Assert.Contains("INTEGER", idColumn.ToString());
            Assert.Contains("NOT NULL", idColumn.ToString());
            Assert.Contains("PRIMARY KEY", idColumn.ToString());

            Assert.Contains("Name", nameColumn.ToString());
            Assert.Contains("TEXT", nameColumn.ToString());
            Assert.Contains("NOT NULL", nameColumn.ToString());
        }

        private DbTable CreateTestTable()
        {
            var builder = new TableBuilder("TestTable", true);
            builder.AddColumn<int>("Id");
            builder.AddColumn<string>("Name").IsRequired();
            builder.AddColumn<string>("Description").IsOptional();
            builder.AddColumn<decimal>("Price");
            builder.HasKey("Id");

            return DbTable.Create(builder.GetSchema(), Manager);
        }
    }
}
