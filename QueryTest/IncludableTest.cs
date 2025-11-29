using BaseTest.Models;
using BaseTest.Utils;
using NSubstitute;
using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using SharpOrm.ForeignKey;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SharpOrm.Builder.Grammars.SqlServer;

namespace QueryTest
{
    public class IncludableTest
    {
        private readonly INodeCreationListener _listener;
        private readonly ForeignKeyRegister _register;

        public IncludableTest()
        {
            _listener = Substitute.For<INodeCreationListener>();
            _register = new ForeignKeyRegister(new TableInfo(typeof(Order), TranslationRegistry.Default), new DbName("Orders"), _listener);
        }

        [Fact]
        public void GetOrAddChild_Should_Create_Valid_Node_Test()
        {
            var property = _register.TableInfo.Type.GetProperty(nameof(Order.Customer));
            var node = _register.GetOrAddChild(property);

            Assert.NotEmpty(node.Columns);
            Assert.Equal("Customer_", node.GetTreePrefix());
            Assert.Equal("Id", node.LocalKeyColumn);
            Assert.Equal("customer_id", node.ParentKeyColumn);
            Assert.False(node.IsCollection);
        }

        [Fact]
        public void GetOrAddChild_Should_With_Valid_Columns_Test()
        {
            var property = _register.TableInfo.Type.GetProperty(nameof(Order.Customer));
            var node = _register.GetOrAddChild(property);

            var prefix = node.GetTreePrefix();

            CollectionAssert.ContainsAll(
                node.Columns.Select(x => x.Alias),
                node.TableInfo.Columns.Select(x => $"{prefix}c_{x.Name}")
            );

            CollectionAssert.ContainsAll(
                node.Columns.Select(x => x.ColumnInfo._column),
                node.TableInfo.Columns.Select(x => x._column)
            );

            CollectionAssert.ContainsAll(
                node.Columns.Select(x => x.Column.Name),
                node.TableInfo.Columns.Select(x => $"{node.Name.TryGetAlias()}.{x.Name}")
            );
        }

        [Fact]
        public void CallListenerOnAddMember()
        {
            var property = _register.TableInfo.Type.GetProperty(nameof(Order.Customer));
            var node = _register.GetOrAddChild(property);

            Assert.NotNull(node);
            Assert.Equal(property, node.Member);

            _listener.Received().Created(node);
        }

        [Fact]
        public void GetAllChildNodes_WithCircularReference_ShouldNotCauseStackOverflow()
        {
            // Arrange
            var registry = TranslationRegistry.Default;
            var tableA = registry.GetTable(typeof(CircularA));
            var listener = Substitute.For<INodeCreationListener>();
            listener.Info.Returns(new QueryInfo(new SqlServerQueryConfig(false), new DbName("CircularA")));
            var register = new ForeignKeyRegister(tableA, new DbName("CircularA"), listener);

            // Create circular reference: A -> B -> A
            var propertyA = typeof(CircularA).GetProperty(nameof(CircularA.CircularB));
            var propertyB = typeof(CircularB).GetProperty(nameof(CircularB.CircularA));

            var nodeA = register.GetOrAddChild(propertyA);
            var nodeB = nodeA.GetOrAddChild(propertyB);
            var nodeA2 = nodeB.GetOrAddChild(propertyA); // Cycle: A -> B -> A

            // Act
            var allNodes = register.GetAllChildNodes(false).ToList();

            // Assert
            Assert.NotNull(allNodes);

            var uniqueNodes = allNodes.Distinct().ToList();
            Assert.Equal(uniqueNodes.Count, allNodes.Count);
        }

