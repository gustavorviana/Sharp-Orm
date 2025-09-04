using BaseTest.Utils;
using SharpOrm;
using SharpOrm.DataTranslation;
using SharpOrm.DataTranslation.Mappers;
using System.Dynamic;

namespace QueryTest.DataTranslation.Mappers
{
    public class DynamicRecordMapperTests : DbMockTest
    {
        [Fact]
        public void MapDynamicObject_WithSimpleTypes_ShouldReadCorrectly()
        {
            // Arrange
            const int id = 1;
            const string name = "Test Name";
            const double price = 99.99;
            const bool active = true;

            var reader = GetReader(
                new Cell("Id", id),
                new Cell("Name", name),
                new Cell("Price", price),
                new Cell("Active", active)
            );

            var mapper = new DynamicRecordMapper(reader, Translation);

            // Act
            mapper.MoveNext();

            // Assert
            Assert.NotNull(mapper.Current);
            Assert.IsType<ExpandoObject>(mapper.Current);
            Assert.Equal(id, mapper.Current.Id);
            Assert.Equal(name, mapper.Current.Name);
            Assert.Equal(price, mapper.Current.Price);
            Assert.Equal(active, mapper.Current.Active);
        }

        [Fact]
        public void MapDynamicObject_WithDbNullValues_ShouldHandleCorrectly()
        {
            // Arrange
            const int id = 1;
            const string name = "Test Name";

            var reader = GetReader(
                new Cell("Id", id),
                new Cell("Name", name),
                new Cell("Description", DBNull.Value),
                new Cell("OptionalField", null)
            );

            var mapper = new DynamicRecordMapper(reader, Translation);

            // Act
            mapper.MoveNext();

            // Assert
            Assert.NotNull(mapper.Current);
            Assert.IsType<ExpandoObject>(mapper.Current);
            Assert.Equal(id, mapper.Current.Id);
            Assert.Equal(name, mapper.Current.Name);
            Assert.Null(mapper.Current.Description);
            Assert.Null(mapper.Current.OptionalField);
        }

        [Fact]
        public void MapDynamicObject_WithVariousDataTypes_ShouldMapCorrectly()
        {
            // Arrange
            var testDate = DateTime.Today;
            var testGuid = Guid.NewGuid();
            decimal testDecimal = 123.45m;

            var reader = GetReader(
                new Cell("StringField", "Test String"),
                new Cell("IntField", 42),
                new Cell("DateField", testDate),
                new Cell("GuidField", testGuid),
                new Cell("DecimalField", testDecimal),
                new Cell("ByteField", (byte)255)
            );

            var mapper = new DynamicRecordMapper(reader, Translation);

            // Act
            mapper.MoveNext();

            // Assert
            Assert.NotNull(mapper.Current);
            Assert.IsType<ExpandoObject>(mapper.Current);
            Assert.Equal("Test String", mapper.Current.StringField);
            Assert.Equal(42, mapper.Current.IntField);
            Assert.Equal(testDate, mapper.Current.DateField);
            Assert.Equal(testGuid, mapper.Current.GuidField);
            Assert.Equal(testDecimal, mapper.Current.DecimalField);
            Assert.Equal((byte)255, mapper.Current.ByteField);
        }

        [Fact]
        public void MapDynamicObject_WithEmptyReader_ShouldReturnEmptyExpandoObject()
        {
            // Arrange
            var reader = GetReader(); // Empty reader

            var mapper = new DynamicRecordMapper(reader, Translation);

            // Act
            mapper.MoveNext();

            // Assert
            Assert.Null(mapper.Current);
        }

        [Fact]
        public void MoveNext_WithValidData_ShouldReturnTrueAndSetCurrent()
        {
            // Arrange
            var reader = GetReader(
                new Cell("Id", 1),
                new Cell("Name", "Test")
            );

            var mapper = new DynamicRecordMapper(reader, Translation);

            // Act
            var result = mapper.MoveNext();

            // Assert
            Assert.True(result);
            Assert.NotNull(mapper.Current);
            Assert.IsType<ExpandoObject>(mapper.Current);
            Assert.Equal(1, mapper.Current.Id);
            Assert.Equal("Test", mapper.Current.Name);
        }

