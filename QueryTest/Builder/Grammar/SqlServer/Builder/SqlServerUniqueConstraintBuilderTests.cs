using SharpOrm.Builder.Grammars.SqlServer.Builder;
using SharpOrm.Builder.Grammars.Table.Constraints;

namespace QueryTest.Builder.Grammar.SqlServer.Builder
{
    public class SqlServerUniqueConstraintBuilderTests
    {
        private readonly SqlServerUniqueConstraintBuilder _builder;

        public SqlServerUniqueConstraintBuilderTests()
        {
            _builder = new SqlServerUniqueConstraintBuilder();
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
            Assert.Equal("CONSTRAINT [UC_Users_Email] UNIQUE ([Email])", result.ToString());
        }

        [Fact]
        public void Build_MultipleColumns_GeneratesCorrectSql()
        {
            // Arrange
            var constraint = new UniqueConstraint("Users", new[] { "FirstName", "LastName", "BirthDate" }, "UC_Users_Name_Birth");

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("CONSTRAINT [UC_Users_Name_Birth] UNIQUE ([FirstName],[LastName],[BirthDate])", result.ToString());
        }

        [Fact]
        public void Build_WithoutConstraintName_UsesDefaultName()
        {
            // Arrange
            var constraint = new UniqueConstraint("Users", "Email");

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Contains("CONSTRAINT [UC_Users_Email]", result.ToString());
        }
    }
}
