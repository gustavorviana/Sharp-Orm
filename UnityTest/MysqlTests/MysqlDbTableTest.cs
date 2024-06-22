using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;
using SharpOrm.Builder;
using UnityTest.BaseTests;
using UnityTest.Models;
using UnityTest.Utils;

namespace UnityTest.MysqlTests
{
    [TestClass]
    public class MysqlDbTableTest : DbTableTest<MySqlConnection>
    {
        public MysqlDbTableTest() : base(new MysqlQueryConfig(false) { LoadForeign = true }, ConnectionStr.Mysql)
        {
        }
    }
}
