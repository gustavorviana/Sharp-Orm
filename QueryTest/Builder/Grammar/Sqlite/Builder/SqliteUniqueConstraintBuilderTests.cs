using SharpOrm.Builder.Grammars.Sqlite.Builder;
using SharpOrm.Builder.Grammars.Table.Constraints;

namespace QueryTest.Builder.Grammar.Sqlite.Builder
{
    public class SqliteUniqueConstraintBuilderTests
    {
        private readonly SqliteUniqueConstraintBuilder _builder;

        public SqliteUniqueConstraintBuilderTests()
        {
            _builder = new SqliteUniqueConstraintBuilder();
        }

        [Fact]
        public void CanWork_WithUniqueConstraint_ReturnsTrue()
        {
            // Act
            var result = _builder.CanWork(typeof(UniqueConstraint));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Build_SingleColumn_GeneratesCorrectSql()
        {
            // Arrange
            var constraint = new UniqueConstraint("Users", "Email", "UC_Users_Email");

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("CONSTRAINT \"UC_Users_Email\" UNIQUE (\"Email\")", result.ToString());
        }

        [Fact]
        public void Build_MultipleColumns_GeneratesCorrectSql()
        {
            // Arrange
            var constraint = new UniqueConstraint("Users", new[] { "FirstName", "LastName", "BirthDate" });

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.StartsWith("CONSTRAINT \"UC_Users_FirstName_LastName_BirthDate\"", result.ToString());
            Assert.EndsWith("UNIQUE (\"FirstName\",\"LastName\",\"BirthDate\")", result.ToString());
        }
    }
}
