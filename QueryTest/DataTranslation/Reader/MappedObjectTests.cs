using BaseTest.Mock;
using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm;
using SharpOrm.DataTranslation.Reader;
using System.ComponentModel.DataAnnotations.Schema;

namespace QueryTest.DataTranslation.Reader
{
    public class MappedObjectTests : DbMockTest
    {
        [Fact]
        public void ManualPropMapTest()
        {
            const uint id = 1;
            const string name = "Test";
            const string email = "my@email.com";
            const int addressId = 2;

            using var dbReader = GetReader(
                new Cell("Id", id),
                new Cell("Name", name),
                new Cell("Email", email),
                new Cell("address_id", addressId)
            );

            var obj = MappedObject.Read<CustomerWithoutAddress>(dbReader);

            Assert.Equal(id, obj.Id);
            Assert.Equal(name, obj.Name);
            Assert.Equal(email, obj.Email);
            Assert.Equal(addressId, obj.AddressId);
        }

        [Fact]
        public void MapNestedTest()
        {
            Connection.QueryReaders.Add("SELECT TOP(1) * FROM [RootNestedObject]", GetNestedObjectReader);

            using var query = new Query<RootNestedObject>(Manager);
            var obj = query.FirstOrDefault();

            Assert.NotNull(obj);
            Assert.NotNull(obj.Child1);

            Assert.Null(obj.Child2);

            Assert.Equal(11, obj.Id);
            Assert.Equal(32, obj.Child1.ChildId);
            Assert.Equal(4, obj.Child1.Id);
            Assert.Equal("Value Child 1", obj.Child1.Value);
            Assert.Empty(obj.StrArray);
        }

        [Fact]
        public void MapAllAsNestedTest()
        {
            Connection.QueryReaders.Add("SELECT TOP(1) * FROM [RootNestedObject]", GetNestedObjectReader);

            using var query = new Query<RootNestedObject>(GetManager(x => x.NestedMapMode = SharpOrm.Builder.NestedMode.All));
            var obj = query.FirstOrDefault();

            Assert.NotNull(obj);
            Assert.NotNull(obj.Child1);
            Assert.NotNull(obj.Child2);

            Assert.Equal(11, obj.Id);
            Assert.Equal(32, obj.Child1.ChildId);
            Assert.Equal(4, obj.Child1.Id);
            Assert.Equal("Value Child 1", obj.Child1.Value);

            Assert.Equal(11, obj.Id);
            Assert.Equal(32, obj.Child2.ChildId);
            Assert.Equal(5, obj.Child2.Id);

            Assert.Empty(obj.StrArray);
        }

        private static MockDataReader GetNestedObjectReader()
        {
            return new MockDataReader(
                new Cell("Id", 11),
                new Cell("Child_Id", 32),
                new Cell("Child1_Id", 4),
                new Cell("Child2_Id", 5),
                new Cell("Child1_Value", "Value Child 1"),
                new Cell("Child2_Value", "Value Child 2")
            );
        }

        #region Classes

        private class RootNestedObject
        {
            public int Id { get; set; }
            [MapNested]
            public ChildNesteddObject? Child1 { get; set; }
            public ChildNesteddObject? Child2 { get; set; }

            public string[] StrArray { get; set; } = [];
        }

        private class ChildNesteddObject
        {
            public int Id { get; set; }

            [Column("child_id")]
            public int ChildId { get; set; }

            public string? Value { get; set; }
        }
        #endregion
    }
}
