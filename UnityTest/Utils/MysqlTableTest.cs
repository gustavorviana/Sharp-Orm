using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using System;
using UnityTest.Models;

namespace UnityTest.Utils
{
    public class MysqlTableTest : MysqlConnectionTest
    {
        #region Consts
        protected const string TABLE = "TestTable";

        protected const string ID = "id";
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
            using var con = Connection;
            using var cmd = con.CreateCommand();
            cmd.CommandText = GetCreateTableSql();
            cmd.ExecuteNonQuery();
        }

        [ClassCleanup(InheritanceBehavior.BeforeEachDerivedClass)]
        public static void CleanupDbConnection()
        {
            using var con = Connection;
            using var cmd = con.CreateCommand();
            cmd.CommandText = $"DROP TABLE IF EXISTS {TABLE}";
            cmd.ExecuteNonQuery();
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

        private static string GetCreateTableSql()
        {
            return $@"CREATE TABLE IF NOT EXISTS {TABLE} (
                  {ID} INT NOT NULL PRIMARY KEY,
                  {NAME} VARCHAR(256) NOT NULL,
                  {NICK} VARCHAR(256) NULL,
                  {CREATEDAT} TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                  {NUMBER} DECIMAL(13,2) NOT NULL,
                  {GUIDID} VARCHAR(36) NULL,
                  {STATUS} INT NOT NULL
                )";
        }
    }
}
