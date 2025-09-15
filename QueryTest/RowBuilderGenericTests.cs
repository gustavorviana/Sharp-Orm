using NSubstitute;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using SharpOrm.ForeignKey;

namespace QueryTest
{
    public class RowBuilderGenericTests
    {
        private readonly QueryConfig _queryConfig = new SqlServerQueryConfig();

        [Fact]
        public void Constructor_WithValidConfig_ShouldInitializeCorrectly()
        {
            // Act
            var rowBuilder = new RowBuilder<TestEntity>(_queryConfig);

            // Assert
            Assert.NotNull(rowBuilder);
            Assert.NotNull(((IWithQueryInfo)rowBuilder).Info);
        }

        [Fact]
        public void Constructor_WithSettings_ShouldApplySettings()
        {
            // Arrange
            var settings = new ObjectReaderSettings();

            // Act
            var builder = new RowBuilder<TestEntity>(_queryConfig, settings);

            // Assert
            Assert.NotNull(builder);
        }

        [Fact]
        public void Add_WithTypedColumnExpression_ShouldAddValue()
        {
            // Arrange
            var rowBuilder = new RowBuilder<TestEntity>(_queryConfig);
            const int value = 123;

            // Act
            var result = rowBuilder.Add(x => x.Id, value);

            // Assert
            Assert.Same(rowBuilder, result);
        }

        [Fact]
        public void Add_WithObjectAndColumns_ShouldAddSpecifiedColumns()
        {
            // Arrange
            var rowBuilder = new RowBuilder<TestEntity>(_queryConfig);
            var entity = new TestEntity { Id = 1, Name = "Test" };

            // Act
            var result = rowBuilder.Add(entity, x => x.Name);

            // Assert
            Assert.Same(rowBuilder, result);
        }

        [Fact]
        public void AddExcept_WithObjectAndExcludedColumn_ShouldAddAllExceptSpecified()
        {
            // Arrange
            var rowBuilder = new RowBuilder<TestEntity>(_queryConfig);
            var entity = new TestEntity { Id = 1, Name = "Test" };

            // Act
            var result = rowBuilder.AddExcept(entity, x => x.Id);

            // Assert
            Assert.Same(rowBuilder, result);
        }

        [Fact]
        public void Add_WithPrefixAndObject_ShouldAddWithPrefix()
        {
            // Arrange
            var rowBuilder = new RowBuilder<TestEntity>(_queryConfig);
            const string prefix = "Entity_";
            var entity = new TestEntity { Id = 1, Name = "Test" };

            // Act
            var result = rowBuilder.Add(prefix, entity);

            // Assert
            Assert.Same(rowBuilder, result);
        }

        [Fact]
        public void Contains_WithTypedColumn_ShouldCheckExistence()
        {
            // Arrange
            var rowBuilder = new RowBuilder<TestEntity>(_queryConfig);
            rowBuilder.Add("Id", 1);

            // Act
            var result = rowBuilder.Contains<int>(x => x.Id);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Remove_WithTypedColumn_ShouldRemoveColumn()
        {
            // Arrange
            var rowBuilder = new RowBuilder<TestEntity>(_queryConfig);
            rowBuilder.Add("Id", 1);

            // Act
            var result = rowBuilder.Remove<int>(x => x.Id);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        public void Add_WithVariousIntegerValues_ShouldWork(int value)
        {
            // Arrange
            var rowBuilder = new RowBuilder<TestEntity>(_queryConfig);

            // Act
            var result = rowBuilder.Add(x => x.Id, value);

            // Assert
            Assert.Same(rowBuilder, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Test")]
        [InlineData("   ")]
        [InlineData("Special@#$%Characters")]
        public void Add_WithVariousStringValues_ShouldWork(string value)
        {
            // Arrange
            var rowBuilder = new RowBuilder<TestEntity>(_queryConfig);

            // Act
            var result = rowBuilder.Add(x => x.Name, value);

            // Assert
            Assert.Same(rowBuilder, result);
        }
    }

}
