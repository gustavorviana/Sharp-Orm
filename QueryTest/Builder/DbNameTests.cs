using BaseTest.Models;
using SharpOrm.Builder;
using System.ComponentModel.DataAnnotations.Schema;

namespace QueryTest.Builder
{
    public class DbNameTests
    {

        [Fact]
        public void InvalidNameTest()
        {
            Assert.Throws<InvalidOperationException>(() => new DbName("My Name", ""));
            Assert.Throws<InvalidOperationException>(() => new DbName("Invalid(name)", ""));
            _ = new DbName("Schema.Table");
            _ = new DbName("#TempTable");
        }

        [Fact]
        public void InvalidAliasTest()
        {
            Assert.Throws<InvalidOperationException>(() => new DbName("Table", "'My.Alias'"));
            Assert.Throws<InvalidOperationException>(() => new DbName("Table", "\"MyAlias\""));
        }

        [Fact]
        public void BypassInvalidNameTest()
        {
            _ = new DbName("My Name", "", false);
            _ = new DbName("Invalid(name)", "", false);
        }

        [Fact]
        public void BypassInvalidAliasTest()
        {
            _ = new DbName("Table", "My.Alias", false);
            _ = new DbName("Table", "#MyAlias", false);
        }

        [Fact]
        public void GetNameOf()
        {
            var name = DbName.Of<Order>("");

            Assert.Equal("Orders", name.Name);
        }

        [Fact]
        public void GetNameWithSchemaOf()
        {
            var name = DbName.Of<Product>("");

            Assert.Equal("Orders.Product", name.Name);
        }

        [Table("Product", Schema = "Orders")]
        private class Product
        {
        }
    }
}
