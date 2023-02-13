using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
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
        private static readonly ObjectLoader loader = new(typeof(TestClass), new TranslationConfig());

        [TestMethod]
        public void InvalidPk()
        {
            var cells = loader.GetCells(new TestClass()).ToArray();
            Assert.IsFalse(cells.Any(c => c.Name == nameof(TestClass.MyId)));
            Assert.AreEqual(6, cells.Length);

            cells = loader.GetCells(new TestClass { MyId = 1}).ToArray();
            Assert.IsTrue(cells.Any(c => c.Name == nameof(TestClass.MyId)));
            Assert.AreEqual(7, cells.Length);
        }

        [TestMethod]
        public void IgnorePk()
        {
            var cells = loader.GetCells(new TestClass { MyId = 1 }, true).ToArray();
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

        private static void AssertPropertyValue(object expected, TestClass objOwner, string propName)
        {
            var prop = loader.Properties[propName];

            Assert.IsNotNull(prop);
            Assert.AreEqual(expected, loader.GetColumnValue(ObjectLoader.GetColumnName(prop), objOwner, prop));
        }

        private static void AssertSqlValueConverted(object expected, object value)
        {
            Assert.AreEqual(expected, Query.Translator.Config.FromSql(value, expected?.GetType()));
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
