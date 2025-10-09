using SharpOrm;
using SharpOrm.Builder;

namespace QueryTest
{
    public class CaseTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_Default_CreatesEmptyCase()
        {
            var caseStatement = new Case();
            Assert.NotNull(caseStatement);
        }

        [Fact]
        public void Constructor_FromColumn_CopiesColumn()
        {
            var column = new Column("TestColumn", "TestAlias");
            var caseStatement = new Case(column);

            Assert.NotNull(caseStatement);
            Assert.Equal("TestAlias", caseStatement.Alias);
        }

        [Fact]
        public void Constructor_FromDbName_CreatesWithDbName()
        {
            var dbName = new DbName("TestTable", "TestColumn");
            var caseStatement = new Case(dbName);

            Assert.NotNull(caseStatement);
        }

        [Fact]
        public void Constructor_FromString_CreatesWithColumnName()
        {
            var caseStatement = new Case("TestColumn");

            Assert.NotNull(caseStatement);
        }

        [Fact]
        public void Constructor_FromStringWithAlias_CreatesWithColumnNameAndAlias()
        {
            var caseStatement = new Case("TestColumn", "TestAlias");

            Assert.NotNull(caseStatement);
            Assert.Equal("TestAlias", caseStatement.Alias);
        }

        #endregion

        #region When Tests

        [Fact]
        public void When_WithColumnAndValue_AddsCondition()
        {
            var caseStatement = new Case();
            var result = caseStatement.When("status", 1, "Active");

            Assert.NotNull(result);
            Assert.Same(caseStatement, result);
        }

        [Fact]
        public void When_WithColumnOperationAndValue_AddsCondition()
        {
            var caseStatement = new Case();
            var result = caseStatement.When("age", ">", 18, "Adult");

            Assert.NotNull(result);
            Assert.Same(caseStatement, result);
        }

        [Fact]
        public void When_WithSqlExpression_AddsCondition()
        {
            var caseStatement = new Case();
            var expression = new SqlExpression("status = ?", 1);
            var result = caseStatement.When(expression, "Active");

            Assert.NotNull(result);
            Assert.Same(caseStatement, result);
        }

        [Fact]
        public void When_WithExpectedValue_AddsCondition()
        {
            var caseStatement = new Case("status");
            var result = caseStatement.When(1, "Active");

            Assert.NotNull(result);
            Assert.Same(caseStatement, result);
        }

        [Fact]
        public void When_WithQueryCallback_AddsCondition()
        {
            var caseStatement = new Case();
            var result = caseStatement.When(q => q.Where("age", ">", 18), "Adult");

            Assert.NotNull(result);
            Assert.Same(caseStatement, result);
        }

        [Fact]
        public void When_WithCollectionAsThen_ThrowsNotSupportedException()
        {
            var caseStatement = new Case();
            var collection = new List<int> { 1, 2, 3 };

            Assert.Throws<NotSupportedException>(() => caseStatement.When("status", 1, collection));
        }

        [Fact]
        public void When_WithInvalidOperation_ThrowsException()
        {
            var caseStatement = new Case();

            Assert.Throws<InvalidOperationException>(() => caseStatement.When("status", "INVALID_OP", 1, "Active"));
        }

        #endregion

        #region WhenNull Tests

        [Fact]
        public void WhenNull_AddsNullCheckCondition()
        {
            var caseStatement = new Case();
            var result = caseStatement.WhenNull("description", "No Description");

            Assert.NotNull(result);
            Assert.Same(caseStatement, result);
        }

        [Fact]
        public void WhenNull_GeneratesCorrectSql()
        {
            var caseStatement = new Case();
            caseStatement.WhenNull("description", "No Description")
                         .Else("Has Description");

            var info = GetInfo();
            var expression = caseStatement.ToExpression(info, false);

            Assert.Contains("IS NULL", expression.ToString());
        }

        #endregion

        #region WhenNotNull Tests

