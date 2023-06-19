using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Builder.DataTranslation;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using UnityTest.Models;

namespace UnityTest
{
    [TestClass]
    public class ObjectTranslatorTest
    {
        private static readonly TableInfo table = new(new TranslationRegistry(), typeof(TestClass));

        [TestMethod]
        public void InvalidPk()
        {
            var cells = table.GetCells(new TestClass()).ToArray();
            Assert.IsFalse(cells.Any(c => c.Name == nameof(TestClass.MyId)));
            Assert.AreEqual(6, cells.Length);

            cells = table.GetCells(new TestClass { MyId = 1 }).ToArray();
            Assert.IsTrue(cells.Any(c => c.Name == nameof(TestClass.MyId)));
            Assert.AreEqual(7, cells.Length);
        }

        [TestMethod]
        public void IgnorePk()
        {
            var cells = table.GetCells(new TestClass { MyId = 1 }, true).ToArray();
            Assert.IsFalse(cells.Any(c => c.Name == nameof(TestClass.MyId)));
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

        private class TestClass
        {
            [Key]
            public int MyId { get; set; }
            public string MyName { get; set; }
            public DateTime MyDate { get; set; }
            public TimeSpan MyTime { get; set; }
            public byte MyByte { get; set; }
            public Status MyEnum { get; set; }
            public Guid? MyGuid { get; set; }
        }
    }
}
