using SharpOrm.Builder;

namespace QueryTest
{
    public class DbNameTest
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
    }
}
