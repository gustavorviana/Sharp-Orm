using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Builder.DataTranslation;
using SharpOrm.Builder.Expressions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using UnityTest.Models;
using UnityTest.Utils;
using UnityTest.Utils.Mock;

namespace UnityTest
{
    [TestClass]
    public class ObjectTranslatorTest : MockTest
    {
        private static readonly TableInfo table = new(typeof(TestClass), new TranslationRegistry());

        [TestMethod]
        public void TestInvalidFields()
        {
            TableInfo table = new(typeof(InvalidFields), new TranslationRegistry());
            Assert.IsNull(table.Columns.FirstOrDefault(c => c.Name == nameof(InvalidFields.Id)), "The invalid field 'Id' was retrieved.");
            Assert.IsNull(table.Columns.FirstOrDefault(c => c.Name == nameof(InvalidFields.Value)), "The invalid property 'Value' was retrieved.");
            Assert.IsNull(table.Columns.FirstOrDefault(c => c.Name == nameof(InvalidFields.Name)), "The invalid property 'Name' was retrieved.");
        }

        [TestMethod]
        public void InvalidPk()
        {
            var cells = table.GetObjCells(new TestClass(), true, false).ToArray();
            Assert.IsFalse(cells.Any(c => c.Name == nameof(TestClass.MyId)));
            Assert.AreEqual(6, cells.Length);

            cells = table.GetObjCells(new TestClass { MyId = 1 }, true, false).ToArray();
            Assert.IsTrue(cells.Any(c => c.Name == nameof(TestClass.MyId)));
            Assert.AreEqual(7, cells.Length);
        }

        [TestMethod]
        public void IgnorePk()
        {
            var cells = table.GetObjCells(new TestClass { MyId = 1 }, false, false).ToArray();
            Assert.IsFalse(cells.Any(c => c.Name == nameof(TestClass.MyId)));
        }

        [TestMethod]
        public void SelectValidPk()
        {
            var obj = new Customer { AddressId = 1, Address = new Address(2) };
            var cells = new TableInfo(typeof(Customer)).GetObjCells(obj, false, true).Where(c => c.Name == "address_id").ToArray();
            Assert.AreEqual(1, cells.Length);
            Assert.AreEqual(1, cells[0].Value);
        }

        [TestMethod]
        public void SelectForeignKeyValue()
        {
            var obj = new CustomCustomer { Address = new CustomAddr { Id = 1 } };
            var cell = new TableInfo(typeof(CustomCustomer)).GetObjCells(obj, false, true).FirstOrDefault(c => c.Name == "address_id");
            Assert.AreEqual(1, cell.Value);
        }

        [TestMethod]
        public void SelectForeignKeyNullValue()
        {
            var obj = new CustomCustomer();
            var cell = new TableInfo(typeof(CustomCustomer)).GetObjCells(obj, false, true).FirstOrDefault(c => c.Name == "address_id");
            Assert.IsNull(cell.Value);
        }

        [TestMethod]
        public void EnumToSql()
        {
            AssertPropertyValue(0, new() { MyEnum = Status.Unknow }, nameof(TestClass.MyEnum));
            AssertPropertyValue(1, new() { MyEnum = Status.Success }, nameof(TestClass.MyEnum));
        }

        [TestMethod]
        public void GuidToSql()
        {
            Guid expected = Guid.NewGuid();

            AssertPropertyValue(expected.ToString(), new() { MyGuid = expected }, nameof(TestClass.MyGuid));
            AssertPropertyValue(DBNull.Value, new() { MyGuid = null }, nameof(TestClass.MyGuid));
        }

        [TestMethod]
        public void ParseDateFromDb()
        {
            var registry = new TranslationRegistry { DbTimeZone = TimeZoneInfo.Utc };
            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;

            TestAssert.AreEqualsDate(now, registry.FromSql(TimeZoneInfo.ConvertTimeToUtc(now), typeof(DateTime)), "DateTime.Now failed.");
            TestAssert.AreEqualsDate(TimeZoneInfo.ConvertTimeFromUtc(utcNow, TimeZoneInfo.Local), registry.FromSql(utcNow, typeof(DateTime)), "DateTime.UtcNow failed.");
        }

        [TestMethod]
        public void ParseDateFromDbWithoutConfig()
        {
            var registry = new TranslationRegistry();
            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;

            TestAssert.AreEqualsDate(now, registry.FromSql(now, typeof(DateTime)), "DateTime.Now failed.");
            TestAssert.AreEqualsDate(utcNow, registry.FromSql(utcNow, typeof(DateTime)), "DateTime.UtcNow failed.");
        }

