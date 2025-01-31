using BaseTest.Mock;
using BaseTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.DataTranslation;
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

            var obj = MappedObject.Read<NoAddressCustomer>(dbReader);

            Assert.Equal(id, obj.Id);
            Assert.Equal(name, obj.Name);
            Assert.Equal(email, obj.Email);
            Assert.Equal(addressId, obj.AddressId);
        }

        [Fact]
        public void MapNestedTest()
        {
            Connection.QueryReaders.Add("SELECT TOP(1) * FROM [RootNestedObject]", GetAdvancedObjectReader);

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
        public void RecursiveCallTest()
        {
            Connection.QueryReaders.Add("SELECT TOP(1) * FROM [RootNestedObject]", GetAdvancedObjectReader);

            using var query = new Query<RecursiveClass>(Manager);
            Assert.NotNull(query.FirstOrDefault());
        }

        private MockDataReader GetAdvancedObjectReader()
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

        [Fact]
        public void TestGetTranslationTest()
        {
            TableInfo table = new(typeof(CustomClassInfo), new TranslationRegistry());
            var owner = new CustomClassInfo();
            var cell = table.GetObjCells(owner, true, false).FirstOrDefault();
            Assert.Equal(2, cell?.Value);
        }

        #region Classes

        private class CustomClassInfo
        {
            [SqlConverter(typeof(TestGetTranslator))]
            public int MyValue { get; set; }
        }

        public class TestGetTranslator : ISqlTranslation
        {
            public bool CanWork(Type type) => type == typeof(int) || type == typeof(RecursiveClass);

            public object FromSqlValue(object value, Type expectedType)
            {
                return 1;
            }

            public object ToSqlValue(object value, Type type)
            {
                return 2;
            }
        }

        [Table("Recursive")]
        private class RecursiveClass
        {
            public int Id { get; set; }

            [SqlConverter(typeof(CustomTranslation))]
            [Column("Child1_Id")]
            public RecursiveClass? Parent { get; set; }
        }

        private class NoAddressCustomer
        {
            public uint Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }

            [Column("address_id")]
            public int? AddressId { get; set; }
        }

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

        internal class CustomTranslation : ISqlTranslation
        {
            public bool CanWork(Type type) => type == typeof(int) || type == typeof(RecursiveClass);

            public object FromSqlValue(object value, Type expectedType)
            {
                return new RecursiveClass { };
            }

            public object ToSqlValue(object value, Type type)
            {
                return value;
            }
        }
        #endregion
    }
}
