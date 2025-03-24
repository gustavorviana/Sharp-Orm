using BaseTest.Fixtures;
using BaseTest.Models;
using BaseTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
namespace QueryTest.Utils
{
    public class BaseBulkOperationTest : DbMockFallbackTest
    {
        public BaseBulkOperationTest()
        {

        }

        public BaseBulkOperationTest(QueryConfig config) : base(null, new MockFixture(config))
        {

        }

        protected void ExecuteUpdate(Guid guid, out DbName tableName)
        {
            var rows = new[] {
                    MakeUpdateRow(1, guid),
                    MakeUpdateRow(2, guid),
                    MakeUpdateRow(3, guid)
            };

            using var q = NewQuery<TestTable>();
            using var bulk = new BulkOperation(q, rows, 0);
            bulk.Update([TestTableUtils.ID, (TestTableUtils.NAME)]);

            tableName = bulk.table.DbName;
        }

        protected void ExecuteDelete(out DbName tableName)
        {
            var values = new[]
            {
                MakeDeleteRow(1),
                MakeDeleteRow(2),
                MakeDeleteRow(3)
            };

            using var q = NewQuery<TestTable>();
            using var bulk = new BulkOperation(q, values, 0);
            bulk.Delete();

            tableName = bulk.table.DbName;
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

        private static Row MakeDeleteRow(int number)
        {
            return new Row(
                new Cell(TestTableUtils.NAME, $"User {number}"),
                new Cell(TestTableUtils.NUMBER, number)
            );
        }

        protected Query<T> NewQuery<T>()
        {
            return new Query<T>(Manager);
        }
    }
}
