using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using Teste.Utils;

namespace UnityTest
{
    [TestClass]
    public class InsertBuilder : MysqlTableTest
    {
        [TestMethod]
        public void BulkInsert()
        {
            var q = new Query(connection, TABLE);

            q.BulkInsert(this.NewRow(1, "T1"), this.NewRow(2, "T2"), this.NewRow(3, "T3"), this.NewRow(4, "T4"), this.NewRow(5, "T5")); 
        }

        private Row NewRow(int id, string name)
        {
            return new Row(new Cell[] { new Cell("id", id), new Cell("name", name) });
        }
    }
}
