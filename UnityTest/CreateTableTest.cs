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
            DbTable.Create(GetSchema()).Dispose();
        }

        [TestMethod]
        public void CreateEmptyByAnother()
        {
            var manager = GetConnectionManager();
            using var table = DbTable.CreateTemp(new TableSchema("MyTestTable", "Address"), NewConfig, manager);
            var expectedCols = GetTableColumns(new DbName("Address"), manager);

            CollectionAssert.AreEqual(expectedCols, GetTableColumns(table.Name, manager));
            Assert.AreEqual(0, table.GetQuery().Count());
        }

        [TestMethod]
        public void CreateByAnother()
        {
            var manager = GetConnectionManager();
            var expectedRows = InsertAddressValue();

            using var table = DbTable.CreateTemp(new TableSchema("MyTestTable", "Address", true), NewConfig, manager);
            var expectedCols = GetTableColumns(new DbName("Address"), manager);
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
            var schema = GetSchema();
            using var table = DbTable.Create(schema, NewConfig);
            Assert.IsTrue(DbTable.Exists(table.Manager, schema, NewConfig));
        }

        [TestMethod]
        public void InsertData()
        {
            using var table = DbTable.Create(GetSchema(), NewConfig);
            var q = table.GetQuery();
            q.Insert(new Cell("name", "Richard"));
            q.Insert(new Cell("name", "Manuel"));

            Assert.AreEqual(2, q.Count());
        }

        private static TableSchema GetSchema()
        {
            var schema = new TableSchema("MyTestTable") { Temporary = true };
            schema.Columns.AddUnique("Id");
            schema.Columns.Add<string>("Name");

            return schema;
        }
    }
}
