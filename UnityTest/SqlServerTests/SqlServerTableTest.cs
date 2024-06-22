using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm.Builder;
using System.Data.SqlClient;
using UnityTest.BaseTests;
using UnityTest.Utils;

namespace UnityTest.SqlServerTests
{
    [TestClass]
    public class SqlServerTableTest : DbTableTest<SqlConnection>
    {
        public SqlServerTableTest() : base(new SqlServerQueryConfig(false) { UseOldPagination = false }, ConnectionStr.SqlServer)
        {
        }

        public override void CheckExists()
        {
            var schema = GetSchema();
            using var table = DbTable.Create(schema, GetConnectionManager());
            Assert.IsTrue(DbTable.Exists(table.DbName.Name[1..], schema.Temporary, table.Manager), "DbTable.Exists(string, bool, ConnectionManager)");
            Assert.IsTrue(table.Exists(), "DbTable.Exists()");
        }
    }
}
