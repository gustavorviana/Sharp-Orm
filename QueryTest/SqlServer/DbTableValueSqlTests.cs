using BaseTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using System.Text;

namespace QueryTest.SqlServer
{
    public class DbTableValueSqlTests : DbMockFallbackTest
    {
        public DbTableValueSqlTests() : base(new SqlServerQueryConfig())
        {
            Manager.Management = SharpOrm.Connection.ConnectionManagement.CloseOnManagerDispose;
        }

        [Fact]
        public void FromValues_ShouldGenerateCreateTable_InsertValues_AndDropOnDispose()
        {
            using var fallback = RegisterFallback();
            DbTableValue<int> tableValue = DbTableValue<int>.FromValues(Manager, new[] { 10, 20, 30 });
            var name = tableValue.Table;
            tableValue.Dispose();

            StringBuilder expected = new();
            expected.Append($"CREATE TABLE [{name}] ([Int32] INT NULL)");
            expected.AppendLine();
            expected.Append($"INSERT INTO [{name}] ([Int32]) VALUES (10), (20), (30)");
            expected.AppendLine();
            expected.Append($"DROP TABLE [{name}]");

            Assert.Equal(expected.ToString(), fallback.ToString());
        }

        [Fact]
        public void WhereIn_WithTableValue_ShouldGenerateSelectWithInSubquery()
        {
            using var fallback = RegisterFallback();
            using var tableValue = DbTableValue<int>.FromValues(Manager, new[] { 1, 2, 3 });
            var tableName = tableValue.Table;

            new Query("Orders", Manager)
                .Select("CustomerId")
                .WhereIn("CustomerId", tableValue)
                .ReadRows();

            StringBuilder expected = new();
            expected.Append($"CREATE TABLE [{tableName}] ([Int32] INT NULL)");
            expected.AppendLine();
            expected.Append($"INSERT INTO [{tableName}] ([Int32]) VALUES (1), (2), (3)");
            expected.AppendLine();
            expected.Append($"SELECT [CustomerId] FROM [Orders] WHERE [CustomerId] IN (SELECT [Int32] FROM [{tableName}])");

            Assert.Equal(expected.ToString(), fallback.ToString());
        }

        [Fact]
        public void Exists_WithTableValueJoinAndWhereColumn_ShouldGenerateExistsSubquery()
        {
            using var fallback = RegisterFallback();
            using var tableValue = DbTableValue<int>.FromValues(Manager, [1, 2, 3]);
            var tableName = tableValue.Table;

            var subQuery = Query.ReadOnly(tableName, Config);
            subQuery.Join("Orders", q => q.WhereColumn($"{tableName.Name}.Int32", "Orders.CustomerId"));

            var query = new Query("Customers", Manager);
            query.Select("Id").Exists(subQuery);
            query.ReadRows();

            StringBuilder expected = new();
            expected.Append($"CREATE TABLE [{tableName}] ([Int32] INT NULL)");
            expected.AppendLine();
            expected.Append($"INSERT INTO [{tableName}] ([Int32]) VALUES (1), (2), (3)");
            expected.AppendLine();
            expected.Append($"SELECT [Id] FROM [Customers] WHERE EXISTS (SELECT 1 FROM [{tableName}] INNER JOIN [Orders] ON [{tableName}].[Int32] = [Orders].[CustomerId])");

            Assert.Equal(expected.ToString(), fallback.ToString());
        }

        [Fact]
        public void InsertInto_AddFromSource_ShouldGenerateInsertIntoSelect()
        {
            using var fallback = RegisterFallback();
            DbTableValue<int> tableValue = DbTableValue<int>.FromValues(Manager, new[] { 1, 2, 3 });
            var tableName = tableValue.Table;

            tableValue.InsertInto("TargetTable").Add("TargetCol").Execute();

            StringBuilder expected = new();
            expected.Append($"CREATE TABLE [{tableName}] ([Int32] INT NULL)");
            expected.AppendLine();
            expected.Append($"INSERT INTO [{tableName}] ([Int32]) VALUES (1), (2), (3)");
            expected.AppendLine();
            expected.Append($"INSERT INTO [TargetTable] ([TargetCol]) SELECT [Int32] FROM [{tableName}];");

            Assert.Equal(expected.ToString(), fallback.ToString());
        }

        [Fact]
        public void InsertInto_AddConstant_ShouldGenerateInsertIntoSelectWithLiteral()
        {
            using var fallback = RegisterFallback();
            DbTableValue<string> tableValue = DbTableValue<string>.FromValues(Manager, new[] { "a", "b" });
            var tableName = tableValue.Table;

            tableValue.InsertInto("Dest").Add("FixedCol", 42).Execute();

            StringBuilder expected = new();
            expected.Append($"CREATE TABLE [{tableName}] ([String] VARCHAR(MAX) NULL)");
            expected.AppendLine();
            expected.Append($"INSERT INTO [{tableName}] ([String]) VALUES (@p1), (@p2)");
            expected.AppendLine();
            expected.Append("INSERT INTO [Dest] ([FixedCol]) SELECT @p1 AS [FixedCol] FROM [").Append(tableName).Append("];");

            Assert.Equal(expected.ToString(), fallback.ToString());
        }
    }

}
