using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Builder.Tables;
using System.Text;

namespace QueryTest.SqlServer
{
    public class DbTableSqlTests : DbMockFallbackTest
    {
        public DbTableSqlTests() : base(new SqlServerQueryConfig())
        {
            Manager.Management = SharpOrm.Connection.ConnectionManagement.CloseOnManagerDispose;
        }

        [Fact]
        public void CreateTempTable_ShouldGenerateSelectInto_AndDropOnDispose()
        {
            using var fallback = RegisterFallback();
            var query = new Query(new DbName("SourceTable"), Manager);
            query.Select("Id", "Name");
            query.Where(new SqlExpression("1!=1"));

            DbTable<TestTable> table = DbTable<TestTable>.CreateTempTable(query);
            var name = table.Name;
            table.Dispose();

            StringBuilder expected = new();
            expected.Append($"SELECT [Id],[Name] INTO [{name}] FROM [SourceTable] WHERE 1!=1");
            expected.AppendLine();
            expected.Append($"DROP TABLE [{name}]");

            Assert.Equal(expected.ToString(), fallback.ToString());
        }

        [Fact]
        public void Create_WithColumns_ShouldGenerateCreateTable_AndDropOnDispose()
        {
            using var fallback = RegisterFallback();
            var builder = new TableBuilder("MyTempTable", true);
            builder.AddColumn("Id", typeof(int));
            builder.AddColumn("Name", typeof(string));

            var table = DbTable.Create(builder.GetSchema(), Manager);
            var name = table.DbName;
            table.Dispose();

            StringBuilder expected = new();
            expected.Append($"CREATE TABLE [{name}] ([Id] INT NULL,[Name] VARCHAR(MAX) NULL)");
            expected.AppendLine();
            expected.Append($"DROP TABLE [{name}]");

            Assert.Equal(expected.ToString(), fallback.ToString());
        }
    }
}