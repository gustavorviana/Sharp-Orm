using SharpOrm.Builder.Grammars.Mysql.Builder;
using SharpOrm.Builder.Grammars.Table.Constraints;

namespace QueryTest.Builder.Grammar.Mysql.Builder
{
    public class MySqlUniqueConstraintBuilderTests
    {
        private readonly MySqlUniqueConstraintBuilder _builder;

        public MySqlUniqueConstraintBuilderTests()
        {
            _builder = new MySqlUniqueConstraintBuilder();
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
            Assert.Equal("CONSTRAINT `UC_Users_Email` UNIQUE (`Email`)", result.ToString());
        }

        [Fact]
        public void Build_MultipleColumns_GeneratesCorrectSql()
        {
            // Arrange
            var constraint = new UniqueConstraint("Products", ["Name", "CategoryId"], "UC_Products_Name_Category");

            // Act
            var result = _builder.Build(constraint);

            // Assert
            Assert.Equal("CONSTRAINT `UC_Products_Name_Category` UNIQUE (`Name`,`CategoryId`)", result.ToString());
        }
    }
}
