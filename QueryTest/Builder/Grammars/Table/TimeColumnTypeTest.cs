using SharpOrm.Builder.Grammars.Table;
using System.Data;

namespace QueryTest.Builder.Grammars.Table
{
    public class TimeColumnTypeTest
    {
        [Fact]
        public void Constructor_ShouldInitializeWithRawType()
        {
            // Arrange
            const string rawType = "TIME";

            // Act
            var columnType = new TimeColumnType(rawType);

            // Assert
            Assert.NotNull(columnType);
        }

        [Fact]
        public void CanWork_WithTimeSpan_ShouldReturnTrue()
        {
            // Arrange
            var columnType = new TimeColumnType("TIME");

            // Act
            var result = columnType.CanWork(typeof(TimeSpan));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanWork_WithString_ShouldReturnFalse()
        {
            // Arrange
            var columnType = new TimeColumnType("TIME");

            // Act
            var result = columnType.CanWork(typeof(string));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanWork_WithInt_ShouldReturnFalse()
        {
            // Arrange
            var columnType = new TimeColumnType("TIME");

            // Act
            var result = columnType.CanWork(typeof(int));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanWork_WithDateTime_ShouldReturnFalse()
        {
            // Arrange
            var columnType = new TimeColumnType("TIME");

            // Act
            var result = columnType.CanWork(typeof(DateTime));

            // Assert
            Assert.False(result);
        }

#if NET6_0_OR_GREATER
        [Fact]
        public void CanWork_WithTimeOnly_ShouldReturnTrue()
        {
            // Arrange
            var columnType = new TimeColumnType("TIME");

            // Act
            var result = columnType.CanWork(typeof(TimeOnly));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Build_WithTimeOnlyColumn_ShouldReturnTimeType()
        {
            // Arrange
            var columnType = new TimeColumnType("TIME");
            var column = new DataColumn("TestColumn", typeof(TimeOnly));

            // Act
            var result = columnType.Build(column);

            // Assert
            Assert.Equal("TIME", result);
        }
#endif

        [Fact]
        public void Build_WithTimeSpanColumn_ShouldReturnTimeType()
        {
            // Arrange
            var columnType = new TimeColumnType("TIME");
            var column = new DataColumn("TestColumn", typeof(TimeSpan));

            // Act
            var result = columnType.Build(column);

            // Assert
            Assert.Equal("TIME", result);
        }

        [Fact]
        public void Build_WithDifferentTimeFormats_ShouldWork()
        {
            // Arrange
            var columnType1 = new TimeColumnType("TIME");
            var columnType2 = new TimeColumnType("TIME(7)");
            var columnType3 = new TimeColumnType("INTERVAL");
            var column = new DataColumn("TestColumn", typeof(TimeSpan));

            // Act
            var result1 = columnType1.Build(column);
            var result2 = columnType2.Build(column);
            var result3 = columnType3.Build(column);

            // Assert
            Assert.Equal("TIME", result1);
            Assert.Equal("TIME(7)", result2);
            Assert.Equal("INTERVAL", result3);
        }

        [Fact]
        public void Build_WithNullColumn_ShouldNotThrow()
        {
            // Arrange
            var columnType = new TimeColumnType("TIME");

            // Act & Assert
            var result = columnType.Build(null);
            Assert.Equal("TIME", result);
        }

        [Fact]
        public void Build_WithEmptyColumnName_ShouldStillWork()
        {
            // Arrange
            var columnType = new TimeColumnType("TIME");
            var column = new DataColumn("", typeof(TimeSpan));

            // Act
            var result = columnType.Build(column);

            // Assert
            Assert.Equal("TIME", result);
        }

        [Fact]
        public void CanWork_WithNullableTimeSpan_ShouldReturnFalse()
        {
            // Arrange
            var columnType = new TimeColumnType("TIME");

            // Act
            var result = columnType.CanWork(typeof(TimeSpan?));

            // Assert
            // CanWork checks the exact type, not nullable types
            Assert.False(result);
        }

        [Fact]
        public void Build_WithCustomTimeFormat_ShouldReturnCustomFormat()
        {
            // Arrange
            const string customFormat = "DURATION";
            var columnType = new TimeColumnType(customFormat);
            var column = new DataColumn("TestColumn", typeof(TimeSpan));

            // Act
            var result = columnType.Build(column);

            // Assert
            Assert.Equal(customFormat, result);
        }

        [Fact]
        public void CanWork_WithObjectType_ShouldReturnFalse()
        {
            // Arrange
            var columnType = new TimeColumnType("TIME");

            // Act
            var result = columnType.CanWork(typeof(object));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Build_WithEmptyRawType_ShouldReturnEmptyString()
        {
            // Arrange
            var columnType = new TimeColumnType("");
            var column = new DataColumn("TestColumn", typeof(TimeSpan));

            // Act
            var result = columnType.Build(column);

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void Build_WithNullRawType_ShouldReturnNull()
        {
            // Arrange
            var columnType = new TimeColumnType(null);
            var column = new DataColumn("TestColumn", typeof(TimeSpan));

            // Act
            var result = columnType.Build(column);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Build_SqlServerTimeFormat_ShouldWork()
        {
            // Arrange
            var columnType = new TimeColumnType("TIME(7)");
            var column = new DataColumn("Duration", typeof(TimeSpan));

            // Act
            var result = columnType.Build(column);

            // Assert
            Assert.Equal("TIME(7)", result);
        }

        [Fact]
        public void Build_MySqlTimeFormat_ShouldWork()
        {
            // Arrange
            var columnType = new TimeColumnType("TIME");
            var column = new DataColumn("Duration", typeof(TimeSpan));

            // Act
            var result = columnType.Build(column);

            // Assert
            Assert.Equal("TIME", result);
        }

        [Fact]
        public void Build_PostgresIntervalFormat_ShouldWork()
        {
            // Arrange
            var columnType = new TimeColumnType("INTERVAL");
            var column = new DataColumn("Duration", typeof(TimeSpan));

            // Act
            var result = columnType.Build(column);

            // Assert
            Assert.Equal("INTERVAL", result);
        }

        [Fact]
        public void CanWork_WithDecimal_ShouldReturnFalse()
        {
            // Arrange
            var columnType = new TimeColumnType("TIME");

            // Act
            var result = columnType.CanWork(typeof(decimal));

            // Assert
            Assert.False(result);
        }
    }
}
