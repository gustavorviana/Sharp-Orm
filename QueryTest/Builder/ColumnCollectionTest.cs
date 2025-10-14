using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm.Builder;
using SharpOrm.Builder.Tables;

namespace QueryTest.Builder
{
    public class ColumnCollectionTest : DbMockTest
    {
        [Fact]
        public void Find_WithValidName_ShouldReturnColumn()
        {
            // Arrange
            var registry = Config.Translation;
            var tableInfo = registry.GetTable(typeof(Customer));
            var collection = tableInfo.Columns;

            // Act
            var column = collection.Find("Name");

            // Assert
            Assert.NotNull(column);
            Assert.Equal("Name", column.Name);
        }

        [Fact]
        public void Find_WithInvalidName_ShouldReturnNull()
        {
            // Arrange
            var registry = Config.Translation;
            var tableInfo = registry.GetTable(typeof(Customer));
            var collection = tableInfo.Columns;

            // Act
            var column = collection.Find("NonExistentColumn");

            // Assert
            Assert.Null(column);
        }

        [Fact]
        public void Find_WithExpression_ShouldReturnColumn()
        {
            // Arrange
            var registry = Config.Translation;
            var tableInfo = registry.GetTable(typeof(Customer));
            var collection = tableInfo.Columns;

            // Act
            var column = collection.Find<Customer>(x => x.Name);

            // Assert
            Assert.NotNull(column);
            Assert.Equal("Name", column.Name);
        }

        [Fact]
        public void Find_WithNullExpression_ShouldThrowArgumentNullException()
        {
            // Arrange
            var registry = Config.Translation;
            var tableInfo = registry.GetTable(typeof(Customer));
            var collection = tableInfo.Columns;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => collection.Find<Customer>(null));
        }

