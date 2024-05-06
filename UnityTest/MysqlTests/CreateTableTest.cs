using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using System.Data;
using System.Data.Common;
using System.Linq;
using UnityTest.Utils;

namespace UnityTest.MysqlTests
{
    [TestClass]
    public class CreateTableTest : MysqlTableTest
    {
        [TestMethod]
        public void CreateByColumnTest()
        {
            DbTable.Create(GetSchema(), GetConnectionManager()).Dispose();
        }

        [TestMethod]
        public void CreateEmptyByAnother()
        {
            var manager = GetConnectionManager();
            DbTable.Exists(new TableSchema("address") { Temporary = true }, manager);
            //using var table = DbTable.Create("MyTestTable", true, new Query("Address", manager));
            //var expectedCols = GetTableColumns(new DbName("Address"), manager);

            //CollectionAssert.AreEqual(expectedCols, GetTableColumns(table.Name, manager));
            //Assert.AreEqual(0, table.GetQuery().Exists());
        }

        [TestMethod]
        public void CreateByAnother()
        {
            //var manager = GetConnectionManager();
            //var expectedRows = InsertAddressValue();

            //using var table = DbTable.Create("MyTestTable", true, new Query("Address", manager));
            //var expectedCols = GetTableColumns(new DbName("Address"), manager);
            //var rows = table.GetQuery().ReadRows();

            //Assert.AreEqual(1, rows.Length);
            //CollectionAssert.AreEqual(expectedCols, GetTableColumns(table.Name, manager));
            //CollectionAssert.AreEqual(expectedRows.Cells, rows[0].Cells);
        }

        private static string[] GetTableColumns(DbName name, ConnectionManager manager)
        {
            using var q = new Query(name, manager);
            q.Where(new SqlExpression("1=2"));

            using var reader = q.ExecuteReader();
            return reader.GetColumnSchema().Select(x => x.ColumnName).ToArray();
        }

        private Row InsertAddressValue()
        {
            var cells = new[] { new Cell("id", 1), new Cell("name", "My name"), new Cell("street", "My street") };
            using var q = new Query("Address", Creator);
            q.Delete();
            q.Insert(cells);

            return new Row(cells);
        }

        [TestMethod]
        public void CheckExists()
        {
            var schema = GetSchema();
            using var table = DbTable.Create(schema, GetConnectionManager());
            Assert.IsTrue(DbTable.Exists(schema, table.Manager));
        }

        [TestMethod]
        public void InsertData()
        {
            using var table = DbTable.Create(GetSchema(), GetConnectionManager());
            var q = table.GetQuery();
            q.Insert(new Cell("name", "Richard"));
            q.Insert(new Cell("name", "Manuel"));

            Assert.AreEqual(2, q.Count());
        }

        private static TableSchema GetSchema()
        {
            var schema = new TableSchema("MyTestTable") { Temporary = true };
            schema.Columns.AddPk("Id").AutoIncrement = true;
            schema.Columns.Add<string>("Name");

            return schema;
        }
    }
}
