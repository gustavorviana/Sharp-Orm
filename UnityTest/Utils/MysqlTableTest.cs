using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using System;
using System.IO;
using UnityTest.Models;

namespace UnityTest.Utils
{
    public class MysqlTableTest : MysqlTest
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
            Grammar.QueryLogger = (x) => System.Diagnostics.Debug.WriteLine(x);
            using var creator = GetCreator();
            ExecuteScript(File.ReadAllText("./Scripts/Mysql.sql"), creator.GetConnection());
        }
        #endregion

        protected virtual Row NewRow(int? id, string name, int number = 0)
        {
            return new Row(new Cell(ID, id), new Cell(NAME, name), new Cell(NUMBER, number), new Cell(GUIDID, Guid.NewGuid().ToString()), new Cell(STATUS, Status.Unknow));
        }

        protected void InsertRows(int count)
        {
            Row[] rows = new Row[count];

            for (int i = 1; i <= count; i++)
                rows[i - 1] = NewRow(i, $"User {i}");

            using var q = NewQuery();
            q.BulkInsert(rows);
        }

        protected Query NewQuery()
        {
            return NewQuery(TABLE);
        }
    }
}
