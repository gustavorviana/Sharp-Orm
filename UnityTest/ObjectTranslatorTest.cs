using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Builder.DataTranslation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using UnityTest.Models;
using UnityTest.Utils;

namespace UnityTest
{
    [TestClass]
    public class ObjectTranslatorTest : MockTest
    {
        private static readonly TableInfo table = new(new TranslationRegistry(), typeof(TestClass));

        [TestMethod]
        public void TestInvalidFields()
        {
            TableInfo table = new(new TranslationRegistry(), typeof(InvalidFields));
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
            var obj = new Customer { AddressId = 1, Address = new Address { Id = 2 } };
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
            Assert.AreEqual(null, TableReaderBase.Registry.FromSql(DBNull.Value, typeof(Guid)));
            Assert.AreEqual(guid, TableReaderBase.Registry.FromSql(guid.ToString(), typeof(Guid?)));
        }

        [TestMethod]
        public void NullInt()
        {
            int value = 123;
            Assert.AreEqual(null, TableReaderBase.Registry.FromSql(DBNull.Value, typeof(int)));
            Assert.AreEqual(value, TableReaderBase.Registry.FromSql(value, typeof(int?)));
        }

        [TestMethod]
        public void CanParseNull()
        {
            Assert.AreEqual(true, new NativeSqlValueConversor().CanWork(typeof(int?)));
        }

        [TestMethod]
        public void LoadArrayChild()
        {
            Connection.QueryReaders.Add("SELECT * FROM `Orders` LIMIT 1", () => GetReader(3, i => new Order { Id = 1 }, true));
            Connection.QueryReaders.Add("SELECT * FROM `OrderItems` WHERE `id` = 1", () => GetReader(10, i => new OrderItem
            {
                Id = i + 1,
                OrderId = 1,
                Value = i * 3 + 1
            }, true));

            using var query = new Query<Order>(Connection, Config);
            query.WithForeigns("OrderItems");
            var obj = query.FirstOrDefault();

            Assert.IsNotNull(obj);
            Assert.IsTrue(obj.ArrayItems.All(itm => itm.OrderId == 1));
            Assert.IsTrue(obj.ListItems.All(itm => itm.OrderId == 1));
            Assert.IsTrue(obj.IListItems.All(itm => itm.OrderId == 1));
        }

        private static void AssertPropertyValue(object expected, TestClass objOwner, string propName)
        {
            var prop = table.Columns.FirstOrDefault(c => c.Name == propName);

            Assert.IsNotNull(prop);
            Assert.AreEqual(expected, prop.Get(objOwner));
        }

        private static void AssertSqlValueConverted(object expected, object value)
        {
            Assert.AreEqual(expected, TableReaderBase.Registry.FromSql(value, expected?.GetType()));
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
    }
}
