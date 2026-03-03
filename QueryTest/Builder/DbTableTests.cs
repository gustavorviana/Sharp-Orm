using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Builder.Tables;
using SharpOrm.Connection;

namespace QueryTest.Builder
{
	public class DbTableOfTTests : DbMockTest
    {
        public override Task InitializeAsync()
        {
            Manager.Management = ConnectionManagement.CloseOnManagerDispose;
            return base.InitializeAsync();
        }

        [Fact]
		public void Constructor_WithOrmTable_ShouldSetName()
		{
			Connection.OnExecuteNonQuery = _ => 0;
			var builder = new TableBuilder("MyTable", true);
			builder.AddColumn("Id", typeof(int));
			using var ormTable = DbTable.Create(builder.GetSchema(), Manager);
			using var table = new DbTable<TestTable>(ormTable);

			Assert.Equal(ormTable.DbName, table.Name);
			Assert.Same(ormTable, table.OrmTable);
		}

		[Fact]
		public void GetQuery_ShouldReturnTypedQuery()
		{
			Connection.OnExecuteNonQuery = _ => 0;
			var builder = new TableBuilder("MyTable", true);
			builder.AddColumn("Id", typeof(int));
			using var ormTable = DbTable.Create(builder.GetSchema(), Manager);
			using var table = new DbTable<TestTable>(ormTable);

			using var query = table.GetQuery();
			Assert.NotNull(query);
			Assert.IsType<Query<TestTable>>(query);
		}

		[Fact]
		public void Dispose_CanBeCalledMultipleTimes_WithoutThrowing()
		{
			Connection.OnExecuteNonQuery = _ => 0;
			var builder = new TableBuilder("MyTable", true);
			builder.AddColumn("Id", typeof(int));
			var ormTable = DbTable.Create(builder.GetSchema(), Manager);
			var table = new DbTable<TestTable>(ormTable);

			table.Dispose();
			table.Dispose();
		}

		[Fact]
		public void CreateTempTable_WithQuery_ShouldCreateDbTableOfT()
		{
			Connection.OnExecuteNonQuery = _ => 0;
			var query = new Query(new DbName("SourceTable"), Manager);
			query.Select("Id", "Name");
			query.Where(new SqlExpression("1!=1"));

			using var table = DbTable<TestTable>.CreateTempTable(query);

			Assert.NotNull(table);
			Assert.NotNull(table.Name);
			Assert.NotNull(table.OrmTable);
		}
	}

	public class DbTableTests : DbMockTest
	{
		[Fact]
		public void Create_WithNullManager_ShouldThrowArgumentNullException()
		{
			var builder = new TableBuilder("T", true);
			builder.AddColumn("Id", typeof(int));
			var schema = builder.GetSchema();

			Assert.Throws<ArgumentNullException>(() => DbTable.Create(schema, null));
		}

		[Fact]
		public void Create_WithEmptySchemaNoBasedQuery_ShouldThrowInvalidOperationException()
		{
			var schema = new TableSchema("EmptyTable");

			Connection.OnExecuteNonQuery = _ => 0;
			using var manager = new ConnectionManager(Config, Connection) { Management = ConnectionManagement.CloseOnManagerDispose };

			Assert.Throws<InvalidOperationException>(() => DbTable.Create(schema, manager));
		}

		[Fact]
		public void OpenIfExists_WithNullManager_ShouldThrowArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() => DbTable.OpenIfExists("SomeTable", null));
		}

		[Fact]
		public void Create_WithValidSchemaAndManager_ShouldReturnDbTable()
        {
            Manager.Management = ConnectionManagement.CloseOnManagerDispose;
            Connection.OnExecuteNonQuery = _ => 0;
			var builder = new TableBuilder("TestTable", true);
			builder.AddColumn("Id", typeof(int));

			using var table = DbTable.Create(builder.GetSchema(), Manager);

			Assert.NotNull(table);
			Assert.NotNull(table.DbName);
			Assert.StartsWith("#", table.DbName.Name);
			Assert.EndsWith("TestTable", table.DbName.Name);
		}

		[Fact]
		public void GetQuery_ShouldReturnQueryWithCorrectTableName()
        {
            Manager.Management = ConnectionManagement.CloseOnManagerDispose;
            Connection.OnExecuteNonQuery = _ => 0;
			var builder = new TableBuilder("TestTable", true);
			builder.AddColumn("Id", typeof(int));
			using var table = DbTable.Create(builder.GetSchema(), Manager);

			using var query = table.GetQuery();

			Assert.NotNull(query);
            Assert.StartsWith("#", table.DbName.Name);
            Assert.EndsWith("TestTable", table.DbName.Name);
        }

		[Fact]
		public void GetQuery_Generic_ShouldReturnTypedQuery()
        {
            Manager.Management = ConnectionManagement.CloseOnManagerDispose;
            Connection.OnExecuteNonQuery = _ => 0;
			var builder = new TableBuilder("TestTable", true);
			builder.AddColumn("Id", typeof(int));
			using var table = DbTable.Create(builder.GetSchema(), Manager);

			using var query = table.GetQuery<TestTable>();

			Assert.NotNull(query);
			Assert.IsType<Query<TestTable>>(query);
		}

		[Fact]
		public void ToString_TemporaryTable_ShouldContainTemporary()
        {
            Manager.Management = ConnectionManagement.CloseOnManagerDispose;
            Connection.OnExecuteNonQuery = _ => 0;
			var builder = new TableBuilder("TempTable", true);
			builder.AddColumn("Id", typeof(int));
			using var table = DbTable.Create(builder.GetSchema(), Manager);

			var result = table.ToString();

			Assert.Contains("Temporary", result);
			Assert.Contains("TempTable", result);
		}

		[Fact]
		public void Dispose_WhenTemporary_ShouldDropTable()
        {
            Manager.Management = ConnectionManagement.CloseOnManagerDispose;
            var dropCalled = false;
			Connection.OnExecuteNonQuery = cmd =>
			{
				if (cmd?.Contains("DROP", StringComparison.OrdinalIgnoreCase) == true)
					dropCalled = true;
				return 0;
			};
			var builder = new TableBuilder("DisposeTemp", true);
			builder.AddColumn("Id", typeof(int));
			var table = DbTable.Create(builder.GetSchema(), Manager);
			table.Dispose();
			Assert.True(dropCalled);
		}
	}
}
