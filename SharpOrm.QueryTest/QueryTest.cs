using BaseTest.Mock;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.Connection;
using Xunit.Abstractions;

namespace QueryTest
{
    public class QueryTest(ITestOutputHelper? output) : MockTest(output)
    {
        [Fact]
        public void OrderBy()
        {
            var query = new Query("table");
            query.OrderBy(SharpOrm.OrderBy.None, "Col1");
            Assert.Empty(query.Info.Orders);

            query.OrderBy(SharpOrm.OrderBy.Asc, "Col2");
            Assert.Single(query.Info.Orders);

            query.OrderBy(SharpOrm.OrderBy.Desc, "3");
            Assert.Single(query.Info.Orders);
        }

        [Fact]
        public void WhereQuery()
        {
            var query = new Query("table");
            var toWhereQuery = Query.ReadOnly("ToWhereQuery");

            Assert.Throws<InvalidOperationException>(() => query.Where("Column", toWhereQuery));

            toWhereQuery.Select("Column");
            Assert.Throws<InvalidOperationException>(() => query.Where("Column", toWhereQuery));

            toWhereQuery.Limit = 1;
            query.Where("Column", toWhereQuery);
        }

        [Fact]
        public void Clone()
        {
            var original = new Query("table alias")
            {
                Limit = 1,
                Offset = 3,
                Distinct = true
            };

            original.OrderBy("Id");
            original.Select("Col1", "Col2");
            original.WhereColumn("Col1", "=", "Col2");

            Assert.Equal(original.ToString(), original.Clone(true).ToString());
            var clone = original.Clone(false);

            var cloneQuery = original.Clone(false);
            cloneQuery.OrderBy("Id");

            Assert.NotEqual(original.ToString(), cloneQuery.ToString());
            Assert.Equal(original.Limit, clone.Limit);
            Assert.Equal(original.Offset, clone.Offset);
            Assert.Equal(original.Distinct, clone.Distinct);
        }

        [Fact]
        public void DefaultTimeoutTest()
        {
            var query = new Query("table");

            using var cmd = query.GetCommand(new SqlExpression(""));
            Assert.Equal(30, cmd.CommandTimeout);
        }

        [Fact]
        public void QueryCustomTimeoutTest()
        {
            var query = new Query("table")
            {
                CommandTimeout = 120
            };

            using var cmd = query.GetCommand(new SqlExpression(""));
            Assert.Equal(120, cmd.CommandTimeout);
        }

        [Fact]
        public void ConfigCustomTimeoutTest()
        {
            using var creator = new MultipleConnectionCreator<MockConnection>(new SqlServerQueryConfig(false) { CommandTimeout = 120 }, "");
            var query = new Query("table", creator);

            using var cmd = query.GetCommand(new SqlExpression(""));
            Assert.Equal(120, cmd.CommandTimeout);
        }
    }
}
