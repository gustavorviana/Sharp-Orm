using BaseTest.Utils;
using NSubstitute;
using SharpOrm;
using SharpOrm.Builder;
using SharpOrm.DataTranslation;
using System.Globalization;

namespace QueryTest.DataTranslation
{
    public class TranslationRegistryTests : DbMockTest
    {
        #region Initialization and Default Registry Tests

        [Fact]
        public void DefaultRegistry_ShouldHaveDefaultSettings()
        {
            // Act
            var registry = new TranslationRegistry();

            // Assert
            Assert.NotNull(registry);
            Assert.False(registry.EmptyStringToNull);
            Assert.Equal(TimeZoneInfo.Local, registry.DbTimeZone);
            Assert.Equal("D", registry.GuidFormat);
            Assert.Equal(EnumSerialization.Value, registry.EnumSerialization);
            Assert.Null(registry.Culture);
        }

        [Fact]
        public void SetDefaultRegistry_WithNull_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => TranslationRegistry.Default = null);
        }

        [Fact]
        public void SetDefaultRegistry_WithValidRegistry_ShouldUpdateDefault()
        {
            // Arrange
            var originalDefault = TranslationRegistry.Default;
            var newRegistry = new TranslationRegistry { EmptyStringToNull = true };

            try
            {
                // Act
                TranslationRegistry.Default = newRegistry;

                // Assert
                Assert.Same(newRegistry, TranslationRegistry.Default);
                Assert.True(TranslationRegistry.Default.EmptyStringToNull);
            }
            finally
            {
                // Cleanup
                TranslationRegistry.Default = originalDefault;
            }
        }

        #endregion

        #region Property Configuration Tests

        [Fact]
        public void EmptyStringToNull_WhenSet_ShouldUpdateProperty()
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            registry.EmptyStringToNull = true;

            // Assert
            Assert.True(registry.EmptyStringToNull);
        }

        [Theory]
        [InlineData("N")]
        [InlineData("D")]
        [InlineData("B")]
        [InlineData("P")]
        [InlineData("X")]
        public void GuidFormat_WhenSet_ShouldUpdateProperty(string format)
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            registry.GuidFormat = format;

            // Assert
            Assert.Equal(format, registry.GuidFormat);
        }

        [Fact]
        public void DateFormat_WhenSet_ShouldUpdateProperty()
        {
            // Arrange
            var registry = new TranslationRegistry();
            const string customFormat = "dd/MM/yyyy HH:mm:ss";

            // Act
            registry.DateFormat = customFormat;

            // Assert
            Assert.Equal(customFormat, registry.DateFormat);
        }

        [Fact]
        public void Culture_WhenSet_ShouldUpdateProperty()
        {
            // Arrange
            var registry = new TranslationRegistry();
            var culture = new CultureInfo("en-US");

            // Act
            registry.Culture = culture;

            // Assert
            Assert.Equal(culture, registry.Culture);
        }

        [Theory]
        [InlineData(EnumSerialization.Value)]
        [InlineData(EnumSerialization.Name)]
        public void EnumSerialization_WhenSet_ShouldUpdateProperty(EnumSerialization serialization)
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            registry.EnumSerialization = serialization;

            // Assert
            Assert.Equal(serialization, registry.EnumSerialization);
        }

        [Fact]
        public void TimeZones_WhenSet_ShouldUpdateProperties()
        {
            // Arrange
            var registry = new TranslationRegistry();
            var utcTimeZone = TimeZoneInfo.Utc;

            // Act
            registry.DbTimeZone = utcTimeZone;
            registry.TimeZone = utcTimeZone;

            // Assert
            Assert.Equal(utcTimeZone, registry.DbTimeZone);
            Assert.Equal(utcTimeZone, registry.TimeZone);
        }

        #endregion

        #region Translators Management Tests

        [Fact]
        public void Translators_WhenSet_ShouldClearCache()
        {
            // Arrange
            var registry = new TranslationRegistry();
            var mockTranslator = Substitute.For<ISqlTranslation>();
            mockTranslator.CanWork(typeof(string)).Returns(true);

            // Act - First call to cache a translation
            registry.GetFor(typeof(string));

            // Act - Set new translators (should clear cache)
            registry.Translators = new[] { mockTranslator };
            var result = registry.GetFor(typeof(string));

            // Assert
            Assert.Same(mockTranslator, result);
        }

        [Fact]
        public void Translators_CustomTranslator_ShouldBeUsedBeforeNative()
        {
            // Arrange
            var registry = new TranslationRegistry();
            var mockTranslator = Substitute.For<ISqlTranslation>();
            mockTranslator.CanWork(typeof(int)).Returns(true);
            registry.Translators = new[] { mockTranslator };

            // Act
            var result = registry.GetFor(typeof(int));

            // Assert
            Assert.Same(mockTranslator, result);
        }

        #endregion

        #region ToSql Conversion Tests

        [Fact]
        public void ToSql_NullValue_ShouldReturnDBNull()
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            var result = registry.ToSql(null);

            // Assert
            Assert.Equal(DBNull.Value, result);
        }

        [Fact]
        public void ToSql_DBNullValue_ShouldReturnDBNull()
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            var result = registry.ToSql(DBNull.Value);

            // Assert
            Assert.Equal(DBNull.Value, result);
        }

        [Fact]
        public void ToSql_EmptyString_WithEmptyStringToNull_ShouldReturnDBNull()
        {
            // Arrange
            var registry = new TranslationRegistry { EmptyStringToNull = true };

            // Act
            var result = registry.ToSql("");

            // Assert
            Assert.Equal(DBNull.Value, result);
        }

        [Fact]
        public void ToSql_EmptyString_WithoutEmptyStringToNull_ShouldReturnEmptyString()
        {
            // Arrange
            var registry = new TranslationRegistry { EmptyStringToNull = false };

            // Act
            var result = registry.ToSql("");

            // Assert
            Assert.Equal("", result);
        }

        [Fact]
        public void ToSql_UnsupportedType_ShouldThrowNotSupportedException()
        {
            // Arrange
            var registry = new TranslationRegistry();
            var unsupportedObject = new object();

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => registry.ToSql(unsupportedObject));
        }

        [Theory]
        [InlineData(123)]
        [InlineData("Test")]
        [InlineData(true)]
        [InlineData(123.45)]
        public void ToSql_SupportedTypes_ShouldConvertCorrectly(object value)
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            var result = registry.ToSql(value);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(DBNull.Value, result);
        }

        #endregion

        #region FromSql Conversion Tests

        [Fact]
        public void FromSql_WithoutType_NullValue_ShouldReturnNull()
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            var result = registry.FromSql(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FromSql_WithoutType_DBNullValue_ShouldReturnNull()
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            var result = registry.FromSql(DBNull.Value);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FromSql_WithoutType_ValidValue_ShouldReturnConvertedValue()
        {
            // Arrange
            var registry = new TranslationRegistry();
            const string testValue = "Test";

            // Act
            var result = registry.FromSql(testValue);

            // Assert
            Assert.Equal(testValue, result);
        }

        [Fact]
        public void FromSql_WithType_NullValue_ShouldReturnNull()
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            var result = registry.FromSql(null, typeof(string));

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FromSql_WithType_DBNullValue_ShouldReturnNull()
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            var result = registry.FromSql(DBNull.Value, typeof(int));

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData(123, typeof(int), 123)]
        [InlineData("123", typeof(int), 123)]
        [InlineData(true, typeof(bool), true)]
        [InlineData(1, typeof(bool), true)]
        [InlineData(0, typeof(bool), false)]
        public void FromSql_WithType_ValidConversions_ShouldConvertCorrectly(object input, Type targetType, object expected)
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            var result = registry.FromSql(input, targetType);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void FromSql_Generic_ShouldReturnTypedResult()
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            var result = registry.FromSql<int>("123");

            // Assert
            Assert.Equal(123, result);
            Assert.IsType<int>(result);
        }

        #endregion

        #region Type Validation Tests

        [Theory]
        [InlineData(typeof(int), typeof(int))]
        [InlineData(typeof(int?), typeof(int))]
        [InlineData(typeof(string), typeof(string))]
        [InlineData(typeof(DateTime?), typeof(DateTime))]
        public void GetValidTypeFor_ShouldHandleNullableTypes(Type input, Type expected)
        {
            // Act
            var result = TranslationRegistry.GetValidTypeFor(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetValidTypeFor_NullType_ShouldReturnNull()
        {
            // Act
            var result = TranslationRegistry.GetValidTypeFor(null);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region Translation Retrieval Tests

        [Fact]
        public void GetFor_PrimitiveType_ShouldReturnNativeTranslation()
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            var result = registry.GetFor(typeof(int));

            // Assert
            Assert.NotNull(result);
            Assert.IsType<NativeSqlTranslation>(result);
        }

        [Fact]
        public void GetFor_UnsupportedType_ShouldReturnNull()
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            var result = registry.GetFor(typeof(object));

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetFor_TypeWithSqlConverterAttribute_ShouldReturnCustomTranslation()
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            var result = registry.GetFor(typeof(TestTypeWithConverter));

            // Assert
            Assert.NotNull(result);
            Assert.IsType<TestSqlTranslation>(result);
        }

        [Fact]
        public void GetFor_SameTypeTwice_ShouldReturnCachedInstance()
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            var result1 = registry.GetFor(typeof(int));
            var result2 = registry.GetFor(typeof(int));

            // Assert
            Assert.Same(result1, result2);
        }

        #endregion

        #region Member Translation Tests

        [Fact]
        public void GetOf_MemberWithSqlConverterAttribute_ShouldReturnCustomTranslation()
        {
            // Arrange
            var registry = new TranslationRegistry();
            var memberInfo = typeof(TestClassWithConverterMember).GetProperty(nameof(TestClassWithConverterMember.CustomProperty));

            // Act
            var result = registry.GetOf(memberInfo);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<TestSqlTranslation>(result);
        }

        [Fact]
        public void GetOf_MemberWithoutSqlConverterAttribute_ShouldReturnNull()
        {
            // Arrange
            var registry = new TranslationRegistry();
            var memberInfo = typeof(TestClassWithConverterMember).GetProperty(nameof(TestClassWithConverterMember.NormalProperty));

            // Act
            var result = registry.GetOf(memberInfo);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region DateTime and TimeZone Tests

        [Fact]
        public void IsDateOrTime_DateTimeType_ShouldReturnTrue()
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            var result = registry.IsDateOrTime(typeof(DateTime));

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsDateOrTime_NonDateTimeType_ShouldReturnFalse()
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            var result = registry.IsDateOrTime(typeof(int));

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Table Management Tests

        [Fact]
        public void GetTableName_ValidType_ShouldReturnTableName()
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            var result = registry.GetTableName(typeof(TestClass));

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void GetTable_ValidType_ShouldReturnTableInfo()
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            var result = registry.GetTable(typeof(TestClass));

            // Assert
            Assert.NotNull(result);
            Assert.IsType<TableInfo>(result);
        }

        [Fact]
        public void GetTable_RowType_ShouldReturnNull()
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            var result = registry.GetTable(typeof(Row));

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetTable_SameTypeTwice_ShouldReturnCachedInstance()
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act
            var result1 = registry.GetTable(typeof(TestClass));
            var result2 = registry.GetTable(typeof(TestClass));

            // Assert
            Assert.Same(result1, result2);
        }

        #endregion

        #region Equality Tests

        [Fact]
        public void Equals_SameInstance_ShouldReturnTrue()
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act & Assert
            Assert.True(registry.Equals(registry));
        }

        [Fact]
        public void Equals_DifferentValues_ShouldReturnFalse()
        {
            // Arrange
            var registry1 = new TranslationRegistry { GuidFormat = "D" };
            var registry2 = new TranslationRegistry { GuidFormat = "N" };

            // Act & Assert
            Assert.False(registry1.Equals(registry2));
        }

        [Fact]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            // Arrange
            var registry = new TranslationRegistry();

            // Act & Assert
            Assert.False(registry.Equals(null));
        }

        [Fact]
        public void GetHashCode_SameValues_ShouldReturnSameHash()
        {
            // Arrange
            var registry1 = new TranslationRegistry { GuidFormat = "D" };
            var registry2 = new TranslationRegistry { GuidFormat = "D" };

            // Act
            var hash1 = registry1.GetHashCode();
            var hash2 = registry2.GetHashCode();

            // Assert
            Assert.Equal(hash1, hash2);
        }

        #endregion

        #region Clone Tests

        [Fact]
        public void Clone_ShouldCreateIndependentCopy()
        {
            // Arrange
            var original = new TranslationRegistry
            {
                GuidFormat = "N",
                EmptyStringToNull = true,
                DbTimeZone = TimeZoneInfo.Utc,
                EnumSerialization = EnumSerialization.Name
            };

            // Act
            var cloned = original.Clone();

            // Assert
            Assert.NotSame(original, cloned);
            Assert.Equal(original.GuidFormat, cloned.GuidFormat);
            Assert.Equal(original.EmptyStringToNull, cloned.EmptyStringToNull);
            Assert.Equal(original.DbTimeZone, cloned.DbTimeZone);
            Assert.Equal(original.EnumSerialization, cloned.EnumSerialization);
        }

        [Fact]
        public void Clone_Modifications_ShouldNotAffectOriginal()
        {
            // Arrange
            var original = new TranslationRegistry { GuidFormat = "D" };
            var cloned = original.Clone();

            // Act
            cloned.GuidFormat = "N";

            // Assert
            Assert.Equal("D", original.GuidFormat);
            Assert.Equal("N", cloned.GuidFormat);
        }

        [Fact]
        public void ICloneable_Clone_ShouldReturnSameAsTypedClone()
        {
            // Arrange
            var registry = new TranslationRegistry();
            var cloneable = (ICloneable)registry;

            // Act
            var clonedObject = cloneable.Clone();
            var clonedTyped = registry.Clone();

            // Assert
            Assert.IsType<TranslationRegistry>(clonedObject);
            Assert.Equal(clonedTyped.GetHashCode(), clonedObject.GetHashCode());
        }

        #endregion

        #region Test Helper Classes

        [SqlConverter(typeof(TestSqlTranslation))]
        private class TestTypeWithConverter
        {
            public int Value { get; set; }
        }

        private class TestClassWithConverterMember
        {
            [SqlConverter(typeof(TestSqlTranslation))]
            public string CustomProperty { get; set; }

            public string NormalProperty { get; set; }
        }

        private class TestSqlTranslation : ISqlTranslation
        {
            public bool CanWork(Type type) => true;
            public object FromSqlValue(object value, Type expectedType) => value;
            public object ToSqlValue(object value, Type expectedType) => value;
        }

        private class TestClass
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        #endregion
    }
}