using BaseTest.Mock;
using BaseTest.Models;
using BaseTest.Utils;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using System.ComponentModel.DataAnnotations.Schema;
using Xunit.Abstractions;

namespace QueryTest
{
    public class ObjectTranslatorTest(ITestOutputHelper? output) : MockTest(output)
    {
        private static readonly TableInfo table = new(typeof(TestClass), new TranslationRegistry());

        [Fact]
        public void TestInvalidFields()
        {
            TableInfo table = new(typeof(InvalidFields), new TranslationRegistry());
            Assert.True(table.Columns.FirstOrDefault(c => c.Name == nameof(InvalidFields.Id)) == null, "The invalid field 'Id' was retrieved.");
            Assert.True(table.Columns.FirstOrDefault(c => c.Name == nameof(InvalidFields.Value)) == null, "The invalid property 'Value' was retrieved.");
            Assert.True(table.Columns.FirstOrDefault(c => c.Name == nameof(InvalidFields.Name)) == null, "The invalid property 'Name' was retrieved.");
        }

        [Fact]
        public void InvalidPk()
        {
            var cells = table.GetObjCells(new TestClass(), true, false).ToArray();
            Assert.DoesNotContain(cells, c => c.Name == nameof(TestClass.MyId));
            Assert.Equal(6, cells.Length);

            cells = table.GetObjCells(new TestClass { MyId = 1 }, true, false).ToArray();
            Assert.Contains(cells, c => c.Name == nameof(TestClass.MyId));
            Assert.Equal(7, cells.Length);
        }

        [Fact]
        public void IgnorePk()
        {
            var cells = table.GetObjCells(new TestClass { MyId = 1 }, false, false).ToArray();
            Assert.DoesNotContain(cells, c => c.Name == nameof(TestClass.MyId));
        }

        [Fact]
        public void SelectValidPk()
        {
            var obj = new Customer { AddressId = 1, Address = new Address(2) };
            var cells = new TableInfo(typeof(Customer)).GetObjCells(obj, false, true).Where(c => c.Name == "address_id").ToArray();
            Assert.Single(cells);
            Assert.Equal(1, cells[0].Value);
        }

        [Fact]
        public void SelectForeignKeyValue()
        {
            var obj = new CustomCustomer { Address = new CustomAddr { Id = 1 } };
            var cell = new TableInfo(typeof(CustomCustomer)).GetObjCells(obj, false, true).FirstOrDefault(c => c.Name == "address_id");
            Assert.Equal(1, cell?.Value);
        }

        [Fact]
        public void SelectForeignKeyNullValue()
        {
            var obj = new CustomCustomer();
            var cell = new TableInfo(typeof(CustomCustomer)).GetObjCells(obj, false, true).FirstOrDefault(c => c.Name == "address_id");
            Assert.Null(cell?.Value);
        }

        [Fact]
        public void EnumToSql()
        {
            AssertPropertyValue(0, new() { MyEnum = Status.Unknow }, nameof(TestClass.MyEnum));
            AssertPropertyValue(1, new() { MyEnum = Status.Success }, nameof(TestClass.MyEnum));
        }

        [Fact]
        public void GuidToSql()
        {
            Guid expected = Guid.NewGuid();

            AssertPropertyValue(expected.ToString(), new() { MyGuid = expected }, nameof(TestClass.MyGuid));
            AssertPropertyValue(DBNull.Value, new() { MyGuid = null }, nameof(TestClass.MyGuid));
        }

        [Fact]
        public void ParseDateFromDb()
        {
            var registry = new TranslationRegistry { DbTimeZone = TimeZoneInfo.Utc };
            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;

            TestAssert.EqualDate(now, registry.FromSql(TimeZoneInfo.ConvertTimeToUtc(now), typeof(DateTime)), "DateTime.Now failed.");
            TestAssert.EqualDate(TimeZoneInfo.ConvertTimeFromUtc(utcNow, TimeZoneInfo.Local), registry.FromSql(utcNow, typeof(DateTime)), "DateTime.UtcNow failed.");
        }

