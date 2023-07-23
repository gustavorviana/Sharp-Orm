using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm.Builder;
using System;
using System.IO;
using System.Linq;
using UnityTest.Models;

namespace UnityTest
{
    [TestClass]
    public class ColumnInfoTest
    {
        private static readonly TableInfo info = new(typeof(TestClass));

        [TestMethod]
        public void GetValue()
        {
            var table = new TestClass { MyGuid = Guid.Empty };
            var col = info.Columns.Where(c => c.Name == nameof(table.MyGuid)).First();

            Assert.AreEqual(table.MyGuid, col.GetRaw(table));
            Assert.AreEqual(table.MyGuid.ToString(), col.Get(table));
        }

        [TestMethod]
        public void SetRawGuidValue()
        {
            var id = Guid.NewGuid();
            var table = new TestClass();
            var col = info.Columns.Where(c => c.Name == nameof(table.MyGuid)).First();

            col.SetRaw(table, id);
            Assert.AreEqual(id, col.GetRaw(table));
        }

        [TestMethod]
        public void SetGuidStringValue()
        {
            var id = Guid.NewGuid();
            var table = new TestClass();
            var col = info.Columns.Where(c => c.Name == nameof(table.MyGuid)).First();

            col.Set(table, id.ToString());
            Assert.AreEqual(id, col.GetRaw(table));
        }

        [TestMethod]
        public void SetGuidValue()
        {
            var id = Guid.NewGuid();
            var table = new TestClass();
            var col = info.Columns.Where(c => c.Name == nameof(table.MyGuid)).First();

            col.Set(table, id);
            Assert.AreEqual(id, col.GetRaw(table));
        }

        [TestMethod]
        public void SetDateTime()
        {
            var now = DateTime.Now;
            var table = new TestClass();
            var col = info.Columns.Where(c => c.Name == nameof(table.MyDate)).First();

            col.Set(table, now);
            Assert.AreEqual(now, col.GetRaw(table));
        }

        [TestMethod]
        public void SetTimeByDate()
        {
            var now = DateTime.Now;
            var table = new TestClass();
            var col = info.Columns.Where(c => c.Name == nameof(table.MyTime)).First();

            col.Set(table, now);
            Assert.AreEqual(now.TimeOfDay, col.GetRaw(table));
        }

        [TestMethod]
        public void SetByteArray()
        {
            var buffer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var info = new TableInfo(typeof(BinaryClass));
            var _class = new BinaryClass();

            var col = info.Columns.Where(c => c.Name == nameof(_class.bytes)).First();

            col.Set(_class, buffer);
            Assert.AreEqual(buffer, _class.bytes);
        }

        [TestMethod]
        public void SetStream()
        {
            using var ms = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            var info = new TableInfo(typeof(BinaryClass));
            var _class = new BinaryClass();

            var bytesCol = info.Columns.Where(c => c.Name == nameof(_class.bytes)).First();
            var streamCol = info.Columns.Where(c => c.Name == nameof(_class.Stream)).First();

            bytesCol.Set(_class, ms.ToArray());
            streamCol.Set(_class, ms.ToArray());
            CollectionAssert.AreEqual(ms.ToArray(), _class.bytes);
        }

        [TestMethod]
        public void GetByteArray()
        {
            var buffer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var info = new TableInfo(typeof(BinaryClass));
            var _class = new BinaryClass();

            var bytesCol = info.Columns.Where(c => c.Name == nameof(_class.bytes)).First();
            var streamCol = info.Columns.Where(c => c.Name == nameof(_class.Stream)).First();

            bytesCol.Set(_class, buffer);
            streamCol.Set(_class, buffer);
            CollectionAssert.AreEqual(buffer, bytesCol.Get(_class) as byte[]);
            CollectionAssert.AreEqual(buffer, streamCol.Get(_class) as byte[]);
        }

        private class BinaryClass
        {
#pragma warning disable CS0649 // Field "ColumnInfoTest.BinaryClass.bytes" is never assigned and will always have its default value null.
            public byte[] bytes;
#pragma warning restore CS0649 // Field "ColumnInfoTest.BinaryClass.bytes" is never assigned and will always have its default value null.

            public Stream Stream { get; set; }
        }
    }
}