        [Fact]
        public void GetAllChildNodes_WithDeepNesting_ShouldCompleteSuccessfully()
        {
            // Arrange
            var registry = TranslationRegistry.Default;
            var tableA = registry.GetTable(typeof(CircularA));
            var listener = Substitute.For<INodeCreationListener>();
            listener.Info.Returns(new QueryInfo(new SqlServerQueryConfig(false), new DbName("CircularA")));
            var register = new ForeignKeyRegister(tableA, new DbName("CircularA"), listener);
            var propertyA = typeof(CircularA).GetProperty(nameof(CircularA.CircularB));
            var propertyB = typeof(CircularB).GetProperty(nameof(CircularB.CircularA));

            var node1 = register.GetOrAddChild(propertyA);
            var node2 = node1.GetOrAddChild(propertyB);
            var node3 = node2.GetOrAddChild(propertyA);
            var node4 = node3.GetOrAddChild(propertyB);

            // Act
            var allNodes = register.GetAllChildNodes(false).ToList();

            // Assert
            Assert.NotNull(allNodes);
            Assert.True(allNodes.Count < 100, "Should have a reasonable limit of processed nodes");
        }

        [Fact]
        public void GetAllChildNodes_WithCollection_ShouldHandleCollectionsCorrectly()
        {
            // Arrange
            var registry = TranslationRegistry.Default;
            var tableA = registry.GetTable(typeof(CircularA));
            var listener = Substitute.For<INodeCreationListener>();
            listener.Info.Returns(new QueryInfo(new SqlServerQueryConfig(false), new DbName("CircularA")));
            var register = new ForeignKeyRegister(tableA, new DbName("CircularA"), listener);
            var propertyA = typeof(CircularA).GetProperty(nameof(CircularA.CircularB));

            var node = register.GetOrAddChild(propertyA);

            // Act
            var nodesWithCollection = register.GetAllChildNodes(true).ToList();
            var nodesWithoutCollection = register.GetAllChildNodes(false).ToList();

            // Assert
            Assert.NotNull(nodesWithCollection);
            Assert.NotNull(nodesWithoutCollection);
        }

        [Fact]
        public void GetAllChildNodes_ShouldReturnUniqueNodes()
        {
            // Arrange
            var registry = TranslationRegistry.Default;
            var tableA = registry.GetTable(typeof(CircularA));
            var listener = Substitute.For<INodeCreationListener>();
            listener.Info.Returns(new QueryInfo(new SqlServerQueryConfig(false), new DbName("CircularA")));
            var register = new ForeignKeyRegister(tableA, new DbName("CircularA"), listener);
            var propertyA = typeof(CircularA).GetProperty(nameof(CircularA.CircularB));
            var propertyB = typeof(CircularB).GetProperty(nameof(CircularB.CircularA));

            var node1 = register.GetOrAddChild(propertyA);
            var node2 = node1.GetOrAddChild(propertyB);
            var node3 = node2.GetOrAddChild(propertyA);

            // Act
            var allNodes = register.GetAllChildNodes(false).ToList();
            var distinctNodes = allNodes.Distinct().ToList();

            // Assert
            Assert.Equal(distinctNodes.Count, allNodes.Count);
        }

        [Fact]
        public void GetOrAddChild_WithUnregisteredTableType_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var registry = TranslationRegistry.Default;
            var tableA = registry.GetTable(typeof(CircularA));
            var listener = Substitute.For<INodeCreationListener>();
            listener.Info.Returns(new QueryInfo(new SqlServerQueryConfig(false), new DbName("CircularA")));
            var register = new ForeignKeyRegister(tableA, new DbName("CircularA"), listener);

            // Create a property that references a type not registered in the registry
            var property = typeof(UnregisteredType).GetProperty(nameof(UnregisteredType.SomeProperty));

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => register.GetOrAddChild(property));
            Assert.Equal("Column not found for member SomeProperty in table CircularA", ex.Message);
        }

        [Table("UnregisteredType")]
        private class UnregisteredType
        {
            public int Id { get; set; }
            public string SomeProperty { get; set; }
        }

        [Table("CircularA")]
        private class CircularA
        {
            public int Id { get; set; }
            public string Name { get; set; }

            [ForeignKey("circular_b_id")]
            public CircularB? CircularB { get; set; }
        }

        [Table("CircularB")]
        private class CircularB
        {
            public int Id { get; set; }
            public string Name { get; set; }

            [ForeignKey("circular_a_id")]
            public CircularA? CircularA { get; set; }
        }
    }
}