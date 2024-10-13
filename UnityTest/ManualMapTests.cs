﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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

            table.GetColumn("Level1_Level2_Level3_MyLevelName").Set(instance, "My custom level name");
            Assert.AreEqual("My custom level name", instance.Level1.Level2.Level3.MyLevelName);
        }

        [TestMethod]
        public void FindColumnByNameTest()
        {
            var reg = new TranslationRegistry();
            var tm = new TableMap<MyClass>(reg);
            tm.Property(x => x.Date, "BeginDate");
            tm.Build();

            var colLevel = Column.FromExp<MyClass>(x => x.Level1.Level2.Level3.MyLevelName, reg);
            Assert.AreEqual("Level1_Level2_Level3_MyLevelName", colLevel.Name);

            var colDate = Column.FromExp<MyClass>(x => x.Date, reg);
            Assert.AreEqual("BeginDate", colDate.Name);
        }

        [TestMethod]
        public void QueryReadTest()
        {
            var id = 1;
            var id2 = 2;
            var name = "My Name";
            var nick = "My Nick";
            var record_created = DateTime.Now;
            var number = 123;
            var custom_id = Guid.NewGuid().ToString();
            var custom_status = Status.Success;

            Connection.QueryReaders.Add("SELECT * FROM `DynamicMappedTable` LIMIT 1",
                () => new MockDataReader(
                    new Cell("Id", id), 
                    new Cell("id2", id2), 
                    new Cell("name", name), 
                    new Cell("nick", nick),
                    new Cell("record_created", record_created),
                    new Cell("number", number),
                    new Cell("custom_id", custom_id),
                    new Cell("custom_status", custom_status))
            );

            var reg = new TranslationRegistry();
            var tm = new TableMap<DynamicMappedTable>(reg);
            tm.Property(x => x.User.Name, "name");
            tm.Property(x => x.User.Nick, "nick");
            tm.Property(x => x.CreatedAt, "record_created");
            tm.Build();

            var mockConfig = Config.Clone();
            mockConfig.Translation = reg;

            using var q = new Query<DynamicMappedTable>(GetConnectionManager(mockConfig));
            var actual = q.FirstOrDefault();

            Assert.IsNotNull(actual);
            Assert.AreEqual(id, actual.Id);
            Assert.AreEqual(id2, actual.Id2);
            Assert.AreEqual(record_created, actual.CreatedAt);
            Assert.AreEqual(number, actual.Number);

            Assert.AreEqual(name, actual.User.Name);
            Assert.AreEqual(nick, actual.User.Nick);

            Assert.AreEqual(Guid.Parse(custom_id), actual.Custom.Id);
            Assert.AreEqual(custom_status, actual.Custom.Status);
        }

        private class DynamicMappedTable
        {
            public int Id { get; set; }
            public int Id2 { get; set; }
            public DateTime? CreatedAt { get; set; }
            public decimal Number { get; set; }

            public UserInfo User { get; set; }
            public Custom Custom { get; set; }
        }

        private class UserInfo
        {
            public string Name { get; set; }
            public string Nick { get; set; }
        }

        private class Custom
        {
            public Guid? Id { get; set; }
            public Status Status { get; set; }
        }
    }
}