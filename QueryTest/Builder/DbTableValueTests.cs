using BaseTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Builder.Tables;
using SharpOrm.Connection;

namespace QueryTest.Builder
{
	public class DbTableValueTests : DbMockTest
    {
        public override Task InitializeAsync()
        {
            Manager.Management = ConnectionManagement.CloseOnManagerDispose;
            return base.InitializeAsync();
        }

        [Fact]
		public void Constructor_WithTableAndColumn_ShouldSetTableAndColumn()
		{
			Connection.OnExecuteNonQuery = _ => 0;
			var builder = new TableBuilder("ValueTable", true);
			builder.AddColumn<int>("Value");
			using var ormTable = DbTable.Create(builder.GetSchema(), Manager);
			using var tableValue = new DbTableValue<int>(ormTable, "Value");

			Assert.Equal(ormTable.DbName, tableValue.Table);
			Assert.Equal("Value", tableValue.Column);
		}

		[Fact]
		public void FromValues_WithManagerAndValues_ShouldCreateDbTableValue()
		{
			Connection.OnExecuteNonQuery = _ => 0;
			Connection.OnQueryFallback = _ => new BaseTest.Mock.MockDataReader();

			using var tableValue = DbTableValue<int>.FromValues(Manager, new[] { 10, 20, 30 });

			Assert.NotNull(tableValue);
			Assert.Equal("Int32", tableValue.Column);
			Assert.NotNull(tableValue.Table);
		}

		[Fact]
		public void InsertInto_WithNullOrEmptyTable_ShouldThrowArgumentException()
		{
			Connection.OnExecuteNonQuery = _ => 0;
			var builder = new TableBuilder("ValueTable", true);
			builder.AddColumn<int>("Value");
			using var ormTable = DbTable.Create(builder.GetSchema(), Manager);
			using var tableValue = new DbTableValue<int>(ormTable, "Value");

			Assert.Throws<ArgumentException>(() => tableValue.InsertInto(""));
			Assert.Throws<ArgumentException>(() => tableValue.InsertInto("   "));
			Assert.Throws<ArgumentException>(() => tableValue.InsertInto(null));
		}

		[Fact]
		public void InsertInto_AddWithEmptyTargetColumn_ShouldThrowArgumentException()
		{
			Connection.OnExecuteNonQuery = _ => 0;
			var builder = new TableBuilder("ValueTable", true);
			builder.AddColumn<int>("Value");
			using var ormTable = DbTable.Create(builder.GetSchema(), Manager);
			using var tableValue = new DbTableValue<int>(ormTable, "Value");

			var builderInsert = tableValue.InsertInto("TargetTable");
			Assert.Throws<ArgumentException>(() => builderInsert.Add(""));
			Assert.Throws<ArgumentException>(() => builderInsert.Add("  "));
			Assert.Throws<ArgumentException>(() => builderInsert.Add("Col", ""));
			Assert.Throws<ArgumentException>(() => builderInsert.Add("", "SourceCol"));
		}

		[Fact]
		public void InsertInto_AddWithEmptySourceColumn_ShouldThrowArgumentException()
		{
			Connection.OnExecuteNonQuery = _ => 0;
			var builder = new TableBuilder("ValueTable", true);
			builder.AddColumn<int>("Value");
			using var ormTable = DbTable.Create(builder.GetSchema(), Manager);
			using var tableValue = new DbTableValue<int>(ormTable, "Value");

			var builderInsert = tableValue.InsertInto("TargetTable");
			Assert.Throws<ArgumentException>(() => builderInsert.Add("TargetCol", ""));
			Assert.Throws<ArgumentException>(() => builderInsert.Add("TargetCol", "   "));
		}

		[Fact]
		public void InsertInto_ExecuteWithoutAdd_ShouldThrowInvalidOperationException()
		{
			Connection.OnExecuteNonQuery = _ => 0;
			var builder = new TableBuilder("ValueTable", true);
			builder.AddColumn<int>("Value");
			using var ormTable = DbTable.Create(builder.GetSchema(), Manager);
			using var tableValue = new DbTableValue<int>(ormTable, "Value");

			var builderInsert = tableValue.InsertInto("TargetTable");

			Assert.Throws<InvalidOperationException>(() => builderInsert.Execute());
		}

		[Fact]
		public void InsertInto_AddThenExecute_ShouldCallExecuteNonQuery()
		{
			var executed = false;
			Connection.OnExecuteNonQuery = cmd =>
			{
				if (cmd?.Contains("INSERT INTO", StringComparison.OrdinalIgnoreCase) == true)
					executed = true;
				return 0;
			};
			var builder = new TableBuilder("ValueTable", true);
			builder.AddColumn<int>("Value");
			using var ormTable = DbTable.Create(builder.GetSchema(), Manager);
			using var tableValue = new DbTableValue<int>(ormTable, "Value");

			tableValue.InsertInto("TargetTable").Add("TargetCol").Execute();

			Assert.True(executed);
		}

		[Fact]
		public void CreateEqualsExpression_WithStringColumn_ShouldReturnSqlExpression()
		{
			Connection.OnExecuteNonQuery = _ => 0;
			Connection.OnQueryFallback = _ => new BaseTest.Mock.MockDataReader();
			var builder = new TableBuilder("ValueTable", true);
			builder.AddColumn<int>("Value");
			using var ormTable = DbTable.Create(builder.GetSchema(), Manager);
			using var tableValue = new DbTableValue<int>(ormTable, "Value");

			var expression = tableValue.CreateEqualsExpression("OtherColumn");

			Assert.NotNull(expression);
			Assert.False(expression.IsEmpty);
		}

		[Fact]
		public void CreateEqualsExpression_WithColumn_ShouldReturnSqlExpression()
		{
			Connection.OnExecuteNonQuery = _ => 0;
			Connection.OnQueryFallback = _ => new BaseTest.Mock.MockDataReader();
			var builder = new TableBuilder("ValueTable", true);
			builder.AddColumn<int>("Value");
			using var ormTable = DbTable.Create(builder.GetSchema(), Manager);
			using var tableValue = new DbTableValue<int>(ormTable, "Value");

			var expression = tableValue.CreateEqualsExpression((Column)"OtherColumn");

			Assert.NotNull(expression);
			Assert.False(expression.IsEmpty);
		}

		[Fact]
		public void Dispose_CanBeCalledMultipleTimes_WithoutThrowing()
		{
			Connection.OnExecuteNonQuery = _ => 0;
			var builder = new TableBuilder("ValueTable", true);
			builder.AddColumn<int>("Value");
			var ormTable = DbTable.Create(builder.GetSchema(), Manager);
			var tableValue = new DbTableValue<int>(ormTable, "Value");

			tableValue.Dispose();
			tableValue.Dispose();
		}

		[Fact]
		public void InsertInto_ReturnsFluentBuilder()
		{
			Connection.OnExecuteNonQuery = _ => 0;
			var builder = new TableBuilder("ValueTable", true);
			builder.AddColumn<int>("Value");
			using var ormTable = DbTable.Create(builder.GetSchema(), Manager);
			using var tableValue = new DbTableValue<int>(ormTable, "Value");

			var result = tableValue.InsertInto("Target").Add("Col1").Add("Col2", 42);

			Assert.NotNull(result);
			Assert.IsAssignableFrom<IInsertIntoBuilder>(result);
		}
	}
}
