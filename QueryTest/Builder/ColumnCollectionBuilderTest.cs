using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm.Builder.Tables;

namespace QueryTest.Builder
{
    public class ColumnCollectionBuilderTest : DbMockTest
    {
        private SharpOrm.Builder.ColumnInfo GetColumnInfo(Type type, string propertyName)
        {
            var tableInfo = Config.Translation.GetTable(type);
            return tableInfo.Columns.Find(propertyName);
        }

        [Fact]
        public void Add_ShouldAddColumnToBuilder()
        {
            // Arrange
            var builder = new ColumnCollectionBuilder();
            var columnInfo = GetColumnInfo(typeof(Customer), "Name");

            // Act
            builder.Add(columnInfo);
            var collection = builder.Build();

            // Assert
            Assert.Single(collection);
            Assert.Equal("Name", collection.First().Name);
        }

        [Fact]
        public void Add_WithNullColumn_ShouldThrowArgumentNullException()
        {
            // Arrange
            var builder = new ColumnCollectionBuilder();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.Add(null));
        }

        [Fact]
        public void Add_MultipleColumns_ShouldAddAllColumns()
        {
            // Arrange
            var builder = new ColumnCollectionBuilder();
            var column1 = GetColumnInfo(typeof(Customer), "Name");
            var column2 = GetColumnInfo(typeof(Customer), "Id");
            var column3 = GetColumnInfo(typeof(Customer), "Email");

            // Act
            builder.Add(column1);
            builder.Add(column2);
            builder.Add(column3);
            var collection = builder.Build();

            // Assert
            Assert.Equal(3, collection.Count);
        }

        [Fact]
        public void Build_ShouldCreateColumnCollection()
        {
            // Arrange
            var builder = new ColumnCollectionBuilder();
            var column = GetColumnInfo(typeof(Customer), "Name");
            builder.Add(column);

            // Act
            var collection = builder.Build();

            // Assert
            Assert.NotNull(collection);
            Assert.IsType<ColumnCollection>(collection);
        }

        [Fact]
        public void Build_WithNoColumns_ShouldReturnEmptyCollection()
        {
            // Arrange
            var builder = new ColumnCollectionBuilder();

            // Act
            var collection = builder.Build();

            // Assert
            Assert.NotNull(collection);
            Assert.Empty(collection);
        }

        [Fact]
        public void BuilderNode_Add_ShouldAddChildNode()
        {
            // Arrange
            var builder = new ColumnCollectionBuilder();
            var parentColumn = GetColumnInfo(typeof(Customer), "Name");
            var childColumn = GetColumnInfo(typeof(Address), "City");

            // Act
            var parentNode = builder.Add(parentColumn);
            parentNode.Add(childColumn);
            var collection = builder.Build();

            // Assert
            var builtParent = collection.Nodes.First();
            Assert.Single(builtParent.Nodes);
            Assert.Equal("City", builtParent.Nodes.First().Column.Name);
        }

        [Fact]
        public void BuilderNode_Add_WithNullColumn_ShouldThrowArgumentNullException()
        {
            // Arrange
            var builder = new ColumnCollectionBuilder();
            var parentColumn = GetColumnInfo(typeof(Customer), "Name");
            var parentNode = builder.Add(parentColumn);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => parentNode.Add(null));
        }

        [Fact]
        public void BuilderNode_MultipleChildren_ShouldAddAllChildren()
        {
            // Arrange
            var builder = new ColumnCollectionBuilder();
            var parentColumn = GetColumnInfo(typeof(Customer), "Name");
            var child1 = GetColumnInfo(typeof(Address), "City");
            var child2 = GetColumnInfo(typeof(Address), "Street");

            // Act
            var parentNode = builder.Add(parentColumn);
            parentNode.Add(child1);
            parentNode.Add(child2);
            var collection = builder.Build();

            // Assert
            var builtParent = collection.Nodes.First();
            Assert.Equal(2, builtParent.Nodes.Count);
        }

        [Fact]
        public void Build_DuplicateColumnNames_ShouldHandleCorrectly()
        {
            // Arrange
            var builder = new ColumnCollectionBuilder();
            var column1 = GetColumnInfo(typeof(Customer), "Name");
            var column2 = GetColumnInfo(typeof(Address), "Name");

            // Act
            builder.Add(column1);
            builder.Add(column2);
            var collection = builder.Build();

            // Assert
            // Should find at least one column with name "Name"
            var found = collection.Find("Name");
            Assert.NotNull(found);
        }

        [Fact]
        public void BuilderNode_IsCollection_ShouldReflectColumnType()
        {
            // Arrange
            var builder = new ColumnCollectionBuilder();
            var scalarColumn = GetColumnInfo(typeof(Customer), "Name");

            // Act
            builder.Add(scalarColumn);
            var collection = builder.Build();

            // Assert
            var scalarNode = collection.Nodes.FirstOrDefault(n => n.Column.Name == "Name");
            Assert.False(scalarNode?.IsCollection ?? true);
        }

        [Fact]
        public void BuilderNode_ToString_ShouldReturnDescription()
        {
            // Arrange
            var builder = new ColumnCollectionBuilder();
            var column = GetColumnInfo(typeof(Customer), "Name");

            // Act
            var node = builder.Add(column);
            var result = node.ToString();

            // Assert
            Assert.NotNull(result);
            Assert.Contains("BuilderNode", result);
            Assert.Contains("Name", result);
        }

        [Fact]
        public void BuilderNode_Column_ShouldReturnCorrectColumnInfo()
        {
            // Arrange
            var builder = new ColumnCollectionBuilder();
            var column = GetColumnInfo(typeof(Customer), "Id");

            // Act
            var node = builder.Add(column);

            // Assert
            Assert.Same(column, ((ColumnCollectionBuilder.BuilderNode)node).Column);
        }

        [Fact]
        public void Add_CaseInsensitiveNames_ShouldBeFindable()
        {
            // Arrange
            var builder = new ColumnCollectionBuilder();
            var column1 = GetColumnInfo(typeof(Customer), "Name");

            // Act
            builder.Add(column1);
            var collection = builder.Build();

            // Assert
            // Both should be findable (case-insensitive lookup in ColumnCollection)
            Assert.NotNull(collection.Find("name"));
            Assert.NotNull(collection.Find("Name"));
            Assert.NotNull(collection.Find("NAME"));
        }

        [Fact]
        public void BuilderNode_Nodes_ShouldBeAccessible()
        {
            // Arrange
            var builder = new ColumnCollectionBuilder();
            var parentColumn = GetColumnInfo(typeof(Customer), "Name");
            var parentNode = (ColumnCollectionBuilder.BuilderNode)builder.Add(parentColumn);

            // Act & Assert
            Assert.NotNull(((ITreeNodes<ColumnCollectionBuilder.BuilderNode>)parentNode).Nodes);
            Assert.Empty(((ITreeNodes<ColumnCollectionBuilder.BuilderNode>)parentNode).Nodes);
        }
    }
}
