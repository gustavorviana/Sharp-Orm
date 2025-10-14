using SharpOrm.Builder.Grammars.Table;
using System.Data;

namespace QueryTest.Builder.Grammars.Table
{
    public class DateColumnTypeTest
    {
        [Fact]
        public void Constructor_ShouldInitializeWithParameters()
        {
            // Arrange
            const string dateTimeOffset = "DATETIMEOFFSET";
            const string dateTime = "DATETIME";
            const string dateOnly = "DATE";

            // Act
            var columnType = new DateColumnType(dateTimeOffset, dateTime, dateOnly);

            // Assert
            Assert.NotNull(columnType);
        }

        [Fact]
        public void CanWork_WithDateTime_ShouldReturnTrue()
        {
            // Arrange
            var columnType = new DateColumnType("DATETIMEOFFSET", "DATETIME", "DATE");

            // Act
            var result = columnType.CanWork(typeof(DateTime));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanWork_WithString_ShouldReturnFalse()
        {
            // Arrange
            var columnType = new DateColumnType("DATETIMEOFFSET", "DATETIME", "DATE");

            // Act
            var result = columnType.CanWork(typeof(string));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanWork_WithInt_ShouldReturnFalse()
        {
            // Arrange
            var columnType = new DateColumnType("DATETIMEOFFSET", "DATETIME", "DATE");

            // Act
            var result = columnType.CanWork(typeof(int));

            // Assert
            Assert.False(result);
        }

#if NET6_0_OR_GREATER
        [Fact]
        public void CanWork_WithDateOnly_ShouldReturnTrue()
        {
            // Arrange
            var columnType = new DateColumnType("DATETIMEOFFSET", "DATETIME", "DATE");

            // Act
            var result = columnType.CanWork(typeof(DateOnly));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Build_WithDateOnlyColumn_ShouldReturnDateOnlyType()
        {
            // Arrange
            var columnType = new DateColumnType("DATETIMEOFFSET", "DATETIME", "DATE");
            var column = new DataColumn("TestColumn", typeof(DateOnly));

            // Act
            var result = columnType.Build(column);

            // Assert
            Assert.Equal("DATE", result);
        }
#endif

        [Fact]
        public void Build_WithDateTimeColumn_ShouldReturnDateTimeType()
        {
            // Arrange
            var columnType = new DateColumnType("DATETIMEOFFSET", "DATETIME", "DATE");
            var column = new DataColumn("TestColumn", typeof(DateTime));

            // Act
            var result = columnType.Build(column);

            // Assert
            Assert.Equal("DATETIME", result);
        }

        [Fact]
        public void Build_WithDateTimeOffsetColumn_ShouldReturnDateTimeType()
        {
            // Arrange
            var columnType = new DateColumnType("DATETIMEOFFSET", "DATETIME", "DATE");
            var column = new DataColumn("TestColumn", typeof(DateTimeOffset));

            // Act
            var result = columnType.Build(column);

            // Assert
            // DateTimeOffset is not specifically handled, so it defaults to DateTime
            Assert.Equal("DATETIME", result);
        }

        [Fact]
        public void Build_WithDifferentDateTimeFormats_ShouldWork()
        {
            // Arrange
            var columnType1 = new DateColumnType("DATETIMEOFFSET", "DATETIME2", "DATE");
            var columnType2 = new DateColumnType("DATETIMEOFFSET", "DATETIME", "DATE");
            var column = new DataColumn("TestColumn", typeof(DateTime));

            // Act
            var result1 = columnType1.Build(column);
            var result2 = columnType2.Build(column);

            // Assert
            Assert.Equal("DATETIME2", result1);
            Assert.Equal("DATETIME", result2);
        }

        [Fact]
        public void Build_WithNullDateTimeOffset_ShouldStillWork()
        {
            // Arrange
            var columnType = new DateColumnType(null, "DATETIME", "DATE");
            var column = new DataColumn("TestColumn", typeof(DateTime));

            // Act
            var result = columnType.Build(column);

            // Assert
            Assert.Equal("DATETIME", result);
        }

        [Fact]
        public void Build_WithCustomDateTimeFormat_ShouldReturnCustomFormat()
        {
            // Arrange
            const string customFormat = "TIMESTAMP";
            var columnType = new DateColumnType("DATETIMEOFFSET", customFormat, "DATE");
            var column = new DataColumn("TestColumn", typeof(DateTime));

            // Act
            var result = columnType.Build(column);

            // Assert
            Assert.Equal(customFormat, result);
        }

        [Fact]
        public void CanWork_WithNullableDateTime_ShouldReturnFalse()
        {
            // Arrange
            var columnType = new DateColumnType("DATETIMEOFFSET", "DATETIME", "DATE");

            // Act
            var result = columnType.CanWork(typeof(DateTime?));

            // Assert
            // CanWork checks the exact type, not nullable types
            Assert.False(result);
        }

        [Fact]
        public void Build_WithEmptyColumnName_ShouldStillWork()
        {
            // Arrange
            var columnType = new DateColumnType("DATETIMEOFFSET", "DATETIME", "DATE");
            var column = new DataColumn("", typeof(DateTime));

            // Act
            var result = columnType.Build(column);

            // Assert
            Assert.Equal("DATETIME", result);
        }
    }
}
