﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityTest.Utils;
using static System.Net.Mime.MediaTypeNames;

namespace UnityTest
{
    [TestClass]
    internal class BlobTest : MysqlConnectionTest
    {
        #region Consts
        private const string TABLE = "Files";
        private const string ID = "id";
        private const string BINARY = "bin";
        #endregion

        #region Init/Cleanup
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

        private static string GetCreateTableSql()
        {
            return $@"CREATE TABLE IF NOT EXISTS {TABLE} (
                  {ID} INT NOT NULL PRIMARY KEY AUTO_INCREMENT,
                  {BINARY} BLOB NULL
                )";
        }
        #endregion


        [TestMethod]
        public void OnInsertBytes()
        {
            Clear();
            var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            using var query = new Query(Connection, TABLE);
            query.Insert(new Cell(BINARY, bytes));
            
            var row = query.FirstRow();
            Assert.IsInstanceOfType(row[BINARY], typeof(byte[]));
            CollectionAssert.AreEqual(bytes, (byte[])row[BINARY]);
        }

        static void Clear()
        {
            using var query = new Query<Teste>(Connection, TABLE);
            query.Delete();
        }

        [Table(TABLE)]
        class Teste
        {
            public int Id { get; set; }
            public Stream Arquivo { get; set; }
        }
    }
}
