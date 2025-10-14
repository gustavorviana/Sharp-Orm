using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Builder.Grammars.Table.Constraints;
using SharpOrm.Builder.Tables;

namespace QueryTest.Builder
{
    public class TableBuilderTest : DbMockTest
    {
        [Fact]
        public void SetName_ShouldSetTableName()
        {
            // Arrange
            var builder = new TableBuilder();
            const string expectedName = "MyTable";

            // Act
            builder.SetName(expectedName);
            var schema = builder.GetSchema();

            // Assert
            Assert.Equal(expectedName, schema.Name);
        }

        [Fact]
        public void AddColumn_ShouldAddColumnToSchema()
        {
            // Arrange
            var builder = new TableBuilder();
            builder.SetName("TestTable");

            // Act
            builder.AddColumn("TestColumn", typeof(string));
            var schema = builder.GetSchema();

            // Assert
            Assert.Single(schema.Columns);
            Assert.Equal("TestColumn", schema.Columns[0].ColumnName);
            Assert.Equal(typeof(string), schema.Columns[0].DataType);
        }

        [Fact]
        public void AddColumn_WithNullType_ShouldThrowArgumentNullException()
        {
            // Arrange
            var builder = new TableBuilder();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.AddColumn("TestColumn", null));
        }

        [Fact]
        public void AddColumn_AfterBuild_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var builder = new TableBuilder();
            builder.SetName("TestTable");
            builder.GetSchema(); // Build the schema

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => builder.AddColumn("NewColumn", typeof(string)));
        }

        [Fact]
        public void AddColumn_WithBasedQuery_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var builder = new TableBuilder();
            builder.SetBasedTable("SourceTable");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => builder.AddColumn("TestColumn", typeof(string)));
        }

        [Fact]
        public void HasKey_ShouldAddPrimaryKeyConstraint()
        {
            // Arrange
            var builder = new TableBuilder();
            builder.SetName("TestTable");
            builder.AddColumn("Id", typeof(int));

            // Act
            builder.HasKey("Id");
            var schema = builder.GetSchema();

            // Assert
            Assert.Single(schema.Constraints);
            Assert.IsType<PrimaryKeyConstraint>(schema.Constraints[0]);
            var pk = (PrimaryKeyConstraint)schema.Constraints[0];
            Assert.Equal("Id", pk.Columns[0]);
        }

        [Fact]
        public void HasKey_WithNonExistentColumn_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var builder = new TableBuilder();
            builder.SetName("TestTable");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => builder.HasKey("NonExistentColumn"));
        }

        [Fact]
        public void HasKey_ShouldMakeColumnRequired()
        {
            // Arrange
            var builder = new TableBuilder();
            builder.SetName("TestTable");
            builder.AddColumn("Id", typeof(int));

            // Act
            builder.HasKey("Id");
            var schema = builder.GetSchema();

            // Assert
            Assert.False(schema.Columns[0].AllowDBNull);
        }

        [Fact]
        public void HasUnique_ShouldAddUniqueConstraint()
        {
            // Arrange
            var builder = new TableBuilder();
            builder.SetName("TestTable");
            builder.AddColumn("Email", typeof(string));

            // Act
            builder.HasUnique("Email");
            var schema = builder.GetSchema();

            // Assert
            Assert.Single(schema.Constraints);
            Assert.IsType<UniqueConstraint>(schema.Constraints[0]);
        }

        [Fact]
        public void HasUnique_WithConstraintName_ShouldSetConstraintName()
        {
            // Arrange
            var builder = new TableBuilder();
            builder.SetName("TestTable");
            builder.AddColumn("Email", typeof(string));
            const string constraintName = "UQ_Email";

            // Act
            builder.HasUnique("Email", constraintName);
            var schema = builder.GetSchema();

            // Assert
            var constraint = (UniqueConstraint)schema.Constraints[0];
            Assert.Equal(constraintName, constraint.Name);
        }

        [Fact]
        public void HasUnique_WithMultipleColumns_ShouldAddCompositeUnique()
        {
            // Arrange
            var builder = new TableBuilder();
            builder.SetName("TestTable");
            builder.AddColumn("FirstName", typeof(string));
            builder.AddColumn("LastName", typeof(string));

            // Act
            builder.HasUnique(new[] { "FirstName", "LastName" });
            var schema = builder.GetSchema();

            // Assert
            Assert.Single(schema.Constraints);
            var constraint = (UniqueConstraint)schema.Constraints[0];
            Assert.Equal(2, constraint.Columns.Length);
        }

        [Fact]
        public void HasForeignKey_ShouldAddForeignKeyConstraint()
        {
            // Arrange
            var builder = new TableBuilder();
            builder.SetName("Orders");
            builder.AddColumn("CustomerId", typeof(int));

            // Act
            builder.HasForeignKey("CustomerId", "Customers", "Id");
            var schema = builder.GetSchema();

            // Assert
            Assert.Single(schema.Constraints);
            Assert.IsType<ForeignKeyConstraint>(schema.Constraints[0]);
            var fk = (ForeignKeyConstraint)schema.Constraints[0];
            Assert.Equal("CustomerId", fk.ForeignKeyColumn);
            Assert.Equal("Customers", fk.ReferencedTable);
            Assert.Equal("Id", fk.ReferencedColumn);
        }

        [Fact]
        public void HasCheck_ShouldAddCheckConstraint()
        {
            // Arrange
            var builder = new TableBuilder();
            builder.SetName("Products");
            builder.AddColumn("Price", typeof(decimal));
            const string checkExpression = "Price > 0";
            const string constraintName = "CHK_Price_Positive";

            // Act
            builder.HasCheck(checkExpression, constraintName);
            var schema = builder.GetSchema();

            // Assert
            Assert.Single(schema.Constraints);
            Assert.IsType<CheckConstraint>(schema.Constraints[0]);
            var check = (CheckConstraint)schema.Constraints[0];
            Assert.Equal(checkExpression, check.Expression);
            Assert.Equal(constraintName, check.Name);
        }

        [Fact]
        public void HasIndex_ShouldAddIndexDefinition()
        {
            // Arrange
            var builder = new TableBuilder();
            builder.SetName("Users");
            builder.AddColumn("Email", typeof(string));

            // Act
            builder.HasIndex("Email");
            var schema = builder.GetSchema();

            // Assert
            Assert.Single(schema.Indexes);
            Assert.Equal("Email", schema.Indexes[0].Columns[0]);
        }

        [Fact]
        public void HasIndex_WithMultipleColumns_ShouldAddCompositeIndex()
        {
            // Arrange
            var builder = new TableBuilder();
            builder.SetName("Orders");
            builder.AddColumn("CustomerId", typeof(int));
            builder.AddColumn("OrderDate", typeof(DateTime));

            // Act
            builder.HasIndex("CustomerId", "OrderDate");
            var schema = builder.GetSchema();

            // Assert
            Assert.Single(schema.Indexes);
            Assert.Equal(2, schema.Indexes[0].Columns.Length);
            Assert.Equal("CustomerId", schema.Indexes[0].Columns[0]);
            Assert.Equal("OrderDate", schema.Indexes[0].Columns[1]);
        }

        [Fact]
        public void Ignore_ShouldRemoveColumnFromSchema()
        {
            // Arrange
            var builder = new TableBuilder();
            builder.SetName("TestTable");
            builder.AddColumn("Column1", typeof(string));
            builder.AddColumn("Column2", typeof(string));

            // Act
            builder.Ignore("Column1");
            var schema = builder.GetSchema();

            // Assert
            Assert.Single(schema.Columns);
            Assert.Equal("Column2", schema.Columns[0].ColumnName);
        }

        [Fact]
        public void Ignore_IgnoredColumn_CannotBeAdded()
        {
            // Arrange
            var builder = new TableBuilder();
            builder.SetName("TestTable");
            builder.Ignore("IgnoredColumn");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => builder.AddColumn("IgnoredColumn", typeof(string)));
        }

        [Fact]
        public void SetBasedTable_ShouldSetBasedQueryMetadata()
        {
            // Arrange
            var builder = new TableBuilder();
            builder.SetName("ViewTable");

            // Act
            builder.SetBasedTable("SourceTable", "Column1", "Column2");
            var schema = builder.GetSchema();

            // Assert
            Assert.True(schema.Metadata.HasKey(Metadatas.BasedQuery));
        }

        [Fact]
        public void SetBasedQuery_ShouldSetBasedQueryMetadata()
        {
            // Arrange
            var builder = new TableBuilder();
            builder.SetName("ViewTable");
            var query = Query.ReadOnly("SourceTable").Select("Column1");

            // Act
            builder.SetBasedQuery(query);
            var schema = builder.GetSchema();

            // Assert
            Assert.True(schema.Metadata.HasKey(Metadatas.BasedQuery));
            var basedQuery = schema.Metadata.GetOrDefault<QueryBase>(Metadatas.BasedQuery);
            Assert.NotNull(basedQuery);
        }

        [Fact]
        public void GetSchema_MultipleCalls_ShouldReturnSameInstance()
        {
            // Arrange
            var builder = new TableBuilder();
            builder.SetName("TestTable");
            builder.AddColumn("Id", typeof(int));

            // Act
            var schema1 = builder.GetSchema();
            var schema2 = builder.GetSchema();

            // Assert
            Assert.Same(schema1, schema2);
        }

        [Fact]
        public void AddConstraint_ShouldAddCustomConstraint()
        {
            // Arrange
            var builder = new TableBuilder();
            builder.SetName("TestTable");
            var constraint = new CheckConstraint("TestTable", "1=1", "CHK_Test");

            // Act
            builder.AddConstraint(constraint);
            var schema = builder.GetSchema();

            // Assert
            Assert.Single(schema.Constraints);
            Assert.Same(constraint, schema.Constraints[0]);
        }

        [Fact]
        public void TableBuilderGeneric_HasKey_WithExpression_ShouldAddPrimaryKey()
        {
            // Arrange
            var registry = Config.Translation;
            var builder = new TableBuilder<Customer>(registry);
            builder.SetName("Customers");

            // Act
            builder.HasKey(x => x.Id);
            var schema = builder.GetSchema();

            // Assert
            Assert.Single(schema.Constraints);
            Assert.IsType<PrimaryKeyConstraint>(schema.Constraints[0]);
        }

        [Fact]
        public void TableBuilderGeneric_AddColumn_WithExpression_ShouldAddColumn()
        {
            // Arrange
            var registry = Config.Translation;
            var builder = new TableBuilder<Customer>(registry);
            builder.SetName("Customers");

            // Act
            builder.AddColumn(x => x.Name);
            var schema = builder.GetSchema();

            // Assert
            Assert.Contains(schema.Columns, c => c.ColumnName == "Name");
        }

        [Fact]
        public void TableBuilderGeneric_HasUnique_WithExpression_ShouldAddUniqueConstraint()
        {
            // Arrange
            var registry = Config.Translation;
            var builder = new TableBuilder<Customer>(registry);
            builder.SetName("Customers");

            // Act
            builder.HasUnique(x => x.Email);
            var schema = builder.GetSchema();

            // Assert
            Assert.Single(schema.Constraints);
            Assert.IsType<UniqueConstraint>(schema.Constraints[0]);
        }

        [Fact]
        public void TableBuilderGeneric_Ignore_WithExpression_ShouldIgnoreColumn()
        {
            // Arrange
            var registry = Config.Translation;
            var builder = new TableBuilder<Address>(registry);
            builder.SetName("Address");
            builder.AddColumn(x => x.City);
            builder.AddColumn(x => x.Street);

            // Act
            builder.Ignore(x => x.Street);
            var schema = builder.GetSchema();

            // Assert
            Assert.DoesNotContain(schema.Columns, c => c.ColumnName == "Street");
        }

        [Fact]
        public void TableBuilderGeneric_HasIndex_WithExpression_ShouldAddIndex()
        {
            // Arrange
            var registry = Config.Translation;
            var builder = new TableBuilder<Customer>(registry);
            builder.SetName("Customers");

            // Act
            builder.HasIndex(x => x.Email);
            var schema = builder.GetSchema();

            // Assert
            Assert.Single(schema.Indexes);
        }

        [Fact]
        public void TableBuilderGeneric_SetBasedTable_WithExpression_ShouldSetBasedQuery()
        {
            // Arrange
            var registry = Config.Translation;
            var builder = new TableBuilder<Customer>(registry);
            builder.SetName("CustomerView");

            // Act
            builder.SetBasedTable("Customers", x => new { x.Id, x.Name });
            var schema = builder.GetSchema();

            // Assert
            Assert.True(schema.Metadata.HasKey(Metadatas.BasedQuery));
        }

        [Fact]
        public void AddColumn_FromColumnInfo_ShouldConfigureColumn()
        {
            // Arrange
            var registry = Config.Translation;
            var tableInfo = registry.GetTable(typeof(Customer));
            var columnInfo = tableInfo.Columns.Find("Name");
            var builder = new TableBuilder();
            builder.SetName("TestTable");

            // Act
            builder.AddColumn(columnInfo);
            var schema = builder.GetSchema();

            // Assert
            Assert.Contains(schema.Columns, c => c.ColumnName == "Name");
        }

        [Fact]
        public void Metadata_ShouldBeAccessible()
        {
            // Arrange
            var builder = new TableBuilder();
            builder.SetName("TestTable");

            // Act
            var metadata = builder.Metadata;

            // Assert
            Assert.NotNull(metadata);
        }

        [Fact]
        public void Schema_Clone_ShouldCreateNewInstance()
        {
            // Arrange
            var builder = new TableBuilder();
            builder.SetName("TestTable");
            builder.AddColumn("Id", typeof(int));
            var schema = builder.GetSchema();

            // Act
            var cloned = schema.Clone();

            // Assert
            Assert.NotSame(schema, cloned);
            Assert.Equal(schema.Name, cloned.Name);
            Assert.Equal(schema.Columns.Count, cloned.Columns.Count);
        }
    }
}
