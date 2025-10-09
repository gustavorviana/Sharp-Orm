using BaseTest.Models;
using BaseTest.Utils;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;
namespace QueryTest
{
    public class TypedQueryTests : DbMockFallbackTest
    {
        public TypedQueryTests()
        {
            SetMockConnectionVersion();
        }

        [Fact]
        public void ITypedQuery_TableInfo_IsAccessible()
        {
            using var query = new Query<TestTable>();
            var typedQuery = (ITypedQuery<TestTable>)query;
            Assert.NotNull(typedQuery.TableInfo);
            Assert.Equal(typeof(TestTable), typedQuery.TableInfo.Type);
        }

        [Fact]
        public void ITypedQuery_GetColumn_ReturnsCorrectColumn()
        {
            using var query = new Query<TestTable>();
            var typedQuery = (ITypedQuery<TestTable>)query;
            var column = typedQuery.GetColumn(t => t.Name);
            Assert.NotNull(column);
            Assert.Equal("Name", column.Name);
        }

        [Fact]
        public void ITypedQuery_GetColumns_ReturnsMultipleColumns()
        {
            using var query = new Query<TestTable>();
            var typedQuery = (ITypedQuery<TestTable>)query;
            var columns = typedQuery.GetColumns(t => new { t.Id, t.Name });
            Assert.NotNull(columns);
            Assert.Equal(2, columns.Length);
        }

        [Fact]
        public void ITypedQuery_GetColumns_WithConfig()
        {
            using var query = new Query<TestTable>();
            var typedQuery = (ITypedQuery<TestTable>)query;
            var columns = typedQuery.GetColumns(
                t => new { t.Id, t.Name },
                SharpOrm.Builder.Expressions.ExpressionConfig.All);
            Assert.NotNull(columns);
            Assert.Equal(2, columns.Length);
        }

        [Fact]
        public void Query_SelectCanBeOverridden()
        {
            using var query = new Query<TestTable>();
            query.Select("Name");
            Assert.Single(query.Info.Select);
            Assert.Equal("Name", query.Info.Select[0].Name);
        }

        [Fact]
        public void Query_SelectStar_Works()
        {
            using var query = new Query<TestTable>();
            query.Select("*");
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Equal("SELECT [Id], [Id2], [Name], [Nick], [record_created], [Number], [custom_id], [custom_status] FROM [TestTable]", sql);
        }
    }
}
