using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpOrm.Builder;
using System;
namespace UnityTest
{
    [TestClass]
    public class DbNameTest
    {

        [TestMethod]
        public void InvalidNameTest()
        {
            Assert.ThrowsException<InvalidOperationException>(() => new DbName("My Name", ""));
            Assert.ThrowsException<InvalidOperationException>(() => new DbName("Invalid(name)", ""));
            _ = new DbName("Schema.Table");
            _ = new DbName("#TempTable");
        }

        [TestMethod]
        public void InvalidAliasTest()
        {
            Assert.ThrowsException<InvalidOperationException>(() => new DbName("Table", "'My.Alias'"));
            Assert.ThrowsException<InvalidOperationException>(() => new DbName("Table", "\"MyAlias\""));
        }

        [TestMethod]
        public void BypassInvalidNameTest()
        {
            _ = new DbName("My Name", "", false);
            _ = new DbName("Invalid(name)", "", false);
        }

        [TestMethod]
        public void BypassInvalidAliasTest()
        {
            _ = new DbName("Table", "My.Alias", false);
            _ = new DbName("Table", "#MyAlias", false);
        }

    }
}