        [Fact]
        public void WhenNotNull_AddsNotNullCheckCondition()
        {
            var caseStatement = new Case();
            var result = caseStatement.WhenNotNull("description", "Has Description");

            Assert.NotNull(result);
            Assert.Same(caseStatement, result);
        }

        [Fact]
        public void WhenNotNull_GeneratesCorrectSql()
        {
            var caseStatement = new Case();
            caseStatement.WhenNotNull("description", "Has Description")
                         .Else("No Description");

            var info = GetInfo();
            var expression = caseStatement.ToExpression(info, false);

            Assert.Contains("IS NOT NULL", expression.ToString());
        }

        #endregion

        #region Else Tests

        [Fact]
        public void Else_SetsDefaultValue()
        {
            var caseStatement = new Case();
            caseStatement.When("status", 1, "Active");
            var result = caseStatement.Else("Inactive");

            Assert.NotNull(result);
        }

        [Fact]
        public void Else_WithCollection_ThrowsNotSupportedException()
        {
            var caseStatement = new Case();
            caseStatement.When("status", 1, "Active");
            var collection = new List<int> { 1, 2, 3 };

            Assert.Throws<NotSupportedException>(() => caseStatement.Else(collection));
        }

        [Fact]
        public void Else_WithNull_ConvertsToDBNull()
        {
            var caseStatement = new Case();
            caseStatement.When("status", 1, "Active");
            var result = caseStatement.Else(null);

            Assert.NotNull(result);
        }

        #endregion

        #region ToExpression Tests

        [Fact]
        public void ToExpression_WithNoConditions_ThrowsInvalidOperationException()
        {
            var caseStatement = new Case();
            var info = GetInfo();

            Assert.Throws<InvalidOperationException>(() => caseStatement.ToExpression(info, false));
        }

        [Fact]
        public void ToExpression_WithSimpleCondition_GeneratesCorrectSql()
        {
            var caseStatement = new Case();
            caseStatement.When("status", 1, "Active")
                         .Else("Inactive");

            var info = GetInfo();
            var expression = caseStatement.ToExpression(info, false);
            var sql = expression.ToString();

            Assert.Contains("CASE", sql);
            Assert.Contains("WHEN", sql);
            Assert.Contains("THEN", sql);
            Assert.Contains("ELSE", sql);
            Assert.Contains("END", sql);
        }

        [Fact]
        public void ToExpression_WithAlias_IncludesAlias()
        {
            var caseStatement = new Case { Alias = "StatusLabel" };
            caseStatement.When("status", 1, "Active")
                         .Else("Inactive");

            var info = GetInfo();
            var expression = caseStatement.ToExpression(info, true);
            var sql = expression.ToString();

            Assert.Contains("AS [StatusLabel]", sql);
        }

        [Fact]
        public void ToExpression_WithoutAlias_DoesNotIncludeAlias()
        {
            var caseStatement = new Case { Alias = "StatusLabel" };
            caseStatement.When("status", 1, "Active")
                         .Else("Inactive");

            var info = GetInfo();
            var expression = caseStatement.ToExpression(info, false);
            var sql = expression.ToString();

            Assert.DoesNotContain("AS [StatusLabel]", sql);
        }

        [Fact]
        public void ToExpression_WithMultipleConditions_GeneratesCorrectSql()
        {
            var caseStatement = new Case();
            caseStatement.When("status", 1, "Active")
                         .When("status", 2, "Pending")
                         .When("status", 3, "Cancelled")
                         .Else("Unknown");

            var info = GetInfo();
            var expression = caseStatement.ToExpression(info, false);
            var sql = expression.ToString();

            Assert.Contains("CASE", sql);
            Assert.Equal(3, CountOccurrences(sql, "WHEN"));
            Assert.Equal(3, CountOccurrences(sql, "THEN"));
            Assert.Contains("ELSE", sql);
            Assert.Contains("END", sql);
        }

        [Fact]
        public void ToExpression_WithColumnName_IncludesColumnName()
        {
            var caseStatement = new Case("status");
            caseStatement.When(1, "Active")
                         .When(2, "Pending")
                         .Else("Unknown");

            var info = GetInfo();
            var expression = caseStatement.ToExpression(info, false);
            var sql = expression.ToString();

            Assert.Contains("CASE [status]", sql);
        }

