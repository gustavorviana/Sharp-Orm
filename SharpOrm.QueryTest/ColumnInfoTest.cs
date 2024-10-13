using BaseTest.Models;
using SharpOrm.Builder;

namespace UnityTest
{
    public class ColumnInfoTest
    {
        private static readonly TableInfo info = new(typeof(TestClass));

        [Fact]
        public void GetValue()
        {
            var table = new TestClass { MyGuid = Guid.Empty };
            var col = info.Columns.Where(c => c.Name == nameof(table.MyGuid)).First();

            Assert.Equal(table.MyGuid, col.GetRaw(table));
            Assert.Equal(table.MyGuid.ToString(), col.Get(table));
        }

        [Fact]
        public void SetRawGuidValue()
        {
            var id = Guid.NewGuid();
            var table = new TestClass();
            var col = info.Columns.Where(c => c.Name == nameof(table.MyGuid)).First();

            col.SetRaw(table, id);
            Assert.Equal(id, col.GetRaw(table));
        }

        [Fact]
        public void SetGuidStringValue()
        {
            var id = Guid.NewGuid();
            var table = new TestClass();
            var col = info.Columns.Where(c => c.Name == nameof(table.MyGuid)).First();

            col.Set(table, id.ToString());
            Assert.Equal(id, col.GetRaw(table));
        }

        [Fact]
        public void SetGuidValue()
        {
            var id = Guid.NewGuid();
            var table = new TestClass();
            var col = info.Columns.Where(c => c.Name == nameof(table.MyGuid)).First();

            col.Set(table, id);
            Assert.Equal(id, col.GetRaw(table));
        }

        [Fact]
        public void SetDateTime()
        {
            var now = DateTime.Now;
            var table = new TestClass();
            var col = info.Columns.Where(c => c.Name == nameof(table.MyDate)).First();

            col.Set(table, now);
            Assert.Equal(now, col.GetRaw(table));
        }

        [Fact]
        public void SetTimeByDate()
        {
            var now = DateTime.Now;
            var table = new TestClass();
            var col = info.Columns.Where(c => c.Name == nameof(table.MyTime)).First();

            col.Set(table, now);
            Assert.Equal(now.TimeOfDay, col.GetRaw(table));
        }

        [Fact]
        public void SetByteArray()
        {
            var buffer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var info = new TableInfo(typeof(BinaryClass));
            var _class = new BinaryClass();

            var col = info.Columns.Where(c => c.Name == nameof(_class.bytes)).First();

            col.Set(_class, buffer);
            Assert.Equal(buffer, _class.bytes);
        }

        [Fact]
        public void SetStream()
        {
            using var ms = new MemoryStream([1, 2, 3, 4, 5, 6, 7, 8, 9]);

            var info = new TableInfo(typeof(BinaryClass));
            var _class = new BinaryClass();

            var bytesCol = info.Columns.Where(c => c.Name == nameof(_class.bytes)).First();
            var streamCol = info.Columns.Where(c => c.Name == nameof(_class.Stream)).First();

            bytesCol.Set(_class, ms.ToArray());
            streamCol.Set(_class, ms.ToArray());
            Assert.Equal(ms.ToArray(), _class.bytes);
        }

        [Fact]
        public void GetByteArray()
        {
            var buffer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var info = new TableInfo(typeof(BinaryClass));
            var _class = new BinaryClass();

            var bytesCol = info.Columns.Where(c => c.Name == nameof(_class.bytes)).First();
            var streamCol = info.Columns.Where(c => c.Name == nameof(_class.Stream)).First();

            bytesCol.Set(_class, buffer);
            streamCol.Set(_class, buffer);
            Assert.Equal(buffer, bytesCol.Get(_class) as byte[]);
            Assert.Equal(buffer, streamCol.Get(_class) as byte[]);
        }

        private class BinaryClass
        {
#pragma warning disable CS0649 // Field "ColumnInfoTest.BinaryClass.bytes" is never assigned and will always have its default value null.
            public byte[] bytes;
#pragma warning restore CS0649 // Field "ColumnInfoTest.BinaryClass.bytes" is never assigned and will always have its default value null.

            public MemoryStream Stream { get; set; }
        }
    }
}