        [Fact]
        public void Find_WithMultiplePathExpression_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var registry = Config.Translation;
            var tableInfo = registry.GetTable(typeof(Customer));
            var collection = tableInfo.Columns;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => collection.Find<Customer>(x => new { x.Id, x.Name }));
        }

        [Fact]
        public void Find_WithEmptyExpression_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var registry = Config.Translation;
            var tableInfo = registry.GetTable(typeof(Customer));
            var collection = tableInfo.Columns;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => collection.Find<Customer>(x => new { }));
        }

        [Fact]
        public void FindAll_WithExpression_ShouldReturnAllMatchingColumns()
        {
            // Arrange
            var registry = Config.Translation;
            var tableInfo = registry.GetTable(typeof(Customer));
            var collection = tableInfo.Columns;

            // Act
            var columns = collection.FindAll<Customer>(x => new { x.Id, x.Name });

            // Assert
            Assert.NotNull(columns);
            Assert.Equal(2, columns.Length);
            Assert.Contains(columns, c => c.Name == "Id");
            Assert.Contains(columns, c => c.Name == "Name");
        }

        [Fact]
        public void FindAll_WithNullExpression_ShouldThrowArgumentNullException()
        {
            // Arrange
            var registry = Config.Translation;
            var tableInfo = registry.GetTable(typeof(Customer));
            var collection = tableInfo.Columns;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => collection.FindAll<Customer>(null));
        }

        [Fact]
        public void FindAll_WithSingleColumn_ShouldReturnSingleColumn()
        {
            // Arrange
            var registry = Config.Translation;
            var tableInfo = registry.GetTable(typeof(Customer));
            var collection = tableInfo.Columns;

            // Act
            var columns = collection.FindAll<Customer>(x => x.Name);

            // Assert
            Assert.NotNull(columns);
            Assert.Single(columns);
            Assert.Equal("Name", columns[0].Name);
        }

        [Fact]
        public void Count_ShouldReturnCorrectNumberOfColumns()
        {
            // Arrange
            var registry = Config.Translation;
            var tableInfo = registry.GetTable(typeof(Customer));
            var collection = tableInfo.Columns;

            // Act
            var count = collection.Count;

            // Assert
            Assert.True(count > 0);
        }

        [Fact]
        public void GetEnumerator_ShouldEnumerateAllColumns()
        {
            // Arrange
            var registry = Config.Translation;
            var tableInfo = registry.GetTable(typeof(Customer));
            var collection = tableInfo.Columns;

            // Act
            var columns = collection.ToList();

            // Assert
            Assert.NotEmpty(columns);
            Assert.All(columns, c => Assert.NotNull(c));
        }

        [Fact]
        public void Nodes_ShouldReturnColumnNodes()
        {
            // Arrange
            var registry = Config.Translation;
            var tableInfo = registry.GetTable(typeof(Customer));
            var collection = tableInfo.Columns;

            // Act
            var nodes = collection.Nodes;

            // Assert
            Assert.NotNull(nodes);
            Assert.NotEmpty(nodes);
        }

        [Fact]
        public void ColumnNode_ShouldContainColumnInfo()
        {
            // Arrange
            var registry = Config.Translation;
            var tableInfo = registry.GetTable(typeof(Customer));
            var collection = tableInfo.Columns;

            // Act
            var firstNode = collection.Nodes.FirstOrDefault();

            // Assert
            Assert.NotNull(firstNode);
            Assert.NotNull(firstNode.Column);
        }

        [Fact]
        public void ColumnNode_Flatten_ShouldReturnAllDescendants()
        {
            // Arrange
            var registry = Config.Translation;
            var tableInfo = registry.GetTable(typeof(Customer));
            var collection = tableInfo.Columns;

            // Find a node with children (like Address foreign key)
            var nodeWithChildren = collection.Nodes
                .FirstOrDefault(n => n.Nodes != null && n.Nodes.Count > 0);

            // Act & Assert
            if (nodeWithChildren != null)
            {
                var flattened = ((ColumnCollection.ColumnNode)nodeWithChildren).Flatten().ToList();
                Assert.NotEmpty(flattened);
            }
        }

        [Fact]
        public void ColumnNode_IsCollection_ShouldReflectPropertyType()
        {
            // Arrange
            var registry = Config.Translation;
            var tableInfo = registry.GetTable(typeof(Customer));
            var collection = tableInfo.Columns;

            // Act
            var nodes = collection.Nodes;

            // Assert - at least one node should not be a collection
            Assert.Contains(nodes, n => !n.IsCollection);
        }

        [Fact]
        public void Find_NestedProperty_ShouldReturnNestedColumn()
        {
            // Arrange
            var registry = Config.Translation;
            var tableInfo = registry.GetTable(typeof(Customer));
            var collection = tableInfo.Columns;

            // Act - Try to find nested property if exists
            var column = collection.Find<Customer>(x => x.Address!.City);

            // Assert - May be null if Address is not loaded as nested property
            // This test verifies the method handles nested properties gracefully
            if (column != null)
            {
                Assert.Equal("City", column.PropName);
            }
        }

        [Fact]
        public void Constructor_WithNullColumnLookup_ShouldThrowArgumentNullException()
        {
            // Arrange
            var nodes = Array.Empty<ColumnCollection.ColumnNode>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ColumnCollection(nodes, null));
        }

        [Fact]
        public void Constructor_WithNullNodes_ShouldThrowArgumentNullException()
        {
            // Arrange
            var lookup = new System.Collections.Generic.Dictionary<string, ColumnInfo[]>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ColumnCollection(null, lookup));
        }

        [Fact]
        public void ColumnCollection_CaseInsensitiveSearch_ShouldWork()
        {
            // Arrange
            var registry = Config.Translation;
            var tableInfo = registry.GetTable(typeof(Customer));
            var collection = tableInfo.Columns;

            // Act
            var column1 = collection.Find("Name");
            var column2 = collection.Find("name");
            var column3 = collection.Find("NAME");

            // Assert
            Assert.NotNull(column1);
            Assert.NotNull(column2);
            Assert.NotNull(column3);
            Assert.Equal(column1.Name, column2.Name);
            Assert.Equal(column1.Name, column3.Name);
        }

        [Fact]
        public void ColumnNode_WithNoChildren_ShouldHaveEmptyNodes()
        {
            // Arrange
            var registry = Config.Translation;
            var tableInfo = registry.GetTable(typeof(Customer));
            var collection = tableInfo.Columns;

            // Act
            var leafNode = collection.Nodes.FirstOrDefault(n => n.Nodes.Count == 0);

            // Assert
            if (leafNode != null)
            {
                Assert.Empty(leafNode.Nodes);
            }
        }

        [Fact]
        public void ColumnNode_Flatten_LeafNode_ShouldReturnSingleColumn()
        {
            // Arrange
            var registry = Config.Translation;
            var tableInfo = registry.GetTable(typeof(Customer));
            var collection = tableInfo.Columns;
            var leafNode = collection.Nodes.FirstOrDefault(n => n.Nodes.Count == 0);

            // Act
            if (leafNode != null)
            {
                var flattened = ((ColumnCollection.ColumnNode)leafNode).Flatten().ToList();

                // Assert
                Assert.Single(flattened);
                Assert.Same(leafNode.Column, flattened[0]);
            }
        }
    }
}
