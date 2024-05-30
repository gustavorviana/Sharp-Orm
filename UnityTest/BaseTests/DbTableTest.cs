using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using System.Data;
using System.Data.Common;
using System.Linq;
using UnityTest.Models;
using UnityTest.Utils;

namespace UnityTest.BaseTests
{
    public abstract class DbTableTest<Conn> : DbTest<Conn> where Conn : DbConnection, new()
    {
        public DbTableTest(QueryConfig config, string connStr) : base(config, connStr)
        {
        }

        [TestMethod]
        public void CreateByColumnTest()
        {
            DbTable.Create(GetSchema(), Manager).Dispose();
        }

        [TestMethod]
        public void CreateEmptyByAnother()
        {
            InsertAddressValue();
            using var table = DbTable.Create("MyTestTable", true, new Column[] { Column.All }, "Address", Manager);
            var expectedCols = GetTableColumns(new DbName("Address"), Manager);

            CollectionAssert.AreEqual(expectedCols, GetTableColumns(table.DbName, Manager));
            Assert.AreEqual(0, table.GetQuery().Count());
        }

        [TestMethod]
        public void CreateByAnother()
        {
            var expectedRows = InsertAddressValue();

            using var q = new Query("Address", Manager);
            q.OrderBy("Id");
            q.Offset = 1;
            using var table = DbTable.Create("MyTestTable", true, q);
            var expectedCols = GetTableColumns(new DbName("Address"), Manager);
            var rows = table.GetQuery().ReadRows();

            Assert.AreEqual(1, rows.Length);
            CollectionAssert.AreEqual(expectedCols, GetTableColumns(table.DbName, Manager));
            CollectionAssert.AreEqual(expectedRows.Cells, rows[0].Cells);
        }

        [TestMethod]
        public void CreateByClass()
        {
            using var table = DbTable.Create<Address>(true, manager: Manager);
            using var query = table.GetQuery();
            query.Limit = 0;

            var tableColumns = query.ReadTable().Columns.OfType<DataColumn>().Select(x => x.ColumnName).ToArray();
            var classColumns = new TableInfo(typeof(Address)).Columns.Select(x => x.Name).ToArray();

            CollectionAssert.AreEqual(tableColumns, classColumns);
        }

        private static string[] GetTableColumns(DbName name, ConnectionManager manager)
        {
            using var q = new Query(name, manager);
            q.Limit = 0;

            using var reader = q.ExecuteReader();
            return reader.GetColumnSchema().Select(x => x.ColumnName).ToArray();
        }

        private Row InsertAddressValue()
        {
            var cells = new[] { new Cell("id", 1), new Cell("name", "My name"), new Cell("street", "My street") };
            using var q = new Query("Address", Creator);
            q.Delete();
            q.Insert(cells);
            cells[0] = new Cell("id", 2);
            q.Insert(cells);

            return new Row(cells);
        }

        [TestMethod]
        public virtual void CheckExists()
        {
            var schema = GetSchema();
            using var table = DbTable.Create(schema, GetConnectionManager());
            Assert.IsTrue(DbTable.Exists(table.DbName.Name, schema.Temporary, table.Manager), "DbTable.Exists(string, bool, ConnectionManager)");
            Assert.IsTrue(table.Exists(), "DbTable.Exists()");
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

        [TestMethod]
        public void CreateTable()
        {
            var cols = new TableColumnCollection();
            cols.AddPk("Id");
            cols.Add<string>("Name");
            cols.Add<int>("Status").Unique = true;

            var schema = new TableSchema("MyTestTable", cols) { Temporary = true };
            using var table = DbTable.Create(schema, GetConnectionManager());
        }

        [TestMethod]
        public void CreateTableMultiplePk()
        {
            var cols = new TableColumnCollection();
            cols.AddPk("Id").AutoIncrement = true;
            cols.AddPk("Id2");

            var schema = new TableSchema("MyTestTable", cols) { Temporary = true };
            using var table = DbTable.Create(schema, GetConnectionManager());
        }

        protected static TableSchema GetSchema()
        {
            var schema = new TableSchema("MyTestTable") { Temporary = true };
            schema.Columns.AddPk("Id").AutoIncrement = true;
            schema.Columns.Add<string>("Name");

            return schema;
        }
    }
}
