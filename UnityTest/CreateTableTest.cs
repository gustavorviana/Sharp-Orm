using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using System.Data;
using System.Data.Common;
using System.Linq;
using UnityTest.Utils;

namespace UnityTest
{
    [TestClass]
    public class CreateTableTest : SqlServerTest
    {
        [TestMethod]
        public void CreateByColumnTest()
        {
            GetTableCreator().Create().Dispose();
        }

        [TestMethod]
        public void CreateEmptyByAnother()
        {
            var manager = GetConnectionManager();
            var schema = new TableSchema("MyTestTable", "Address") { Temporary = true };
            using var table = new TableBuilder(schema, new SqlServerQueryConfig(), manager);
            var expectedCols = GetTableColumns(new DbName("Address"), manager);
            table.Create();

            CollectionAssert.AreEqual(expectedCols, GetTableColumns(table.Name, manager));
            Assert.AreEqual(0, table.GetQuery().Count());
        }

        [TestMethod]
        public void CreateByAnother()
        {
            var manager = GetConnectionManager();
            var schema = new TableSchema("MyTestTable", "Address", true) { Temporary = true };
            using var table = new TableBuilder(schema, new SqlServerQueryConfig(), manager);
            var expectedRows = InsertAddressValue();
            var expectedCols = GetTableColumns(new DbName("Address"), manager);
            table.Create();
            var rows = table.GetQuery().ReadRows();

            Assert.AreEqual(1, rows.Length);
            CollectionAssert.AreEqual(expectedCols, GetTableColumns(table.Name, manager));
            CollectionAssert.AreEqual(expectedRows.Cells, rows[0].Cells);
        }

        private static string[] GetTableColumns(DbName name, ConnectionManager manager)
        {
            using var q = new Query(name, NewConfig, manager);
            q.Where(new SqlExpression("1=2"));

            using var reader = q.ExecuteReader();
            return reader.GetColumnSchema().Select(x => x.ColumnName).ToArray();
        }

        private Row InsertAddressValue()
        {
            var cells = new[] { new Cell("id", 1), new Cell("name", "My name"), new Cell("street", "My street") };
            using var q = new Query("Address");
            q.Delete();
            q.Insert(cells);

            return new Row(cells);
        }

        [TestMethod]
        public void CheckExists()
        {
            using var table = GetTableCreator().Create();
            Assert.IsTrue(table.Exists());
        }

        [TestMethod]
        public void InsertData()
        {
            using var table = GetTableCreator().Create();
            var q = table.GetQuery();
            q.Insert(new Cell("name", "Richard"));
            q.Insert(new Cell("name", "Manuel"));

            Assert.AreEqual(2, q.Count());
        }

        private static TableBuilder GetTableCreator()
        {
            var schema = new TableSchema("MyTestTable") { Temporary = true };
            var table = new TableBuilder(schema, new SqlServerQueryConfig(), new ConnectionManager(Connection));
            schema.Columns.AddUnique("Id");
            schema.Columns.Add<string>("Name");

            return table;
        }
    }
}
