using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm.Builder;
using SharpOrm.Builder.DataTranslation;
using UnityTest.Models;
using System;
using System.Linq;
using MySqlX.XDevAPI.Relational;

namespace UnityTest
{
    [TestClass]
    public class ColumnInfoTest
    {
        private static readonly TableInfo info = new TableInfo(new TranslationRegistry(), typeof(TestClass));

        [TestMethod]
        public void GetValue()
        {
            var table = new TestClass { MyGuid = Guid.Empty };
            var col = info.Columns.Where(c => c.Name == nameof(table.MyGuid)).First();

            Assert.AreEqual(table.MyGuid, col.GetRaw(table));
            Assert.AreEqual(table.MyGuid.ToString(), col.Get(table));
        }

        [TestMethod]
        public void SetValue()
        {
            var id = Guid.NewGuid();

            var table = new TestClass();
            var col = info.Columns.Where(c => c.Name == nameof(table.MyGuid)).First();

            col.SetRaw(table, id);
            Assert.AreEqual(id, col.GetRaw(table));

            table.MyGuid = null;
            col.Set(table, id.ToString());
            Assert.AreEqual(id, col.GetRaw(table));
        }
    }
}