        [TestMethod]
        public void ToGmtDate()
        {
            var registry = new TranslationRegistry { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time") };
            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;

            TestAssert.AreEqualsDate(TimeZoneInfo.ConvertTime(now, TimeZoneInfo.Local, registry.TimeZone), registry.FromSql(now, typeof(DateTime)), "DateTime.Now failed.");
            TestAssert.AreEqualsDate(TimeZoneInfo.ConvertTimeFromUtc(utcNow, registry.TimeZone), registry.FromSql(utcNow, typeof(DateTime)), "DateTime.UtcNow failed.");
        }

        [TestMethod]
        public void ParseDateFromCode()
        {
            var registry = new TranslationRegistry { DbTimeZone = TimeZoneInfo.Utc };
            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;

            TestAssert.AreEqualsDate(TimeZoneInfo.ConvertTimeToUtc(now), registry.ToSql(now), "DateTime.Now failed.");
            TestAssert.AreEqualsDate(utcNow, registry.ToSql(utcNow), "DateTime.UtcNow failed.");
        }

        [TestMethod]
        public void ParseDateFromCodeWithoutConfig()
        {
            var registry = new TranslationRegistry();
            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;

            TestAssert.AreEqualsDate(now, registry.ToSql(now), "DateTime.Now failed.");
            TestAssert.AreEqualsDate(utcNow, registry.ToSql(utcNow), "DateTime.UtcNow failed.");
        }

        [TestMethod]
        public void DateToSql()
        {
            DateTime expected = DateTime.Now;
            AssertPropertyValue(expected, new() { MyDate = expected }, nameof(TestClass.MyDate));
        }

        [TestMethod]
        public void IntToSql()
        {
            int expected = 1;
            AssertPropertyValue(expected, new() { MyId = expected }, nameof(TestClass.MyId));
        }

        [TestMethod]
        public void ByteToSql()
        {
            byte expected = 0x1;
            AssertPropertyValue(expected, new() { MyByte = expected }, nameof(TestClass.MyByte));
        }

        [TestMethod]
        public void SqlToNull()
        {
            AssertSqlValueConverted(null, DBNull.Value);
        }

        [TestMethod]
        public void SqlToEnum()
        {
            AssertSqlValueConverted(Status.Unknow, 0);
            AssertSqlValueConverted(Status.Success, 1);
        }

        [TestMethod]
        public void SqlToGuid()
        {
            Guid expected = Guid.NewGuid();

            AssertSqlValueConverted(expected, expected.ToString());
        }

        [TestMethod]
        public void SqlToDate()
        {
            DateTime expected = DateTime.Now;
            AssertSqlValueConverted(expected, expected);
        }

        [TestMethod]
        public void SqlToInt()
        {
            int expected = 1;
            AssertSqlValueConverted(expected, expected);
        }

        [TestMethod]
        public void SqlToByte()
        {
            byte expected = 0x1;
            AssertSqlValueConverted(expected, expected);
        }

        [TestMethod]
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

        [TestMethod]
        public void NullGuid()
        {
            var guid = Guid.NewGuid();
            Assert.AreEqual(null, TranslationRegistry.Default.FromSql(DBNull.Value, typeof(Guid)));
            Assert.AreEqual(guid, TranslationRegistry.Default.FromSql(guid.ToString(), typeof(Guid?)));
        }

        [TestMethod]
        public void NullInt()
        {
            int value = 123;
            Assert.AreEqual(null, TranslationRegistry.Default.FromSql(DBNull.Value, typeof(int)));
            Assert.AreEqual(value, TranslationRegistry.Default.FromSql(value, typeof(int?)));
        }

        [TestMethod]
        public void CanParseNull()
        {
            Assert.AreEqual(true, new NativeSqlTranslation().CanWork(typeof(int?)));
        }

        [TestMethod]
        public void FreezedDateTest()
        {
            var registry = new TranslationRegistry { DbTimeZone = TimeZoneInfo.Utc };
            var fNow = FreezedDate.Now;

            Assert.AreEqual(fNow.Value, registry.ToSql(fNow));
            Assert.AreEqual(fNow.Value.ToUniversalTime(), registry.ToSql(fNow.Value));
        }

        [TestMethod]
        public void DynamicTranslatorTest()
        {
            const int Id = 1;
            const string Name = "My Dynamic name";

            Connection.QueryReaders.Add("SELECT * FROM `Dynamic`", () => GetReader(i => new[] { new Cell("Id", Id), new Cell("Name", Name), new Cell("Null", DBNull.Value) }, 1));
            using var query = new Query("Dynamic", GetManager());
            var value = query.GetEnumerable<dynamic>().First();

            Assert.AreEqual(value.Id, Id);
            Assert.AreEqual(value.Name, Name);
            Assert.IsNull(value.Null);
        }

        [TestMethod]
        public void LoadArrayChild()
        {
            Connection.QueryReaders.Add("SELECT * FROM `Orders` LIMIT 1", () => GetReader(i => MakeOrderCells(1), 3));
            Connection.QueryReaders.Add("SELECT * FROM `OrderItems` WHERE `id` = 1", () => GetReader(i => MakeOrderItemsCells(i + 1, 1, i * 3 + 1), 10));

            using var query = new Query<Order>(GetManager());
            query.AddForeign(o => o.ArrayItems).AddForeign(o => o.ListItems).AddForeign(o => o.IListItems);
            var obj = query.FirstOrDefault();

            Assert.IsNotNull(obj);
            Assert.IsTrue(obj.ArrayItems.All(itm => itm.OrderId == 1));
            Assert.IsTrue(obj.ListItems.All(itm => itm.OrderId == 1));
            Assert.IsTrue(obj.IListItems.All(itm => itm.OrderId == 1));
        }

        [TestMethod]
        public void LoadAdvancedObject()
        {
            Connection.QueryReaders.Add("SELECT * FROM `RootAdvancedObject` LIMIT 1", GetAdvancedObjectReader);

            using var query = new Query<RootAdvancedObject>(GetManager());
            var obj = query.FirstOrDefault();

            Assert.IsNotNull(obj);
            Assert.AreEqual(11, obj.Id);
            Assert.AreEqual(32, obj.Child1.ChildId);
            Assert.AreEqual(32, obj.Child2.ChildId);
            Assert.AreEqual(4, obj.Child1.Id);
            Assert.AreEqual(5, obj.Child2.Id);
            Assert.AreEqual("Value Child 1", obj.Child1.Value);
            Assert.AreEqual("Value Child 2", obj.Child2.Value);
        }

        [TestMethod]
        public void TableWithSchemaTest()
        {
            var info = new TableInfo(typeof(TableWithSchema));
            Assert.AreEqual("MySchema.MyName", info.Name);
        }

        [TestMethod]
        public void RecursiveCallTest()
        {
            Connection.QueryReaders.Add("SELECT * FROM `Recursive` LIMIT 1", GetAdvancedObjectReader);

            using var query = new Query<RecursiveClass>(GetManager());
            Assert.IsNotNull(query.FirstOrDefault());
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
            return new Cell[] { new("Id", id) };
        }

        private static Cell[] MakeOrderItemsCells(int id, int orderId, int value)
        {
            return new Cell[] { new("Id", id), new("order_id", orderId), new("Value", value) };
        }

        private static void AssertPropertyValue(object expected, TestClass objOwner, string propName)
        {
            var prop = table.Columns.FirstOrDefault(c => c.Name == propName);

            Assert.IsNotNull(prop);
            Assert.AreEqual(expected, prop.Get(objOwner));
        }

        private static void AssertSqlValueConverted(object expected, object value)
        {
            Assert.AreEqual(expected, TranslationRegistry.Default.FromSql(value, expected?.GetType()));
        }

        [TestMethod]
        public void PropertyExpressionVisitorTest()
        {
            var column = new PropertyExpressionVisitor().VisitProperty<OrderItem>(x => x.OrderId);
            Assert.AreEqual("OrderId", column);
        }

        [TestMethod]
        public void CheckIsNumericString()
        {
            Assert.IsTrue(TranslationUtils.IsNumericString("1"));
            Assert.IsTrue(TranslationUtils.IsNumericString("123"));
            Assert.IsTrue(TranslationUtils.IsNumericString("1.1"));
            Assert.IsTrue(TranslationUtils.IsNumericString("1,2"));
            Assert.IsFalse(TranslationUtils.IsNumericString(",255"));
            Assert.IsFalse(TranslationUtils.IsNumericString("255,"));
            Assert.IsFalse(TranslationUtils.IsNumericString("."));
            Assert.IsFalse(TranslationUtils.IsNumericString("1m"));
            Assert.IsFalse(TranslationUtils.IsNumericString("1.1.1"));
            Assert.IsFalse(TranslationUtils.IsNumericString("1,1,1"));
        }

        [TestMethod]
        public void TestGetTranslationTest()
        {
            TableInfo table = new(typeof(CustomClassInfo), new TranslationRegistry());
            var owner = new CustomClassInfo();
            var cell = table.GetObjCells(owner, true, false).FirstOrDefault();
            Assert.AreEqual(2, cell.Value);
        }

        [Table("Recursive")]
        private class RecursiveClass
        {
            public int Id { get; set; }

            [SqlConverter(typeof(CustomTranslation))]
            [Column("Child1_Id")]
            public RecursiveClass Parent { get; set; }
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
        }

        private class ChildAdvancedObject
        {
            public int Id { get; set; }

            [Column("child_id")]
            public int ChildId { get; set; }

            public string Value { get; set; }
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

            [HasMany("id")]
            public OrderItem[] ArrayItems { get; set; }

            [HasMany("id")]
            public List<OrderItem> ListItems { get; set; }

            [HasMany("id")]
            public IList<OrderItem> IListItems { get; set; }
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