        [Fact]
        public void ToExpression_WithoutElse_GeneratesCorrectSql()
        {
            var caseStatement = new Case();
            caseStatement.When("status", 1, "Active");

            var info = GetInfo();
            var expression = caseStatement.ToExpression(info, false);
            var sql = expression.ToString();

            Assert.Contains("CASE", sql);
            Assert.Contains("WHEN", sql);
            Assert.Contains("THEN", sql);
            Assert.DoesNotContain("ELSE", sql);
            Assert.Contains("END", sql);
        }

        [Fact]
        public void ToExpression_WithSqlExpressionAsThen_GeneratesCorrectSql()
        {
            var caseStatement = new Case();
            var thenExpression = new SqlExpression("UPPER(?)", "active");
            caseStatement.When("status", 1, thenExpression)
                         .Else("Unknown");

            var info = GetInfo();
            var expression = caseStatement.ToExpression(info, false);

            Assert.NotNull(expression);
            Assert.Contains("THEN", expression.ToString());
        }

        [Fact]
        public void ToExpression_WithDifferentOperations_GeneratesCorrectSql()
        {
            var caseStatement = new Case();
            caseStatement.When("age", ">", 18, "Adult")
                         .When("age", ">=", 13, "Teenager")
                         .Else("Child");

            var info = GetInfo();
            var expression = caseStatement.ToExpression(info, false);
            var sql = expression.ToString();

            Assert.Contains("CASE", sql);
            Assert.Contains("WHEN", sql);
            Assert.Contains("THEN", sql);
            Assert.Contains("END", sql);
        }

        [Fact]
        public void ToExpression_ChainedConditions_MaintainsOrder()
        {
            var caseStatement = new Case();
            caseStatement.When("priority", 1, "High")
                         .When("priority", 2, "Medium")
                         .When("priority", 3, "Low")
                         .Else("Unknown");

            var info = GetInfo();
            var expression = caseStatement.ToExpression(info, false);

            Assert.NotNull(expression);
            var sql = expression.ToString();
            Assert.Contains("CASE", sql);
            Assert.Contains("END", sql);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void ComplexCase_WithMixedConditions_Works()
        {
            var caseStatement = new Case("status", "StatusDescription");
            caseStatement.WhenNull("status", "No Status")
                         .When(1, "Active")
                         .When(2, "Inactive")
                         .WhenNotNull("status", "Has Status")
                         .Else("Unknown");

            var info = GetInfo();
            var expression = caseStatement.ToExpression(info, true);

            Assert.NotNull(expression);
            var sql = expression.ToString();
            Assert.Contains("CASE", sql);
            Assert.Contains("END", sql);
            Assert.Contains("AS [StatusDescription]", sql);
        }

        [Fact]
        public void Case_WithQueryCallback_GeneratesCorrectSql()
        {
            var caseStatement = new Case();
            caseStatement.When(q => q.Where("age", ">", 18).Where("country", "BR"), "Brazilian Adult")
                         .Else("Other");

            var info = GetInfo();
            var expression = caseStatement.ToExpression(info, false);

            Assert.NotNull(expression);
        }

        [Fact]
        public void Case_WithDbName_GeneratesCorrectSql()
        {
            var dbName = new DbName("users", "status");
            var caseStatement = new Case(dbName);
            caseStatement.When(1, "Active")
                         .Else("Inactive");

            var info = GetInfo();
            var expression = caseStatement.ToExpression(info, false);

            Assert.NotNull(expression);
            Assert.Contains("CASE", expression.ToString());
        }

        #endregion

        #region Helper Methods

        private static ReadonlyQueryInfo GetInfo()
        {
            return new ReadonlyQueryInfo(new SqlServerQueryConfig(), new DbName("TestTable"));
        }

        private static int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(pattern, index)) != -1)
            {
                count++;
                index += pattern.Length;
            }
            return count;
        }

        #endregion
    }
}
