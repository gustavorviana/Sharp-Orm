using BaseTest.Fixtures;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using Xunit.Abstractions;

namespace QueryTest.Mysql
{
    public class MergeTests(ITestOutputHelper output, MockFixture<MysqlQueryConfig> connection) : DbGrammarTestBase(output, connection), IClassFixture<MockFixture<MysqlQueryConfig>>
    {
        [Fact]
        public void MergeTest()
        {
            var expected = new SqlExpression("INSERT INTO `TargetTable` (`Name`, `Description`, `Status`) SELECT `Source`.`Name`, `Source`.`Description`, `Source`.`Status` FROM `SrcTable` `Source` ON DUPLICATE KEY UPDATE `Name`=`Source`.`Name`, `Description`=`Source`.`Description`;");

            using var query = new Query("TargetTable");

            var result = query.GetGrammar().Merge(
                new DbName("SrcTable"),
                ["Id", "Description"],
                ["Name", "Description"],
                ["Name", "Description", "Status"]
            );

            QueryAssert.Equal(expected, result);
        }

        [Fact]
        public void MergeWithAlias()
        {
            var expected = new SqlExpression("INSERT INTO `TargetTable` (`Name`, `Description`, `Status`) SELECT `Src`.`Name`, `Src`.`Description`, `Src`.`Status` FROM `SrcTable` `Src` ON DUPLICATE KEY UPDATE `Name`=`Src`.`Name`, `Description`=`Src`.`Description`;");

            using var query = new Query("TargetTable Tgt");

            var result = query.GetGrammar().Merge(
                new DbName("SrcTable Src"),
                ["Id", "Description"],
                ["Name", "Description"],
                ["Name", "Description", "Status"]
            );

            QueryAssert.Equal(expected, result);
        }
    }
}
