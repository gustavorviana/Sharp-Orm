using BaseTest.Utils;
using DbRunTest.Fixtures;
using SharpOrm.Builder;
using SharpOrm.Builder.Tables;
using SharpOrm.Errors;
using SharpOrm.SqlMethods;
using System.Data.SqlClient;
using Xunit.Abstractions;

namespace DbRunTest.SqlServer
{
    public class SqlServerColumnInspectorTest : DbTestBase, IClassFixture<UnsafeDbFixture<SqlConnection>>
    {
        public SqlServerColumnInspectorTest(ITestOutputHelper output, UnsafeDbFixture<SqlConnection> connection)
            : base(output, connection)
        {
            SetConnectionManagement(SharpOrm.Connection.ConnectionManagement.CloseOnManagerDispose);
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
            Assert.Equal("int", idColumn.DataType);
            Assert.True(idColumn.IsPrimaryKey);
            Assert.True(idColumn.IsIdentity);
            Assert.False(idColumn.IsNullable);

            var nameColumn = columns.First(c => c.ColumnName == "Name");
            Assert.Equal("varchar", nameColumn.DataType);
            Assert.Equal(100, nameColumn.MaxLength);
            Assert.False(nameColumn.IsNullable);

            var descriptionColumn = columns.First(c => c.ColumnName == "Description");
            Assert.Equal("varchar", descriptionColumn.DataType);
            Assert.True(descriptionColumn.IsNullable);

            var priceColumn = columns.First(c => c.ColumnName == "Price");
            Assert.Equal("decimal", priceColumn.DataType);
            Assert.Equal(18, priceColumn.Precision);
            Assert.Equal(2, priceColumn.Scale);
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
        public void GetColumns_WithNonExistentTable_ShouldReturnEmpty()
        {
            var randomName = "NonExistentTable_" + Guid.NewGuid().ToString("N");
            var exception = Assert.Throws<DatabaseException>(() => new DbTable(randomName, Manager));
            Assert.Equal($"The table '{randomName}' was not found.", exception.Message);
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
            Assert.Equal("int", idColumn.DataType);
            Assert.Equal("int", idColumn.GetFullTypeDefinition());
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
            Assert.Contains("int", idColumn.ToString());
            Assert.Contains("NOT NULL", idColumn.ToString());
            Assert.Contains("PRIMARY KEY", idColumn.ToString());
            Assert.Contains("IDENTITY", idColumn.ToString());

            Assert.Contains("Name", nameColumn.ToString());
            Assert.Contains("varchar(100)", nameColumn.ToString());
            Assert.Contains("NOT NULL", nameColumn.ToString());
        }

        private DbTable CreateTestTable()
        {
            var builder = new TableBuilder("TestTable", true);
            builder.AddColumn<int>("Id").IsIdentity();
            builder.AddColumn<string>("Name").HasMaxLength(100).IsRequired();
            builder.AddColumn<string>("Description").IsOptional();
            builder.AddColumn<decimal>("Price").HasPrecision(18, 2);
            builder.HasKey("Id");

            return DbTable.Create(builder.GetSchema(), Manager);
        }
    }
}