        [Fact]
        public void ParseDateFromDbWithoutConfig()
        {
            var registry = new TranslationRegistry();
            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;

            TestAssert.EqualDate(now, registry.FromSql(now, typeof(DateTime)), "DateTime.Now failed.");
            TestAssert.EqualDate(utcNow, registry.FromSql(utcNow, typeof(DateTime)), "DateTime.UtcNow failed.");
        }

        [Fact]
        public void ToGmtDate()
        {
            var registry = new TranslationRegistry { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time") };
            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;

            TestAssert.EqualDate(TimeZoneInfo.ConvertTime(now, TimeZoneInfo.Local, registry.TimeZone), registry.FromSql(now, typeof(DateTime)), "DateTime.Now failed.");
            TestAssert.EqualDate(TimeZoneInfo.ConvertTimeFromUtc(utcNow, registry.TimeZone), registry.FromSql(utcNow, typeof(DateTime)), "DateTime.UtcNow failed.");
        }

        [Fact]
        public void ParseDateFromCode()
        {
            var registry = new TranslationRegistry { DbTimeZone = TimeZoneInfo.Utc };
            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;

            TestAssert.EqualDate(TimeZoneInfo.ConvertTimeToUtc(now), registry.ToSql(now), "DateTime.Now failed.");
            TestAssert.EqualDate(utcNow, registry.ToSql(utcNow), "DateTime.UtcNow failed.");
        }

        [Fact]
        public void ParseDateFromCodeWithoutConfig()
        {
            var registry = new TranslationRegistry();
            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;

            TestAssert.EqualDate(now, registry.ToSql(now), "DateTime.Now failed.");
            TestAssert.EqualDate(utcNow, registry.ToSql(utcNow), "DateTime.UtcNow failed.");
        }

        [Fact]
        public void DateToSql()
        {
            DateTime expected = DateTime.Now;
            AssertPropertyValue(expected, new() { MyDate = expected }, nameof(TestClass.MyDate));
        }

        [Fact]
        public void IntToSql()
        {
            int expected = 1;
            AssertPropertyValue(expected, new() { MyId = expected }, nameof(TestClass.MyId));
        }

        [Fact]
        public void ByteToSql()
        {
            byte expected = 0x1;
            AssertPropertyValue(expected, new() { MyByte = expected }, nameof(TestClass.MyByte));
        }

        [Fact]
        public void SqlToNull()
        {
            AssertSqlValueConverted(null, DBNull.Value);
        }

        [Fact]
        public void SqlToEnum()
        {
            AssertSqlValueConverted(Status.Unknow, 0);
            AssertSqlValueConverted(Status.Success, 1);
        }

        [Fact]
        public void SqlToGuid()
        {
            Guid expected = Guid.NewGuid();

            AssertSqlValueConverted(expected, expected.ToString());
        }

        [Fact]
        public void SqlToDate()
        {
            DateTime expected = DateTime.Now;
            AssertSqlValueConverted(expected, expected);
        }

        [Fact]
        public void SqlToInt()
        {
            int expected = 1;
            AssertSqlValueConverted(expected, expected);
        }

        [Fact]
        public void SqlToByte()
        {
            byte expected = 0x1;
            AssertSqlValueConverted(expected, expected);
        }

        [Fact]
        public void SqlToBoolean()
        {
            bool expected = true;
            int intBool = 1;
            byte byteBool = 1;
            sbyte sbyteBool = 1;

            AssertSqlValueConverted(expected, expected);
            AssertSqlValueConverted(expected, intBool);
            AssertSqlValueConverted(expected, byteBool);
            AssertSqlValueConverted(expected, sbyteBool);
        }

        [Fact]
        public void NullGuid()
        {
            var guid = Guid.NewGuid();
            Assert.Null(TranslationRegistry.Default.FromSql(DBNull.Value, typeof(Guid)));
            Assert.Equal(guid, TranslationRegistry.Default.FromSql(guid.ToString(), typeof(Guid?)));
        }

        [Fact]
        public void NullInt()
        {
            int value = 123;
            Assert.Null(TranslationRegistry.Default.FromSql(DBNull.Value, typeof(int)));
            Assert.Equal(value, TranslationRegistry.Default.FromSql(value, typeof(int?)));
        }

        [Fact]
        public void CanParseNull()
        {
            Assert.True(new NativeSqlTranslation().CanWork(typeof(int?)));
        }

        [Fact]
        public void FreezedDateTest()
        {
            var registry = new TranslationRegistry { DbTimeZone = TimeZoneInfo.Utc };
            var fNow = FreezedDate.Now;

            Assert.Equal(fNow.Value, registry.ToSql(fNow));
            Assert.Equal(fNow.Value.ToUniversalTime(), registry.ToSql(fNow.Value));
        }

        [Fact]
        public void DynamicTranslatorTest()
        {
            const int Id = 1;
            const string Name = "My Dynamic name";

            Connection.QueryReaders.Add("SELECT * FROM [Dynamic]", () => GetReader(i => [new Cell("Id", Id), new Cell("Name", Name), new Cell("Null", DBNull.Value)], 1));
            using var query = new Query("Dynamic", Manager);
            var value = query.GetEnumerable<dynamic>().First();

            Assert.Equal(value.Id, Id);
            Assert.Equal(value.Name, Name);
            Assert.Null(value.Null);
        }

        [Fact]
        public void LoadArrayChild()
        {
            Connection.QueryReaders.Add("SELECT TOP(1) * FROM [Orders]", () => GetReader(i => MakeOrderCells(1), 3));
            Connection.QueryReaders.Add("SELECT * FROM [OrderItems] WHERE [id] = 1", () => GetReader(i => MakeOrderItemsCells(i + 1, 1, i * 3 + 1), 10));

            using var query = new Query<Order>(Manager);
            query.AddForeign(x => x.ArrayItems, x => x.ListItems, x => x.IListItems);

            var obj = query.FirstOrDefault();

            Assert.NotNull(obj);
            Assert.True(obj.ArrayItems.All(itm => itm.OrderId == 1));
            Assert.True(obj.ListItems.All(itm => itm.OrderId == 1));
            Assert.True(obj.IListItems.All(itm => itm.OrderId == 1));
        }

        [Fact]
        public void GetArrayParentCells()
        {
            var order = new Order
            {
                Id = 1,
                ArrayItems =
                [
                    new() { Id = 1 },
                    new() { Id = 2 }
                ]
            };

            var table = new TableInfo(typeof(Order));
            var cells = table.GetObjCells(order, true, true).ToArray();
            Assert.NotNull(cells);
            Assert.Single(cells);
            Assert.Equal("Id", cells[0].Name);
            Assert.Equal(1, cells[0].Value);
        }

        [Fact]
        public void LoadAdvancedObject()
        {
            Connection.QueryReaders.Add("SELECT TOP(1) * FROM [RootAdvancedObject]", GetAdvancedObjectReader);

            using var query = new Query<RootAdvancedObject>(Manager);
            var obj = query.FirstOrDefault();

            Assert.NotNull(obj);
            Assert.Equal(11, obj.Id);
            Assert.Equal(32, obj.Child1.ChildId);
            Assert.Equal(32, obj.Child2.ChildId);
            Assert.Equal(4, obj.Child1.Id);
            Assert.Equal(5, obj.Child2.Id);
            Assert.Equal("Value Child 1", obj.Child1.Value);
            Assert.Equal("Value Child 2", obj.Child2.Value);
            Assert.Empty(obj.StrArray);
        }

        [Fact]
        public void TableWithSchemaTest()
        {
            var info = new TableInfo(typeof(TableWithSchema));
            Assert.Equal("MySchema.MyName", info.Name);
        }

        [Fact]
        public void RecursiveCallTest()
        {
            Connection.QueryReaders.Add("SELECT TOP(1) * FROM [Recursive]", GetAdvancedObjectReader);

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

        private static Cell[] MakeOrderCells(int id)
        {
            return [new("Id", id)];
        }

        private static Cell[] MakeOrderItemsCells(int id, int orderId, int value)
        {
            return [new("Id", id), new("order_id", orderId), new("Value", value)];
        }

        private static void AssertPropertyValue(object expected, TestClass objOwner, string propName)
        {
            var prop = table.Columns.FirstOrDefault(c => c.Name == propName);

            Assert.NotNull(prop);
            Assert.Equal(expected, prop.Get(objOwner));
        }

        private static void AssertSqlValueConverted(object? expected, object? value)
        {
            Assert.Equal(expected, TranslationRegistry.Default.FromSql(value, expected?.GetType()));
        }

        [Fact]
        public void PropertyExpressionVisitorTest()
        {
            var column = ExpressionUtils<OrderItem>.GetPropName(x => x.OrderId);
            Assert.Equal("OrderId", column);
        }

        [Fact]
        public void CheckIsNumericString()
        {
            Assert.True(TranslationUtils.IsNumericString("1"));
            Assert.True(TranslationUtils.IsNumericString("123"));
            Assert.True(TranslationUtils.IsNumericString("1.1"));
            Assert.True(TranslationUtils.IsNumericString("1,2"));
            Assert.False(TranslationUtils.IsNumericString(",255"));
            Assert.False(TranslationUtils.IsNumericString("255,"));
            Assert.False(TranslationUtils.IsNumericString("."));
            Assert.False(TranslationUtils.IsNumericString("1m"));
            Assert.False(TranslationUtils.IsNumericString("1.1.1"));
            Assert.False(TranslationUtils.IsNumericString("1,1,1"));
        }

        [Fact]
        public void TestGetTranslationTest()
        {
            TableInfo table = new(typeof(CustomClassInfo), new TranslationRegistry());
            var owner = new CustomClassInfo();
            var cell = table.GetObjCells(owner, true, false).FirstOrDefault();
            Assert.Equal(2, cell?.Value);
        }

        [Table("Recursive")]
        private class RecursiveClass
        {
            public int Id { get; set; }

            [SqlConverter(typeof(CustomTranslation))]
            [Column("Child1_Id")]
            public RecursiveClass? Parent { get; set; }
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

        [Table("MyName", Schema = "MySchema")]
        private class TableWithSchema
        {
        }

        private class RootAdvancedObject
        {
            public int Id { get; set; }
            public ChildAdvancedObject Child1 { get; set; }
            public ChildAdvancedObject Child2 { get; set; }

            public string[] StrArray { get; set; } = [];
        }

        private class ChildAdvancedObject
        {
            public int Id { get; set; }

            [Column("child_id")]
            public int ChildId { get; set; }

            public string? Value { get; set; }
        }

        private class InvalidFields
        {
            public readonly int Id = 1;
            public int Value { get; }
            public string Name { set { } }
        }

        [Table("Orders")]
        private class Order
        {
            public int Id { get; set; }

            [ForeignKey("id")]
            public OrderItem[] ArrayItems { get; set; }

            [HasMany("id")]
            public List<OrderItem> ListItems { get; set; }

            [HasMany("id")]
            public IList<OrderItem> IListItems { get; set; }

            [ForeignKey("id")]
            public Order Parent { get; set; }
        }

        [Table("OrderItems")]
        private class OrderItem
        {
            public int Id { get; set; }

            [Column("order_id")]
            public int OrderId { get; set; }

            public int Value { get; set; }
        }

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
    }
}
