using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm;
using UnityTest.Utils;

namespace UnityTest
{
    [TestClass]
    public class QueryTest : MysqlTableTest
    {
        [TestMethod]
        public void Clone()
        {
            var original = new Query("table", "alias")
            {
                Limit = 1,
                Offset = 3,
                Distinct = true
            };

            original.Select("Col1", "Col2");
            original.WhereColumn("Col1", "=", "Col2");

            Assert.AreEqual(original.ToString(), original.Clone(true).ToString());
            var clone = original.Clone(false);

            Assert.AreNotEqual(original.ToString(), original.Clone(false).ToString());
            Assert.AreEqual(original.Limit, clone.Limit);
            Assert.AreEqual(original.Offset, clone.Offset);
            Assert.AreEqual(original.Distinct, clone.Distinct);
        }
    }
}
