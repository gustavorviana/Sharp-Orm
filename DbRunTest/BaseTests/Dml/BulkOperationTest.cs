using BaseTest.Models;
using BaseTest.Utils;
using DbRunTest.Fixtures;
using SharpOrm;
using System.Data.Common;
using Xunit.Abstractions;

namespace DbRunTest.BaseTests.Dml
{
    public abstract class BulkOperationTest<T>(ITestOutputHelper output, DbFixture<T> connection) : DmlTest<T>(output, connection), IClassFixture<DbFixture<T>> where T : DbConnection, new()
    {
        [Fact]
        public virtual void UpdateTest()
        {
            var _guid = Guid.NewGuid();
            InsertRows(5);

            using var q = NewQuery<TestTable>();
            int updateCount = QueryExtension.BulkUpdate(
                q,
                [
                    MakeUpdateRow(1, _guid),
                    MakeUpdateRow(2, _guid),
                    MakeUpdateRow(3, _guid)
                ],
                [TestTableUtils.ID, TestTableUtils.NAME]
            );
            q.Where(TestTableUtils.GUIDID, _guid);

            Assert.Equal(3, updateCount);
            Assert.Equal(3, q.Count());
        }

        private static Row MakeUpdateRow(int id, Guid guid)
        {
            return new Row(
                new Cell(TestTableUtils.ID, id),
                new Cell(TestTableUtils.NAME, $"User {id}"),
                new Cell(TestTableUtils.NUMBER, id),
                new Cell(TestTableUtils.GUIDID, guid.ToString()),
                new Cell(TestTableUtils.STATUS, 1)
            );
        }

        [Fact]
        public virtual void DeleteTest()
        {
            InsertRows(5);

            using var q = NewQuery<TestTable>();
            var items = q.Get();
            int deleteCount = QueryExtension.BulkDelete(q, [MakeDeleteRow(1), MakeDeleteRow(2), MakeDeleteRow(3)]);

            Assert.Equal(3, deleteCount);
            Assert.Equal(2, q.Count());
        }

        private static Row MakeDeleteRow(int number)
        {
            return new Row(
                new Cell(TestTableUtils.NAME, $"User {number}"),
                new Cell(TestTableUtils.NUMBER, number)
            );
        }
    }
}
