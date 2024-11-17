using BaseTest.Models;
using SharpOrm;

namespace BaseTest.Utils
{
    public static class TestTableUtils
    {
        public const string TABLE = "TestTable";

        public const string ID = "id";
        public const string NAME = "name";
        public const string NICK = "nick";
        public const string CREATEDAT = "record_created";
        public const string NUMBER = "number";
        public const string GUIDID = "custom_id";
        public const string STATUS = "custom_status";

        public static Row NewRow(int? id, string name, int number = 0, Guid? guid = null)
        {
            return new Row(
                new Cell(ID, id),
                new Cell(NAME, name),
                new Cell(NUMBER, number),
                new Cell(GUIDID, (guid ?? Guid.NewGuid()).ToString()),
                new Cell(STATUS, Status.Unknow)
            );
        }

        public static Row[] GenRows(int count)
        {
            Row[] rows = new Row[count];

            for (int i = 1; i <= count; i++)
                rows[i - 1] = NewRow(i, $"User {i}");

            return rows;
        }
    }
}
