using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using System;
using System.Data.SqlClient;
using System.IO;

namespace UnityTest.Utils
{
    public class SqlServerTest : DbTest<SqlConnection>
    {
        protected static readonly SqlServerQueryConfig EscapeStringsConfig = new(false) { UseOldPagination = false, EscapeStrings = true };
        protected static readonly SqlServerQueryConfig NewConfig = new(false) { UseOldPagination = false };
        protected static readonly SqlServerQueryConfig OldConfig = new(false) { UseOldPagination = true };

        #region Consts
        protected const string TABLE = "TestTable";

        protected const string ID = "id";
        protected const string NAME = "name";
        protected const string NICK = "nick";
        protected const string CREATEDAT = "record_created";
        protected const string NUMBER = "number";
        protected const string GUIDID = "custom_id";
        protected const string STATUS = "custom_status";

        public SqlServerTest() : base(NewConfig, ConnectionStr.SqlServer)
        {
        }
        #endregion

        #region Class Init/Clean
        [ClassInitialize(InheritanceBehavior.BeforeEachDerivedClass)]
        public static void InitConnection(TestContext context)
        {
            using var creator = GetCreator();
            if (DbTable.Exists(TABLE, creator: creator))
                return;

            ExecuteScript(File.ReadAllText("./Scripts/SqlServer.sql"), creator);
        }

        [ClassCleanup(InheritanceBehavior.BeforeEachDerivedClass)]
        public static void CleanupDbConnection()
        {
            using var creator = GetCreator();
            ExecuteScript($"DROP TABLE IF EXISTS {TABLE}", creator);
        }

        private static ConnectionCreator GetCreator()
        {
            return new SingleConnectionCreator(NewConfig, ConnectionStr.SqlServer);
        }
        #endregion

        protected static Row NewRow(int id, string name)
        {
            return new Row(
                new Cell(ID, id),
                new Cell(NAME, name),
                new Cell(NUMBER, id),
                new Cell(GUIDID, Guid.NewGuid().ToString()),
                new Cell(STATUS, 1)
            );
        }

        protected void InsertRows(int count)
        {
            try
            {
                Row[] rows = new Row[count];

                for (int i = 1; i <= count; i++)
                    rows[i - 1] = NewRow(i, $"User {i}");

                using var q = NewQuery(TABLE);
                q.Delete();
                q.BulkInsert(rows);
            }
            catch (Exception)
            {
            }
        }
    }
}
