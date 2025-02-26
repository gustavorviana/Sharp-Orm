using BaseTest.Mock;
using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using SharpOrm.DataTranslation.Reader;
using System.ComponentModel.DataAnnotations.Schema;
using Xunit.Abstractions;
using static QueryTest.DataTranslation.Reader.ObjectActivatorTests;

namespace QueryTest.DataTranslation.Reader
{
    public class ManualMapTests(ITestOutputHelper? output) : DbMockTest(output)
    {
        [Fact]
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

            Assert.Equal(1, instance.Level1.Id);
            Assert.Equal("Prop1 Name", instance.Prop1);
            Assert.Equal(DateTime.Today, instance.Date);
            Assert.Equal("My Custom Name", instance.Level1.Level2.Level3.MyLevelName);
        }

        [Fact]
        public void AutoMapTest()
        {
            var tm = new TableMap<MyClass>(new TranslationRegistry());
            var table = tm.Build();

            Assert.NotNull(table.Columns.FirstOrDefault(x => x.Name == "Level1_Level2_Level3_MyLevelName"));
            Assert.NotNull(table.Columns.FirstOrDefault(x => x.Name == "Level1_Id"));
            Assert.NotNull(table.Columns.FirstOrDefault(x => x.Name == "Date"));
            Assert.NotNull(table.Columns.FirstOrDefault(x => x.Name == "Prop1"));

            Assert.Throws<InvalidOperationException>(() => tm.Property(x => x.Prop1, "CustomName"));
        }

        [Fact]
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
            Assert.False(cells.Any(c => c.Name == nameof(TestClass.MyId)));
            Assert.Equal(4, cells.Length);
        }

        [Fact]
        public void SetComplexObjectTest()
        {
            var tm = new TableMap<MyClass>(new TranslationRegistry());
            var table = tm.Build();

            var instance = new MyClass();

            table.GetColumn("Level1_Level2_Level3_MyLevelName").Set(instance, "My custom level name");
            Assert.Equal("My custom level name", instance.Level1.Level2.Level3.MyLevelName);
        }

        [Fact]
        public void FindColumnByNameTest()
        {
            var reg = new TranslationRegistry();
            var tm = new TableMap<MyClass>(reg);
            tm.Property(x => x.Date, "BeginDate");
            tm.Build();

            var colLevel = Column.FromExp<MyClass>(x => x.Level1.Level2.Level3.MyLevelName, reg);
            Assert.Equal("Level1_Level2_Level3_MyLevelName", colLevel.Name);

            var colDate = Column.FromExp<MyClass>(x => x.Date, reg);
            Assert.Equal("BeginDate", colDate.Name);
        }

        [Fact]
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

            Connection.QueryReaders.Add("SELECT TOP(1) * FROM [DynamicMappedTable]",
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

            using var q = new Query<DynamicMappedTable>(GetManager(mockConfig));
            var actual = q.FirstOrDefault();

            Assert.NotNull(actual);
            Assert.Equal(id, actual.Id);
            Assert.Equal(id2, actual.Id2);
            Assert.Equal(record_created, actual.CreatedAt);
            Assert.Equal(number, actual.Number);

            Assert.Equal(name, actual.User.Name);
            Assert.Equal(nick, actual.User.Nick);

            Assert.Equal(Guid.Parse(custom_id), actual.Custom.Id);
            Assert.Equal(custom_status, actual.Custom.Status);
        }

        [Fact]
        public void MapNestedWithPrefixTest()
        {
            Connection.QueryReaders.Add("SELECT TOP(1) * FROM [RootNestedObject]", GetPrefixedNestedObjectReader);

            var reg = new TranslationRegistry();
            var tm = new TableMap<RootNestedObject>(reg);
            tm.Property(x => x.Id, "Id");
            tm.Prefix(x => x.Child1, "WithPrefix");
            tm.Build();

            var mockConfig = Config.Clone();
            mockConfig.Translation = reg;

            using var q = new Query<RootNestedObject>(GetManager(mockConfig));
            var obj = q.FirstOrDefault();

            Assert.NotNull(obj);
            Assert.NotNull(obj.Child1);
            //Assert.NotNull(obj.Child2);

            var colLevel = Column.FromExp<RootNestedObject>(x => x.Child1!.Id, reg).Name;

            Assert.Equal(11, obj.Id);
            Assert.Equal(4, obj.Child1.Id);
            Assert.Equal(32, obj.Child1.ChildId);
            Assert.Equal("Value Child 1", obj.Child1.Value);
            Assert.Equal(444, obj.Child1.SubNested!.Id);

            Assert.Equal(11, obj.Id);
            //Assert.Equal(5, obj.Child2.Id);
            //Assert.Equal(32, obj.Child2.ChildId);
            //Assert.Equal(555, obj.Child2.SubNested!.Id);
        }

        private static MockDataReader GetPrefixedNestedObjectReader()
        {
            return new MockDataReader(
                new Cell("Id", 11),
                new Cell("WithPrefixId", 4),
                new Cell("Child2_Id", 5),
                new Cell("child_id", 32),
                new Cell("WithPrefixValue", "Value Child 1"),
                new Cell("Child2_Value", "Value Child 2"),
                new Cell("Child2_SubNested_Id", 555),
                new Cell("WithPrefixSubNested_Id", 444)
            );
        }

        private class RootNestedObject
        {
            public int Id { get; set; }

            public virtual ChildNestedObject? Child1 { get; set; }
            //public virtual ChildNestedObject? Child2 { get; set; }
        }

        private class ChildNestedObject
        {
            public int Id { get; set; }

            [Column("child_id")]
            public int ChildId { get; set; }

            public string? Value { get; set; }

            public ChildNestedObject2? SubNested { get; set; }
        }

        private class ChildNestedObject2
        {
            public int Id { get; set; }
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
