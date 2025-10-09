using BaseTest.Models;
using BaseTest.Utils;
using QueryTest.Utils;
using SharpOrm;
using SharpOrm.Builder;

namespace QueryTest
{
    /// <summary>
    /// Integration tests for all new features working together
    /// </summary>
    public class IntegrationTests : DbMockFallbackTest
    {
        public IntegrationTests()
        {
            SetMockConnectionVersion();
        }

        [Fact]
        public void CompleteQueryChain_WithAllFeatures()
        {
            using var query = new Query<SoftDeleteTestTable>();
            var result = query
                .Where(t => t.Number, ">", 100)
                .Where(w => w.Where(t => t.Id, ">", 10))
                .OrWhere(w => w.Where(t => t.Name, "=", "Admin"))
                .WithoutTrashed()
                .Skip(10)
                .Take(20)
                .OrderBy(t => new { t.Id });
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("[Number] >", sql);
            Assert.Contains("[Id] >", sql);
            Assert.Contains("[Name] =", sql);
            Assert.Contains("OR", sql);
            Assert.Equal(10, result.Offset);
            Assert.Equal(20, result.Limit);
        }

        [Fact]
        public void Pagination_WithSoftDelete_AndFilters()
        {
            using var query = new Query<TestTable>();
            query.Where(t => t.Number, ">", 100)
                 .WithTrashed()
                 .Page(2, 25);
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("[Number] >", sql);
            Assert.Equal(Trashed.With, query.Trashed);
            Assert.Equal(25, query.Offset); // (2-1) * 25
            Assert.Equal(25, query.Limit);
        }

        [Fact]
        public void WhereExtensions_AllMethodsWork()
        {
            using var query = new Query<TestTable>();
            query.Where(t => t.Id, 1)
                 .Where(t => t.Name, "Test")
                 .WhereIn(t => t.Nick, "Admin", "Moderator")
                 .WhereNotNull(t => t.CreatedAt)
                 .OrWhere(t => t.Number, 100);
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("[Id] =", sql);
            Assert.Contains("[Name] =", sql);
            Assert.Contains("[Nick] IN", sql);
            Assert.Contains("[record_created] IS NOT NULL", sql);
            Assert.Contains("[Number] =", sql);
            Assert.Contains("OR", sql);
        }

        [Fact]
        public void NestedWhereBuilders_CreateComplexConditions()
        {
            using var query = new Query<TestTable>();
            query.Where(x => x.Where(t => t.Name, "=", "Premium")
                            .Where(t => t.Number, ">", 100))
                 .OrWhere(x => x.Where(t => t.Nick, "=", "Admin"))
                 .Skip(0)
                 .Take(50);
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("[Name] =", sql);
            Assert.Contains("[Number] >", sql);
            Assert.Contains("OR", sql);
            Assert.Equal(0, query.Offset);
            Assert.Equal(50, query.Limit);
        }

        [Fact]
        public void TypedQuery_GetColumnAndGetColumns_Work()
        {
            using var query = new Query<TestTable>();
            var typed = (ITypedQuery<TestTable>)query;
            var singleColumn = typed.GetColumn(t => t.Name);
            var multipleColumns = typed.GetColumns(t => new { t.Id, t.Name, t.Number });
            Assert.NotNull(singleColumn);
            Assert.Equal("Name", singleColumn.Name);
            Assert.NotNull(multipleColumns);
            Assert.Equal(3, multipleColumns.Length);
        }

        [Fact]
        public void WhereBuilder_AndQueryExtensions_WorkTogether()
        {
            using var query = new Query<SoftDeleteTestTable>();
            query.Where(x => x.Where(t => t.Number, ">", 100)
                .WhereNotNull("Name"))
                 .Skip(10)
                 .Take(20)
                 .OnlyTrashed();
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("[Number] >", sql);
            Assert.Contains("IS NOT NULL", sql);
            Assert.Equal(10, query.Offset);
            Assert.Equal(20, query.Limit);
            Assert.Equal(Trashed.Only, query.Trashed);
        }

        [Fact]
        public void CallbackStyle_WhereBuilder_Works()
        {
            using var query = new Query<TestTable>();
            query.Where(w => w
                    .Where(t => t.Id, ">", 100)
                    .Where(t => t.Number, ">", 50))
                 .OrWhere(w => w
                    .Where(t => t.Name, "=", "Admin")
                    .WhereNotNull("Nick"))
                 .Page(1, 50);
            var sqlExpression = query.Grammar().Select();
            var sql = sqlExpression.ToString();
            Assert.Contains("[Id] >", sql);
            Assert.Contains("[Number] >", sql);
            Assert.Contains("[Name] =", sql);
            Assert.Contains("IS NOT NULL", sql);
            Assert.Contains("OR", sql);
            Assert.Equal(0, query.Offset);
            Assert.Equal(50, query.Limit);
        }
    }
}
