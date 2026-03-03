using BaseTest.Utils;
using SharpOrm.DataTranslation;
using System;
using Xunit;

namespace QueryTest.DataTranslation
{
    public class NativeSqlTranslationTests : DbMockTest
    {
        [Fact]
        public void FromSqlValue_WithNullEnumString_ShouldReturnNull()
        {
            // Arrange
            var translator = new NativeSqlTranslation();

            // Act
            var result = translator.FromSqlValue(null, typeof(TestEnum));

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FromSqlValue_WithEmptyEnumString_ShouldReturnNull()
        {
            // Arrange
            var translator = new NativeSqlTranslation();

            // Act
            var result = translator.FromSqlValue("", typeof(TestEnum));

            // Assert
            Assert.Equal(default(TestEnum), result);
        }

        [Fact]
        public void FromSqlValue_WithInvalidEnumString_WhenReturnDefault_ShouldReturnDefaultEnum()
        {
            // Arrange
            var translator = new NativeSqlTranslation();
            translator.InvalidValueBehavior = InvalidValueBehavior.ReturnDefault;

            // Act
            var result = translator.FromSqlValue("InvalidValue", typeof(TestEnum));

            // Assert
            Assert.Equal(default(TestEnum), result);
        }

        [Fact]
        public void FromSqlValue_WithInvalidEnumString_WhenThrowException_ShouldThrowArgumentException()
        {
            // Arrange
            var translator = new NativeSqlTranslation();
            translator.InvalidValueBehavior = InvalidValueBehavior.ThrowException;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => translator.FromSqlValue("InvalidValue", typeof(TestEnum)));
        }


        [Fact]
        public void FromSqlValue_WithInvalidEnumNumeric_WhenReturnDefault_ShouldReturnDefaultEnum()
        {
            // Arrange
            var translator = new NativeSqlTranslation();
            translator.InvalidValueBehavior = InvalidValueBehavior.ReturnDefault;

            // Act
            var result = translator.FromSqlValue(999, typeof(TestEnum));

            // Assert
            Assert.Equal(default(TestEnum), result);
        }

        [Fact]
        public void FromSqlValue_WithInvalidEnumNumeric_WhenThrowException_ShouldThrowArgumentException()
        {
            // Arrange
            var translator = new NativeSqlTranslation();
            translator.InvalidValueBehavior = InvalidValueBehavior.ThrowException;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => translator.FromSqlValue(999, typeof(TestEnum)));
        }

        [Fact]
        public void FromSqlValue_WithDBNullEnum_ShouldReturnNull()
        {
            // Arrange
            var translator = new NativeSqlTranslation();

            // Act
            var result = translator.FromSqlValue(DBNull.Value, typeof(TestEnum));

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FromSqlValue_WithInvalidGuidString_WhenThrowException_ShouldThrowArgumentException()
        {
            // Arrange
            var translator = new NativeSqlTranslation();
            translator.InvalidValueBehavior = InvalidValueBehavior.ThrowException;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => translator.FromSqlValue("invalid-guid", typeof(Guid)));
            Assert.Contains("Cannot convert invalid value", ex.Message);
        }

        [Fact]
        public void FromSqlValue_WithInvalidGuidString_WhenReturnDefault_ShouldReturnDefaultGuid()
        {
            // Arrange
            var translator = new NativeSqlTranslation();
            translator.InvalidValueBehavior = InvalidValueBehavior.ReturnDefault;

            // Act
            var result = translator.FromSqlValue("invalid-guid", typeof(Guid));

            // Assert
            Assert.Equal(default(Guid), result);
        }

        [Fact]
        public void FromSqlValue_WithNullGuid_ShouldReturnNull()
        {
            // Arrange
            var translator = new NativeSqlTranslation();

            // Act
            var result = translator.FromSqlValue(null, typeof(Guid));

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FromSqlValue_WithEmptyGuidString_ShouldReturnNull()
        {
            // Arrange
            var translator = new NativeSqlTranslation();

            // Act
            var result = translator.FromSqlValue("", typeof(Guid));

            // Assert
            Assert.Equal(default(Guid), result);
        }

        [Fact]
        public void FromSqlValue_WithValidGuidString_ShouldParseGuid()
        {
            // Arrange
            var translator = new NativeSqlTranslation();
            var guidString = "12345678-1234-1234-1234-123456789012";

            // Act
            var result = translator.FromSqlValue(guidString, typeof(Guid));

            // Assert
            Assert.IsType<Guid>(result);
            Assert.Equal(Guid.Parse(guidString), result);
        }

        [Fact]
        public void FromSqlValue_WithDBNullGuid_ShouldReturnNull()
        {
            // Arrange
            var translator = new NativeSqlTranslation();

            // Act
            var result = translator.FromSqlValue(DBNull.Value, typeof(Guid));

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FromSqlValue_WithValidEnumString_ShouldParseEnum()
        {
            // Arrange
            var translator = new NativeSqlTranslation();

            // Act
            var result = translator.FromSqlValue("Value1", typeof(TestEnum));

            // Assert
            Assert.Equal(TestEnum.Value1, result);
        }

        private enum TestEnum
        {
            Value1,
            Value2
        }
    }
}

