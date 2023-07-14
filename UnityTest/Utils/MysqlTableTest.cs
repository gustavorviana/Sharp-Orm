using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Connection;
using System;
using System.IO;
using UnityTest.Models;

namespace UnityTest.Utils
{
    public class MysqlTableTest : MysqlConnectionTest
    {
        #region Consts
        protected const string TABLE = "TestTable";

        protected const string ID = "id";
        protected const string ID2 = "id2";
        protected const string NAME = "name";
        protected const string NICK = "nick";
        protected const string CREATEDAT = "record_created";
        protected const string NUMBER = "number";
        protected const string GUIDID = "custom_id";
        protected const string STATUS = "custom_status";
        #endregion

        #region Class Init/Clean
        [ClassInitialize(InheritanceBehavior.BeforeEachDerivedClass)]
        public static void OnMysqlTableTestInit(TestContext context)
        {
            var con = Connection;
            try
            {
                using var cmd = con.CreateCommand();
                cmd.CommandText = File.ReadAllText("./Scripts/Mysql.sql");
                cmd.ExecuteNonQuery();
            }
            finally
            {
                ConnectionCreator.Default.SafeDisposeConnection(con);
            }
        }
        #endregion

        protected static Row NewRow(int id, string name)
        {
            return new Row(new Cell(ID, id), new Cell(NAME, name), new Cell(NUMBER, 0M), new Cell(GUIDID, Guid.NewGuid().ToString()), new Cell(STATUS, Status.Unknow));
        }

        protected static void InsertRows(int count)
        {
            Row[] rows = new Row[count];

            for (int i = 1; i <= count; i++)
                rows[i - 1] = NewRow(i, $"User {i}");

            using var q = NewQuery();
            q.BulkInsert(rows);
        }

        protected static Query NewQuery()
        {
            return NewQuery(TABLE);
        }
    }
}
