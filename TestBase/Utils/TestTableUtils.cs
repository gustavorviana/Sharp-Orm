using SharpOrm;

namespace BaseTest.Utils
{
    internal static class TestTableUtils
    {
        internal const string TABLE = "TestTable";

        internal const string ID = "id";
        internal const string NAME = "name";
        internal const string NICK = "nick";
        internal const string CREATEDAT = "record_created";
        internal const string NUMBER = "number";
        internal const string GUIDID = "custom_id";
        internal const string STATUS = "custom_status";

        internal static Row NewRow(int id, string name)
        {
            return new Row(
                new Cell(ID, id),
                new Cell(NAME, name),
                new Cell(NUMBER, id),
                new Cell(GUIDID, Guid.NewGuid().ToString()),
                new Cell(STATUS, 1)
            );
        }

        internal static Row[] GenRows(int count)
        {
            Row[] rows = new Row[count];

            for (int i = 1; i <= count; i++)
                rows[i - 1] = NewRow(i, $"User {i}");

            return rows;
        }
    }
}
