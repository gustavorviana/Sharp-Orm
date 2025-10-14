using BaseTest.Models;
using SharpOrm.Builder.Grammars.Table;
using System.Data;

namespace QueryTest.Builder.Grammars.Table
{
    public class EnumColumnTypeTest
    {
        // Test enum for testing purposes
        private enum TestEnum
        {
            Value1,
            Value2,
            Value3
        }

        [Flags]
        private enum TestFlagsEnum
        {
            None = 0,
            Flag1 = 1,
            Flag2 = 2,
            Flag3 = 4
        }

        [Fact]
        public void Constructor_ShouldInitializeWithRawType()
        {
            // Arrange
            const string rawType = "INT";

            // Act
            var columnType = new EnumColumnType(rawType);

            // Assert
            Assert.NotNull(columnType);
        }

        [Fact]
        public void CanWork_WithEnumType_ShouldReturnTrue()
        {
            // Arrange
            var columnType = new EnumColumnType("INT");

            // Act
            var result = columnType.CanWork(typeof(TestEnum));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanWork_WithFlagsEnumType_ShouldReturnTrue()
        {
            // Arrange
            var columnType = new EnumColumnType("INT");

            // Act
            var result = columnType.CanWork(typeof(TestFlagsEnum));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanWork_WithStatusEnum_ShouldReturnTrue()
        {
            // Arrange
            var columnType = new EnumColumnType("INT");

            // Act
            var result = columnType.CanWork(typeof(Status));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanWork_WithStringType_ShouldReturnFalse()
        {
            // Arrange
            var columnType = new EnumColumnType("INT");

            // Act
            var result = columnType.CanWork(typeof(string));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanWork_WithIntType_ShouldReturnFalse()
        {
            // Arrange
            var columnType = new EnumColumnType("INT");

            // Act
            var result = columnType.CanWork(typeof(int));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanWork_WithClassType_ShouldReturnFalse()
        {
            // Arrange
            var columnType = new EnumColumnType("INT");

            // Act
            var result = columnType.CanWork(typeof(Customer));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanWork_WithNullableEnumType_ShouldReturnFalse()
        {
            // Arrange
            var columnType = new EnumColumnType("INT");

            // Act
            var result = columnType.CanWork(typeof(TestEnum?));

            // Assert
            // CanWork checks the exact type, not nullable types
            Assert.False(result);
        }

        [Fact]
        public void Build_ShouldReturnRawType()
        {
            // Arrange
            const string rawType = "INT";
            var columnType = new EnumColumnType(rawType);
            var column = new DataColumn("TestColumn", typeof(TestEnum));

            // Act
            var result = columnType.Build(column);

            // Assert
            Assert.Equal(rawType, result);
        }

        [Fact]
        public void Build_WithDifferentRawTypes_ShouldReturnCorrectType()
        {
            // Arrange
            var columnType1 = new EnumColumnType("TINYINT");
            var columnType2 = new EnumColumnType("SMALLINT");
            var columnType3 = new EnumColumnType("INT");
            var column = new DataColumn("TestColumn", typeof(TestEnum));

            // Act
            var result1 = columnType1.Build(column);
            var result2 = columnType2.Build(column);
            var result3 = columnType3.Build(column);

            // Assert
            Assert.Equal("TINYINT", result1);
            Assert.Equal("SMALLINT", result2);
            Assert.Equal("INT", result3);
        }

        [Fact]
        public void Build_WithStringRawType_ShouldReturnStringType()
        {
            // Arrange
            const string rawType = "VARCHAR(50)";
            var columnType = new EnumColumnType(rawType);
            var column = new DataColumn("TestColumn", typeof(TestEnum));

            // Act
            var result = columnType.Build(column);

            // Assert
            Assert.Equal(rawType, result);
        }

        [Fact]
        public void Build_WithNullColumn_ShouldNotThrow()
        {
            // Arrange
            var columnType = new EnumColumnType("INT");

            // Act & Assert
            var result = columnType.Build(null);
            Assert.Equal("INT", result);
        }

        [Fact]
        public void Build_WithEmptyColumnName_ShouldStillWork()
        {
            // Arrange
            var columnType = new EnumColumnType("INT");
            var column = new DataColumn("", typeof(TestEnum));

            // Act
            var result = columnType.Build(column);

            // Assert
            Assert.Equal("INT", result);
        }

        [Fact]
        public void Build_WithCustomEnumType_ShouldReturnRawType()
        {
            // Arrange
            var columnType = new EnumColumnType("BIGINT");
            var column = new DataColumn("StatusColumn", typeof(Status));

            // Act
            var result = columnType.Build(column);

            // Assert
            Assert.Equal("BIGINT", result);
        }

        [Fact]
        public void CanWork_WithObjectType_ShouldReturnFalse()
        {
            // Arrange
            var columnType = new EnumColumnType("INT");

            // Act
            var result = columnType.CanWork(typeof(object));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanWork_WithDateTimeType_ShouldReturnFalse()
        {
            // Arrange
            var columnType = new EnumColumnType("INT");

            // Act
            var result = columnType.CanWork(typeof(DateTime));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Build_WithEmptyRawType_ShouldReturnEmptyString()
        {
            // Arrange
            var columnType = new EnumColumnType("");
            var column = new DataColumn("TestColumn", typeof(TestEnum));

            // Act
            var result = columnType.Build(column);

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void Build_WithNullRawType_ShouldReturnNull()
        {
            // Arrange
            var columnType = new EnumColumnType(null);
            var column = new DataColumn("TestColumn", typeof(TestEnum));

            // Act
            var result = columnType.Build(column);

            // Assert
            Assert.Null(result);
        }
    }
}
