using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using SharpOrm.DataTranslation.Reader;
using System;
using System.Linq;
using UnityTest.Models;
using UnityTest.Utils;
using UnityTest.Utils.Mock;
using static UnityTest.ObjectActivatorTest;

namespace UnityTest
{
    [TestClass]
    public class ManualMapTests : MockTest
    {
        [TestMethod]
        public void ManualPropMapTest()
        {
            var reader = new MockDataReader(new Cell("TestName", "Prop1 Name"), new Cell("MyId", 1), new Cell("Lvl3Name", "My Custom Name"), new Cell("Date", DateTime.Today));
            var tm = new TableMap<MyClass>(new TranslationRegistry());

            tm.Property(x => x.Prop1, "TestName");
            tm.Property(x => x.Level1.Id, "MyId");
            tm.Property(x => x.Level1.Level2.Level3.MyLevelName, "Lvl3Name");

            reader.Read();
            var m = MappedManualObj.FromMap(tm, reader);
            var instance = (MyClass)m.Read(reader);

            Assert.AreEqual(1, instance.Level1.Id);
            Assert.AreEqual("Prop1 Name", instance.Prop1);
            Assert.AreEqual(DateTime.Today, instance.Date);
            Assert.AreEqual("My Custom Name", instance.Level1.Level2.Level3.MyLevelName);
        }

        [TestMethod]
        public void AutoMapTest()
        {
            var tm = new TableMap<MyClass>(new TranslationRegistry());
            var table = tm.Build();

            Assert.IsNotNull(table.Columns.FirstOrDefault(x => x.Name == "Level1_Level2_Level3_MyLevelName"));
            Assert.IsNotNull(table.Columns.FirstOrDefault(x => x.Name == "Level1_Id"));
            Assert.IsNotNull(table.Columns.FirstOrDefault(x => x.Name == "Date"));
            Assert.IsNotNull(table.Columns.FirstOrDefault(x => x.Name == "Prop1"));

            Assert.ThrowsException<InvalidOperationException>(() => tm.Property(x => x.Prop1, "CustomName"));
        }

        [TestMethod]
        public void ReadComplexObjectTest()
        {
            var tm = new TableMap<MyClass>(new TranslationRegistry());
            var table = tm.Build();

            var instance = new MyClass
            {
                Prop1 = "Propriedade 1",
                Level1 = new Level1
                {
                    Id = 50,
                    Level2 = new Level2
                    {
                        Level3 = new Level3 { MyLevelName = "3o Level" }
                    }
                }
            };

            var cells = table.GetObjCells(instance, true, false).ToArray();
            Assert.IsFalse(cells.Any(c => c.Name == nameof(TestClass.MyId)));
            Assert.AreEqual(4, cells.Length);
        }

        [TestMethod]
        public void SetComplexObjectTest()
        {
            var tm = new TableMap<MyClass>(new TranslationRegistry());
            var table = tm.Build();

            var instance = new MyClass();

            table.GetColumns("Level1_Level2_Level3_MyLevelName").Set(instance, "My custom level name");
            Assert.AreEqual("My custom level name", instance.Level1.Level2.Level3.MyLevelName);
        }
    }
}
