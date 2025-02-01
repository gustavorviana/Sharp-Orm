using BaseTest.Models;
using BaseTest.Utils;
using DbRunTest.Fixtures;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using System.Data;
using System.Data.Common;
using Xunit.Abstractions;

namespace DbRunTest.BaseTests
{
    public abstract class DbTableTest<T> : DbTestBase, IClassFixture<UnsafeDbFixture<T>> where T : DbConnection, new()
    {
        public DbTableTest(ITestOutputHelper output, UnsafeDbFixture<T> connection) : base(output, connection)
        {
            connection.Creator.Management = ConnectionManagement.CloseOnManagerDispose;
            connection.Manager.Management = ConnectionManagement.CloseOnManagerDispose;
        }

        [Fact]
        public void CreateByColumnTest()
        {
            DbTable.Create(GetSchema(), Manager).Dispose();
        }

        [Fact]
        public void CreateEmptyByAnother()
        {
            InsertAddressValue();
            using var table = DbTable.Create("MyTestTable", true, [Column.All], "Address", Manager);
            var expectedCols = GetTableColumns(new DbName("Address"), Manager);

            Assert.Equal(expectedCols, GetTableColumns(table.DbName, Manager));
            Assert.Equal(0, table.GetQuery().Count());
        }

        [Fact]
        public void CreateByAnother()
        {
            var expectedRows = InsertAddressValue();

            using var q = new Query("Address", Manager);
            q.OrderBy("Id");
            q.Offset = 1;
            using var table = DbTable.Create("MyTestTable", true, q);
            var expectedCols = GetTableColumns(new DbName("Address"), Manager);
            var rows = table.GetQuery().ReadRows();

            Assert.Single(rows);
            Assert.Equal(expectedCols, GetTableColumns(table.DbName, Manager));

            var cells = rows[0].Cells;
            Assert.True(((long)expectedRows.Cells[0]).Equals((long)cells[0]));
            Assert.Equal(expectedRows.Cells[1], cells[1]);
            Assert.Equal(expectedRows.Cells[2], cells[2]);
        }

        [Fact]
        public void CreateByClass()
        {
            using var table = DbTable.Create<Address>(true, manager: Manager);
            using var query = table.GetQuery();
            query.Limit = 0;

            var tableColumns = query.ReadTable().Columns.OfType<DataColumn>().Select(x => x.ColumnName).ToArray();
            var classColumns = Translation.GetTable(typeof(Address)).Columns.Select(x => x.Name).ToArray();

            Assert.Equal(tableColumns, classColumns);
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

        [Fact]
        public virtual void CheckExists()
        {
            var schema = GetSchema();
            using var table = DbTable.Create(schema, this.Manager);
            Assert.True(DbTable.Exists(table.DbName.Name, schema.Temporary, table.Manager), "DbTable.Exists(string, bool, ConnectionManager)");
            Assert.True(table.Exists(), "DbTable.Exists()");
        }

        [Fact]
        public void InsertData()
        {
            using var table = DbTable.Create(GetSchema(), this.Manager);
            var q = table.GetQuery();
            q.Insert(new Cell("name", "Richard"));
            q.Insert(new Cell("name", "Manuel"));
            Assert.Equal(2, q.Count());
        }

        [Fact]
        public void CreateTable()
        {
            var cols = new TableColumnCollection();
            cols.AddPk("Id");
            cols.Add<string>("Name");
            cols.Add<int>("Status").Unique = true;

            var schema = new TableSchema("MyTestTable", cols) { Temporary = true };
            using var table = DbTable.Create(schema, this.Manager);
        }

        [Fact]
        public void CreateTableMultiplePk()
        {
            var cols = new TableColumnCollection();
            cols.AddPk("Id");
            cols.AddPk("Id2");

            var schema = new TableSchema("MyTestTable", cols) { Temporary = true };
            using var table = DbTable.Create(schema, this.Manager);
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