        [Fact]
        public void MoveNext_WithNoData_ShouldReturnFalse()
        {
            // Arrange
            var reader = GetReader(); // Empty reader
            var mapper = new DynamicRecordMapper(reader, Translation);

            // Act
            var result = mapper.MoveNext();

            // Assert
            Assert.False(result);
            Assert.Null(mapper.Current);
        }

        [Fact]
        public void Enumeration_WithMultipleRecords_ShouldWorkCorrectly()
        {
            // Arrange
            var reader = GetReader(
                (i) => new Cell[]
                {
                    new Cell("Id", i + 1),
                    new Cell("Name", $"Name {i + 1}"),
                    new Cell("Value", (i + 1) * 10.0)
                },
                3 // 3 records
            );

            var mapper = new DynamicRecordMapper(reader, Translation);

            // Act
            var results = new List<dynamic>();
            foreach (dynamic item in mapper)
            {
                results.Add(item);
            }

            // Assert
            Assert.Equal(3, results.Count);

            Assert.Equal(1, results[0].Id);
            Assert.Equal("Name 1", results[0].Name);
            Assert.Equal(10.0, results[0].Value);

            Assert.Equal(2, results[1].Id);
            Assert.Equal("Name 2", results[1].Name);
            Assert.Equal(20.0, results[1].Value);

            Assert.Equal(3, results[2].Id);
            Assert.Equal("Name 3", results[2].Name);
            Assert.Equal(30.0, results[2].Value);
        }

        [Fact]
        public void Constructor_WithNullReader_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DynamicRecordMapper(null, Translation));
        }

        [Fact]
        public void Constructor_WithNullRegistry_ShouldThrowArgumentNullException()
        {
            // Arrange
            var reader = GetReader(new Cell("Id", 1));

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DynamicRecordMapper(reader, null));
        }

        [Fact]
        public void MapDynamicObject_WithCustomTranslation_ShouldApplyTranslation()
        {
            // Arrange
            var customRegistry = new TranslationRegistry();
            customRegistry.Translators = [new TestStringTranslation()];

            var reader = GetReader(
                new Cell("Id", 1),
                new Cell("TestString", "original_value")
            );

            var mapper = new DynamicRecordMapper(reader, customRegistry);

            // Act
            mapper.MoveNext();

            // Assert
            Assert.NotNull(mapper.Current);
            Assert.Equal(1, mapper.Current.Id);
            Assert.Equal("TRANSLATED_original_value", mapper.Current.TestString);
        }

        [Fact]
        public void MapDynamicObject_WithSpecialCharactersInColumnNames_ShouldMapCorrectly()
        {
            // Arrange
            var reader = GetReader(
                new Cell("Column With Spaces", "value1"),
                new Cell("Column_With_Underscores", "value2"),
                new Cell("Column.With.Dots", "value3")
            );

            var mapper = new DynamicRecordMapper(reader, Translation);

            // Act
            mapper.MoveNext();

            // Assert
            Assert.NotNull(mapper.Current);
            var dict = (IDictionary<string, object>)mapper.Current;
            Assert.Equal("value1", dict["Column With Spaces"]);
            Assert.Equal("value2", dict["Column_With_Underscores"]);
            Assert.Equal("value3", dict["Column.With.Dots"]);
        }

        [Fact]
        public void Reset_ShouldThrowNotSupportedException()
        {
            // Arrange
            var reader = GetReader(new Cell("Id", 1));
            var mapper = new DynamicRecordMapper(reader, Translation);

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => ((System.Collections.IEnumerator)mapper).Reset());
        }

        #region Helper Classes
        /// <summary>
        /// Test translation that prefixes strings with "TRANSLATED_"
        /// </summary>
        private class TestStringTranslation : ISqlTranslation
        {
            public bool CanWork(Type type) => type == typeof(string);

            public object FromSqlValue(object value, Type expectedType)
            {
                if (value == null || value == DBNull.Value)
                    return null;

                return $"TRANSLATED_{value}";
            }

            public object ToSqlValue(object value, Type type)
            {
                return value?.ToString()?.Replace("TRANSLATED_", "");
            }
        }
        #endregion
    }
}