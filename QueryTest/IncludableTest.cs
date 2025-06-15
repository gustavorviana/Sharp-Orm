using BaseTest.Models;
using BaseTest.Utils;
using NSubstitute;
using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using SharpOrm.ForeignKey;

namespace QueryTest
{
    public class IncludableTest
    {
        private readonly INodeCreationListener _listener;
        private readonly ForeignKeyRegister _register;

        public IncludableTest()
        {
            _listener = Substitute.For<INodeCreationListener>();
            _register = new ForeignKeyRegister(new TableInfo(typeof(Order), TranslationRegistry.Default), _listener);
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
                node.Columns.Select(x => x.ColumnInfo.column),
                node.TableInfo.Columns.Select(x => x.column)
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
    }
}