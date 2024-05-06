using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using SharpOrm.Builder;
using UnityTest.Utils;

namespace UnityTest.SqlServerTests
{
    [TestClass]
    public class SqlServerTableBuilderTest : SqlServerTest
    {
        [TestMethod]
        public void ExistsTableTest()
        {
            var grammar = this.GetGrammar(new TableSchema("MyTable"));
            var expected = new SqlExpression("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = ?;", grammar.Name.Name);

            TestAssert.AreEqual(expected, grammar.Exists());
        }

        [TestMethod]
        public void ExistsTempTableTest()
        {
            var grammar = this.GetGrammar(new TableSchema("MyTable") { Temporary = true});
            var expected = new SqlExpression("SELECT COUNT(*) FROM tempdb..sysobjects WHERE charindex('_', name) > 0 AND left(name, charindex('_', name) -1) = ? AND xtype = 'u' AND object_id('tempdb..' + name) IS NOT NULL", grammar.Name.Name);
            var current = grammar.Exists();

            TestAssert.AreEqual(expected, current);
        }

        [TestMethod]
        public void DropTableTest()
        {
            var grammar = this.GetGrammar(new TableSchema("MyTable"));
            var expected = new SqlExpression("DROP TABLE MyTable");
            var current = grammar.Drop();

            TestAssert.AreEqual(expected, current);
        }

        [TestMethod]
        public void DropTempTableTest()
        {
            var grammar = this.GetGrammar(new TableSchema("MyTable") { Temporary = true });
            var expected = new SqlExpression("DROP TABLE #MyTable");
            var current = grammar.Drop();

            TestAssert.AreEqual(expected, current);
        }

        private SqlServerTableGrammar GetGrammar(TableSchema schema)
        {
            return new SqlServerTableGrammar(this.Config, schema);
        }
    }
}
