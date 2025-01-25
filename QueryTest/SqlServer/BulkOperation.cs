using QueryTest.Utils;
using SharpOrm.Builder;
using System.Text;

namespace QueryTest.SqlServer
{
    public class BulkOperation() : BaseBulkOperationTest(new SqlServerQueryConfig())
    {
        [Fact]
        public void BulkUpdateTest()
        {
            using var fallback = RegisterFallback();
            Guid id = Guid.NewGuid();
            ExecuteUpdate(id, out var name);
            StringBuilder expected = new();
            for (int i = 1; i <= 3; i++)
                expected.AppendLine($"INSERT INTO [{name}] ([id], [name], [number], [custom_id], [custom_status]) VALUES ({i}, 'User {i}', {i}, '{id}', 1)");

            expected.Append($"UPDATE [target] SET [target].[number] = [tempTable].[number], [target].[custom_id] = [tempTable].[custom_id], [target].[custom_status] = [tempTable].[custom_status] FROM [TestTable] [target] INNER JOIN [{name}] [tempTable] ON [tempTable].[id] = [target].[id] AND [tempTable].[name] = [target].[name]");
            Assert.Equal(expected.ToString(), fallback.ToString());
        }

        [Fact]
        public void BulkDeleteTest()
        {
            using var fallback = RegisterFallback();
            ExecuteDelete(out var name);
            StringBuilder expected = new();
            for (int i = 1; i <= 3; i++)
                expected.AppendLine($"INSERT INTO [{name}] ([name], [number]) VALUES ('User {i}', {i})");

            expected.Append($"DELETE [target] FROM [TestTable] [target] INNER JOIN [{name}] [tempTable] ON [tempTable].[name] = [target].[name] AND [tempTable].[number] = [target].[number]");
            Assert.Equal(expected.ToString(), fallback.ToString());
        }
    }
}
