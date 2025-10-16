using SharpOrm;
using SharpOrm.Errors;
using System.Data;

namespace QueryTest
{
    public class RowDataReaderTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidArrays_ShouldCreateCollection()
        {
            // Arrange
            var columnNames = new[] { "Id", "Name", "Age" };
            var values = new[]
            {
                new object[] { 1, "Alice", 30 },
                new object[] { 2, "Bob", 25 }
            };

            // Act
            var collection = new RowDataReader(columnNames, values);

            // Assert
            Assert.Equal(3, collection.FieldCount);
            Assert.Equal(2, collection.ReadToEnd().Length);
            Assert.Equal(columnNames, collection.ColumnNames);
        }

        [Fact]
        public void Constructor_WithNullColumnNames_ShouldThrowArgumentNullException()
        {
            // Arrange
            var values = new[] { new object[] { 1, "Alice" } };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RowDataReader(null, values));
        }

        [Fact]
        public void Constructor_WithNullValues_ShouldThrowArgumentNullException()
        {
            // Arrange
            var columnNames = new[] { "Id", "Name" };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RowDataReader(columnNames, null));
        }

        [Fact]
        public void Constructor_WithMismatchedValueCount_ShouldThrowArgumentException()
        {
            // Arrange
            var columnNames = new[] { "Id", "Name" };
            var values = new[]
            {
                new object[] { 1, "Alice", 30 } // 3 values but only 2 columns
            };

            // Act & Assert
            var ex = Assert.Throws<DataReaderException>(() => new RowDataReader(columnNames, values).ReadToEnd());
            Assert.Contains("expected 2", ex.Message);
        }

        [Fact]
        public void Constructor_WithNullRow_ShouldThrowArgumentException()
        {
            // Arrange
            var columnNames = new[] { "Id", "Name" };
            var values = new object[][]
            {
                new object[] { 1, "Alice" },
                null // null row
            };

            // Act & Assert
            var ex = Assert.Throws<DataReaderException>(() => new RowDataReader(columnNames, values).ReadToEnd());
            Assert.Contains("Row at index 1 is null", ex.Message);
        }

        [Fact]
        public void Constructor_FromRows_WithValidRows_ShouldCreateCollection()
        {
            // Arrange
            var rows = new[]
            {
                new Row(new Cell("Id", 1), new Cell("Name", "Alice"), new Cell("Age", 30)),
                new Row(new Cell("Id", 2), new Cell("Name", "Bob"), new Cell("Age", 25))
            };

            // Act
            var collection = new RowDataReader(rows);

            // Assert
            Assert.Equal(3, collection.FieldCount);
            Assert.Equal(2, collection.ReadToEnd().Length);
            Assert.Equal(new[] { "Id", "Name", "Age" }, collection.ColumnNames);
        }

        [Fact]
        public void Constructor_FromRows_WithNullRows_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RowDataReader((IEnumerable<Row>)null));
        }

        [Fact]
        public void Constructor_FromRows_WithInconsistentColumnCount_ShouldThrowArgumentException()
        {
            // Arrange
            var rows = new[]
            {
                new Row(new Cell("Id", 1), new Cell("Name", "Alice")),
                new Row(new Cell("Id", 2), new Cell("Name", "Bob"), new Cell("Age", 25)) // Different column count
            };

            // Act & Assert
            var ex = Assert.Throws<DataReaderException>(() => new RowDataReader(rows).ReadToEnd());
            Assert.Contains("expected 2", ex.Message);
        }

        [Fact]
        public void Constructor_FromRows_WithInconsistentColumnNames_ShouldThrowArgumentException()
        {
            // Arrange
            var rows = new[]
            {
                new Row(new Cell("Id", 1), new Cell("Name", "Alice")),
                new Row(new Cell("Id", 2), new Cell("Email", "bob@example.com")) // Different column name
            };

            // Act & Assert
            var ex = Assert.Throws<DataReaderException>(() => new RowDataReader(rows).ReadToEnd());
            Assert.Contains("expected 'Name'", ex.Message);
        }

        [Fact]
        public void Empty_ShouldCreateEmptyCollection()
        {
            // Arrange & Act
            var collection = RowDataReader.Empty("Id", "Name", "Age");

            // Assert
            Assert.Equal(3, collection.FieldCount);
            Assert.Equal(0, collection.ReadToEnd().Length);
            Assert.False(collection.Read());
        }

        [Fact]
        public void Constructor_FromIDataReader_WithValidReader_ShouldCreateCollection()
        {
            // Arrange
            var sourceCollection = CreateTestCollection();

            // Act
            var newCollection = new RowDataReader((IDataReader)sourceCollection);

            // Assert
            Assert.Equal(3, newCollection.FieldCount);
            Assert.Equal(new[] { "Id", "Name", "Age" }, newCollection.ColumnNames);

            // Verify data
            newCollection.Read();
            Assert.Equal(1, newCollection.GetInt32(0));
            Assert.Equal("Alice", newCollection.GetString(1));
            Assert.Equal(30, newCollection.GetInt32(2));

            newCollection.Read();
            Assert.Equal(2, newCollection.GetInt32(0));
            Assert.Equal("Bob", newCollection.GetString(1));
            Assert.Equal(25, newCollection.GetInt32(2));
            Assert.False(newCollection.Read());
        }

        [Fact]
        public void Constructor_FromIDataReader_WithNullReader_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RowDataReader((IDataReader)null));
        }

        [Fact]
        public void Constructor_FromIDataReader_WithEmptyReader_ShouldCreateEmptyCollection()
        {
            // Arrange
            IDataReader emptyReader = RowDataReader.Empty("Id", "Name");

            // Act
            var newCollection = new RowDataReader(emptyReader);

            // Assert
            Assert.Equal(2, newCollection.FieldCount);
            Assert.Equal(0, newCollection.ReadToEnd().Length);
            Assert.Equal(new[] { "Id", "Name" }, newCollection.ColumnNames);
        }

        [Fact]
        public void Constructor_FromIDataReader_ShouldNotDisposeSourceReader()
        {
            // Arrange
            IDataReader sourceCollection = CreateTestCollection();

            // Act
            var newCollection = new RowDataReader(sourceCollection);

            // Assert - Original reader should still be usable
            Assert.False(sourceCollection.IsClosed);

            // Should be able to read from original again (will be at end, but not closed)
            sourceCollection.Close();
            Assert.True(sourceCollection.IsClosed);
        }

        [Fact]
        public void Constructor_FromIDataReader_WithNullValues_ShouldHandleCorrectly()
        {
            // Arrange
            var columnNames = new[] { "Id", "Name", "Value" };
            var values = new[]
            {
                new object[] { 1, "Alice", null },
                new object[] { 2, null, DBNull.Value }
            };
            IDataReader sourceCollection = new RowDataReader(columnNames, values);

            // Act
            var newCollection = new RowDataReader(sourceCollection);

            // Assert
            newCollection.Read();
            Assert.False(newCollection.IsDBNull(0));
            Assert.False(newCollection.IsDBNull(1));
            Assert.True(newCollection.IsDBNull(2));

            newCollection.Read();
            Assert.False(newCollection.IsDBNull(0));
            Assert.True(newCollection.IsDBNull(1));
            Assert.True(newCollection.IsDBNull(2));
        }

        #endregion

        #region IDataReader Tests

        [Fact]
        public void Read_ShouldAdvanceToNextRow()
        {
            // Arrange
            var collection = CreateTestCollection();

            // Act & Assert
            Assert.True(collection.Read()); // First row
            Assert.Equal(1, collection.GetInt32(0));

            Assert.True(collection.Read()); // Second row
            Assert.Equal(2, collection.GetInt32(0));

            Assert.False(collection.Read()); // No more rows
        }

        [Fact]
        public void Read_OnClosedReader_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var collection = CreateTestCollection();
            collection.Close();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => collection.Read());
        }

        [Fact]
        public void GetValue_BeforeRead_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var collection = CreateTestCollection();

            // Act & Assert
            Assert.Throws<DataReaderException>(() => collection.GetValue(0));
        }

        [Fact]
        public void GetValue_WithValidIndex_ShouldReturnValue()
        {
            // Arrange
            var collection = CreateTestCollection();
            collection.Read();

            // Act
            var id = collection.GetValue(0);
            var name = collection.GetValue(1);

            // Assert
            Assert.Equal(1, id);
            Assert.Equal("Alice", name);
        }

        [Fact]
        public void GetValue_WithInvalidIndex_ShouldThrowIndexOutOfRangeException()
        {
            // Arrange
            var collection = CreateTestCollection();
            collection.Read();

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => collection.GetValue(99));
        }

        [Fact]
        public void GetValues_ShouldPopulateArray()
        {
            // Arrange
            var collection = CreateTestCollection();
            collection.Read();
            var values = new object[3];

            // Act
            var count = collection.GetValues(values);

            // Assert
            Assert.Equal(3, count);
            Assert.Equal(1, values[0]);
            Assert.Equal("Alice", values[1]);
            Assert.Equal(30, values[2]);
        }

        [Fact]
        public void GetValues_WithSmallerArray_ShouldCopyPartially()
        {
            // Arrange
            var collection = CreateTestCollection();
            collection.Read();
            var values = new object[2];

            // Act
            var count = collection.GetValues(values);

            // Assert
            Assert.Equal(2, count);
            Assert.Equal(1, values[0]);
            Assert.Equal("Alice", values[1]);
        }

        [Fact]
        public void Indexer_ByInt_ShouldReturnValue()
        {
            // Arrange
            var collection = CreateTestCollection();
            collection.Read();

            // Act & Assert
            Assert.Equal(1, collection[0]);
            Assert.Equal("Alice", collection[1]);
            Assert.Equal(30, collection[2]);
        }

        [Fact]
        public void Indexer_ByString_ShouldReturnValue()
        {
            // Arrange
            var collection = CreateTestCollection();
            collection.Read();

            // Act & Assert
            Assert.Equal(1, collection["Id"]);
            Assert.Equal("Alice", collection["Name"]);
            Assert.Equal(30, collection["Age"]);
        }

        [Fact]
        public void GetOrdinal_WithValidName_ShouldReturnIndex()
        {
            // Arrange
            var collection = CreateTestCollection();

            // Act & Assert
            Assert.Equal(0, collection.GetOrdinal("Id"));
            Assert.Equal(1, collection.GetOrdinal("Name"));
            Assert.Equal(2, collection.GetOrdinal("Age"));
        }

        [Fact]
        public void GetOrdinal_WithInvalidName_ShouldThrowIndexOutOfRangeException()
        {
            // Arrange
            var collection = CreateTestCollection();

            // Act & Assert
            var ex = Assert.Throws<DataReaderColumnException>(() => collection.GetOrdinal("InvalidColumn"));
            Assert.Contains("InvalidColumn", ex.Message);
        }

        [Fact]
        public void GetOrdinal_IsCaseInsensitive()
        {
            // Arrange
            var collection = CreateTestCollection();

            // Act & Assert
            Assert.Equal(0, collection.GetOrdinal("ID"));
            Assert.Equal(1, collection.GetOrdinal("name"));
            Assert.Equal(2, collection.GetOrdinal("AGE"));
        }

        [Fact]
        public void GetName_WithValidIndex_ShouldReturnColumnName()
        {
            // Arrange
            var collection = CreateTestCollection();

            // Act & Assert
            Assert.Equal("Id", collection.GetName(0));
            Assert.Equal("Name", collection.GetName(1));
            Assert.Equal("Age", collection.GetName(2));
        }

        [Fact]
        public void GetName_WithInvalidIndex_ShouldThrowIndexOutOfRangeException()
        {
            // Arrange
            var collection = CreateTestCollection();

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => collection.GetName(99));
        }

        [Fact]
        public void GetFieldType_ShouldReturnCorrectType()
        {
            // Arrange
            var collection = CreateTestCollection();

            // Act & Assert
            Assert.Equal(typeof(int), collection.GetFieldType(0));
            Assert.Equal(typeof(string), collection.GetFieldType(1));
            Assert.Equal(typeof(int), collection.GetFieldType(2));
        }

        [Fact]
        public void GetFieldType_WithAllNullValues_ShouldReturnObjectType()
        {
            // Arrange
            var columnNames = new[] { "NullColumn" };
            var values = new[]
            {
                new object[] { null },
                new object[] { DBNull.Value }
            };
            var collection = new RowDataReader(columnNames, values);

            // Act
            var type = collection.GetFieldType(0);

            // Assert
            Assert.Equal(typeof(object), type);
        }

        [Fact]
        public void GetDataTypeName_ShouldReturnTypeName()
        {
            // Arrange
            var collection = CreateTestCollection();

            // Act & Assert
            Assert.Equal("Int32", collection.GetDataTypeName(0));
            Assert.Equal("String", collection.GetDataTypeName(1));
        }

        [Fact]
        public void IsDBNull_WithNullValue_ShouldReturnTrue()
        {
            // Arrange
            var columnNames = new[] { "Id", "NullableValue" };
            var values = new[]
            {
                new object[] { 1, null },
                new object[] { 2, DBNull.Value }
            };
            var collection = new RowDataReader(columnNames, values);

            // Act & Assert
            collection.Read();
            Assert.False(collection.IsDBNull(0));
            Assert.True(collection.IsDBNull(1));

            collection.Read();
            Assert.False(collection.IsDBNull(0));
            Assert.True(collection.IsDBNull(1));
        }

        [Fact]
        public void IsClosed_InitialValue_ShouldBeFalse()
        {
            // Arrange
            var collection = CreateTestCollection();

            // Act & Assert
            Assert.False(collection.IsClosed);
        }

        [Fact]
        public void Close_ShouldSetIsClosedToTrue()
        {
            // Arrange
            var collection = CreateTestCollection();

            // Act
            collection.Close();

            // Assert
            Assert.True(collection.IsClosed);
        }

        [Fact]
        public void Depth_ShouldAlwaysReturnZero()
        {
            // Arrange
            var collection = CreateTestCollection();

            // Act & Assert
            Assert.Equal(0, collection.Depth);
        }

        [Fact]
        public void RecordsAffected_ShouldReturnMinusOne()
        {
            // Arrange
            var collection = CreateTestCollection();

            // Act & Assert
            Assert.Equal(-1, collection.RecordsAffected);
        }

        [Fact]
        public void NextResult_ShouldAlwaysReturnFalse()
        {
            // Arrange
            var collection = CreateTestCollection();

            // Act & Assert
            Assert.False(collection.NextResult());
        }

        [Fact]
        public void GetSchemaTable_ShouldReturnValidSchema()
        {
            // Arrange
            var collection = CreateTestCollection();

            // Act
            var schemaTable = collection.GetSchemaTable();

            // Assert
            Assert.NotNull(schemaTable);
            Assert.Equal(3, schemaTable.Rows.Count);
            Assert.Equal("Id", schemaTable.Rows[0]["ColumnName"]);
            Assert.Equal(0, schemaTable.Rows[0]["ColumnOrdinal"]);
            Assert.Equal(typeof(int), schemaTable.Rows[0]["DataType"]);
        }

        #endregion

        #region Typed Getter Tests

        [Fact]
        public void GetInt32_ShouldReturnIntValue()
        {
            // Arrange
            var collection = CreateTestCollection();
            collection.Read();

            // Act & Assert
            Assert.Equal(1, collection.GetInt32(0));
            Assert.Equal(30, collection.GetInt32(2));
        }

        [Fact]
        public void GetString_ShouldReturnStringValue()
        {
            // Arrange
            var collection = CreateTestCollection();
            collection.Read();

            // Act & Assert
            Assert.Equal("Alice", collection.GetString(1));
        }

        [Fact]
        public void GetBoolean_ShouldConvertValue()
        {
            // Arrange
            var columnNames = new[] { "IsActive" };
            var values = new[] { new object[] { true } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act & Assert
            Assert.True(collection.GetBoolean(0));
        }

        [Fact]
        public void GetDateTime_ShouldConvertValue()
        {
            // Arrange
            var date = new DateTime(2024, 1, 1);
            var columnNames = new[] { "CreatedAt" };
            var values = new[] { new object[] { date } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act & Assert
            Assert.Equal(date, collection.GetDateTime(0));
        }

        [Fact]
        public void GetDecimal_ShouldConvertValue()
        {
            // Arrange
            var columnNames = new[] { "Price" };
            var values = new[] { new object[] { 99.99m } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act & Assert
            Assert.Equal(99.99m, collection.GetDecimal(0));
        }

        [Fact]
        public void GetGuid_ShouldReturnGuidValue()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var columnNames = new[] { "Uid" };
            var values = new[] { new object[] { guid } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act & Assert
            Assert.Equal(guid, collection.GetGuid(0));
        }

        [Fact]
        public void GetBytes_ShouldReturnByteArray()
        {
            // Arrange
            var bytes = new byte[] { 1, 2, 3, 4, 5 };
            var columnNames = new[] { "Data" };
            var values = new[] { new object[] { bytes } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            var buffer = new byte[5];

            // Act
            var bytesRead = collection.GetBytes(0, 0, buffer, 0, 5);

            // Assert
            Assert.Equal(5, bytesRead);
            Assert.Equal(bytes, buffer);
        }

        [Fact]
        public void GetChars_ShouldReturnCharArray()
        {
            // Arrange
            var columnNames = new[] { "Name" };
            var values = new[] { new object[] { "Hello" } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            var buffer = new char[5];

            // Act
            var charsRead = collection.GetChars(0, 0, buffer, 0, 5);

            // Assert
            Assert.Equal(5, charsRead);
            Assert.Equal("Hello", new string(buffer));
        }

        #endregion

        #region Row Conversion Tests

        [Fact]
        public void ToRow_ShouldConvertCurrentRow()
        {
            // Arrange
            var collection = CreateTestCollection();
            collection.Read();

            // Act
            var row = collection.ToRow();

            // Assert
            Assert.Equal(3, row.Count);
            Assert.Equal("Id", row.ColumnNames[0]);
            Assert.Equal(1, row[0].Value);
            Assert.Equal("Alice", row[1].Value);
            Assert.Equal(30, row[2].Value);
        }

        [Fact]
        public void ToRow_BeforeRead_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var collection = CreateTestCollection();

            // Act & Assert
            Assert.Throws<DataReaderException>(() => collection.ToRow());
        }

        [Fact]
        public void ToRows_ShouldConvertAllRows()
        {
            // Arrange
            var collection = CreateTestCollection();

            // Act
            var rows = collection.ReadToEnd();

            // Assert
            Assert.Equal(2, rows.Length);
            Assert.Equal(1, rows[0][0].Value);
            Assert.Equal("Alice", rows[0][1].Value);
            Assert.Equal(2, rows[1][0].Value);
            Assert.Equal("Bob", rows[1][1].Value);
        }

        #endregion

        #region IEnumerable<Row> Tests

        [Fact]
        public void GetEnumerator_ShouldIterateAsRows()
        {
            // Arrange
            var collection = CreateTestCollection();

            // Act
            var rows = collection.ToList();

            // Assert
            Assert.Equal(2, rows.Count);
            Assert.Equal(1, rows[0][0].Value);
            Assert.Equal("Alice", rows[0][1].Value);
            Assert.Equal(2, rows[1][0].Value);
            Assert.Equal("Bob", rows[1][1].Value);
        }

        [Fact]
        public void GetEnumerator_WithLinq_ShouldWork()
        {
            // Arrange
            var collection = CreateTestCollection();

            // Act
            var aliceRow = collection.FirstOrDefault(r => r[1].Value.ToString() == "Alice");

            // Assert
            Assert.NotNull(aliceRow);
            Assert.Equal(1, aliceRow[0].Value);
            Assert.Equal("Alice", aliceRow[1].Value);
        }

        [Fact]
        public void GetEnumerator_NonGeneric_ShouldWork()
        {
            // Arrange
            var collection = CreateTestCollection();
            var enumerator = ((System.Collections.IEnumerable)collection).GetEnumerator();

            // Act
            var hasFirst = enumerator.MoveNext();
            var firstRow = enumerator.Current as Row;

            // Assert
            Assert.True(hasFirst);
            Assert.NotNull(firstRow);
            Assert.Equal(1, firstRow[0].Value);
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_ShouldCloseReader()
        {
            // Arrange
            var collection = CreateTestCollection();

            // Act
            collection.Dispose();

            // Assert
            Assert.True(collection.IsClosed);
        }

        [Fact]
        public void UsingStatement_ShouldDisposeCorrectly()
        {
            // Arrange
            RowDataReader collection;

            // Act
            using (collection = CreateTestCollection())
            {
                collection.Read();
            }

            // Assert
            Assert.True(collection.IsClosed);
        }

        #endregion

        #region Typed Getters By Name Tests

        [Fact]
        public void GetInt32_ByName_ShouldReturnIntValue()
        {
            // Arrange
            var collection = CreateTestCollection();
            collection.Read();

            // Act & Assert
            Assert.Equal(1, collection.GetInt32("Id"));
            Assert.Equal(30, collection.GetInt32("Age"));
        }

        [Fact]
        public void GetString_ByName_ShouldReturnStringValue()
        {
            // Arrange
            var collection = CreateTestCollection();
            collection.Read();

            // Act & Assert
            Assert.Equal("Alice", collection.GetString("Name"));
        }

        [Fact]
        public void GetBoolean_ByName_ShouldConvertValue()
        {
            // Arrange
            var columnNames = new[] { "IsActive" };
            var values = new[] { new object[] { true } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act & Assert
            Assert.True(collection.GetBoolean("IsActive"));
        }

        [Fact]
        public void GetDateTime_ByName_ShouldConvertValue()
        {
            // Arrange
            var date = new DateTime(2024, 1, 1);
            var columnNames = new[] { "CreatedAt" };
            var values = new[] { new object[] { date } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act & Assert
            Assert.Equal(date, collection.GetDateTime("CreatedAt"));
        }

        [Fact]
        public void GetDecimal_ByName_ShouldConvertValue()
        {
            // Arrange
            var columnNames = new[] { "Price" };
            var values = new[] { new object[] { 99.99m } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act & Assert
            Assert.Equal(99.99m, collection.GetDecimal("Price"));
        }

        [Fact]
        public void GetGuid_ByName_ShouldReturnGuidValue()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var columnNames = new[] { "Uid" };
            var values = new[] { new object[] { guid } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act & Assert
            Assert.Equal(guid, collection.GetGuid("Uid"));
        }

        [Fact]
        public void GetDouble_ByName_ShouldConvertValue()
        {
            // Arrange
            var columnNames = new[] { "Temperature" };
            var values = new[] { new object[] { 36.5 } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act & Assert
            Assert.Equal(36.5, collection.GetDouble("Temperature"));
        }

        [Fact]
        public void GetInt16_ByName_ShouldConvertValue()
        {
            // Arrange
            var columnNames = new[] { "SmallNumber" };
            var values = new[] { new object[] { (short)123 } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act & Assert
            Assert.Equal((short)123, collection.GetInt16("SmallNumber"));
        }

        [Fact]
        public void GetInt64_ByName_ShouldConvertValue()
        {
            // Arrange
            var columnNames = new[] { "BigNumber" };
            var values = new[] { new object[] { 9876543210L } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act & Assert
            Assert.Equal(9876543210L, collection.GetInt64("BigNumber"));
        }

        [Fact]
        public void GetByte_ByName_ShouldConvertValue()
        {
            // Arrange
            var columnNames = new[] { "ByteValue" };
            var values = new[] { new object[] { (byte)255 } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act & Assert
            Assert.Equal((byte)255, collection.GetByte("ByteValue"));
        }

        [Fact]
        public void GetChar_ByName_ShouldConvertValue()
        {
            // Arrange
            var columnNames = new[] { "CharValue" };
            var values = new[] { new object[] { 'A' } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act & Assert
            Assert.Equal('A', collection.GetChar("CharValue"));
        }

        [Fact]
        public void GetFloat_ByName_ShouldConvertValue()
        {
            // Arrange
            var columnNames = new[] { "FloatValue" };
            var values = new[] { new object[] { 3.14f } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act & Assert
            Assert.Equal(3.14f, collection.GetFloat("FloatValue"));
        }

        [Fact]
        public void IsDBNull_ByName_ShouldReturnTrueForNull()
        {
            // Arrange
            var columnNames = new[] { "Id", "NullableValue" };
            var values = new[] { new object[] { 1, null } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act & Assert
            Assert.False(collection.IsDBNull("Id"));
            Assert.True(collection.IsDBNull("NullableValue"));
        }

        [Fact]
        public void TypedGetters_ByName_IsCaseInsensitive()
        {
            // Arrange
            var collection = CreateTestCollection();
            collection.Read();

            // Act & Assert
            Assert.Equal(1, collection.GetInt32("Id"));
            Assert.Equal(1, collection.GetInt32("ID"));
            Assert.Equal(1, collection.GetInt32("id"));

            Assert.Equal("Alice", collection.GetString("Name"));
            Assert.Equal("Alice", collection.GetString("NAME"));
            Assert.Equal("Alice", collection.GetString("name"));
        }

        [Fact]
        public void TypedGetters_ByName_WithInvalidColumnName_ShouldThrowException()
        {
            // Arrange
            var collection = CreateTestCollection();
            collection.Read();

            // Act & Assert
            Assert.Throws<DataReaderColumnException>(() => collection.GetInt32("InvalidColumn"));
            Assert.Throws<DataReaderColumnException>(() => collection.GetString("InvalidColumn"));
            Assert.Throws<DataReaderColumnException>(() => collection.GetBoolean("InvalidColumn"));
        }

        #endregion

        #region Nullable Getters Tests - By Index

        [Fact]
        public void GetNullableBoolean_ByIndex_WithValue_ShouldReturnValue()
        {
            // Arrange
            var columnNames = new[] { "IsActive" };
            var values = new[] { new object[] { true } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableBoolean(0);

            // Assert
            Assert.True(result.HasValue);
            Assert.True(result.Value);
        }

        [Fact]
        public void GetNullableBoolean_ByIndex_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "IsActive" };
            var values = new[] { new object[] { DBNull.Value } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableBoolean(0);

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact]
        public void GetNullableByte_ByIndex_WithValue_ShouldReturnValue()
        {
            // Arrange
            var columnNames = new[] { "ByteValue" };
            var values = new[] { new object[] { (byte)255 } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableByte(0);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal((byte)255, result.Value);
        }

        [Fact]
        public void GetNullableByte_ByIndex_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "ByteValue" };
            var values = new[] { new object[] { null } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableByte(0);

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact]
        public void GetNullableChar_ByIndex_WithValue_ShouldReturnValue()
        {
            // Arrange
            var columnNames = new[] { "CharValue" };
            var values = new[] { new object[] { 'A' } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableChar(0);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal('A', result.Value);
        }

        [Fact]
        public void GetNullableChar_ByIndex_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "CharValue" };
            var values = new[] { new object[] { DBNull.Value } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableChar(0);

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact]
        public void GetNullableDateTime_ByIndex_WithValue_ShouldReturnValue()
        {
            // Arrange
            var date = new DateTime(2024, 1, 1);
            var columnNames = new[] { "CreatedAt" };
            var values = new[] { new object[] { date } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableDateTime(0);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(date, result.Value);
        }

        [Fact]
        public void GetNullableDateTime_ByIndex_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "CreatedAt" };
            var values = new[] { new object[] { null } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableDateTime(0);

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact]
        public void GetNullableDecimal_ByIndex_WithValue_ShouldReturnValue()
        {
            // Arrange
            var columnNames = new[] { "Price" };
            var values = new[] { new object[] { 99.99m } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableDecimal(0);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(99.99m, result.Value);
        }

        [Fact]
        public void GetNullableDecimal_ByIndex_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "Price" };
            var values = new[] { new object[] { DBNull.Value } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableDecimal(0);

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact]
        public void GetNullableDouble_ByIndex_WithValue_ShouldReturnValue()
        {
            // Arrange
            var columnNames = new[] { "Temperature" };
            var values = new[] { new object[] { 36.5 } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableDouble(0);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(36.5, result.Value);
        }

        [Fact]
        public void GetNullableDouble_ByIndex_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "Temperature" };
            var values = new[] { new object[] { null } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableDouble(0);

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact]
        public void GetNullableFloat_ByIndex_WithValue_ShouldReturnValue()
        {
            // Arrange
            var columnNames = new[] { "FloatValue" };
            var values = new[] { new object[] { 3.14f } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableFloat(0);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(3.14f, result.Value);
        }

        [Fact]
        public void GetNullableFloat_ByIndex_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "FloatValue" };
            var values = new[] { new object[] { DBNull.Value } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableFloat(0);

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact]
        public void GetNullableGuid_ByIndex_WithValue_ShouldReturnValue()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var columnNames = new[] { "Uid" };
            var values = new[] { new object[] { guid } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableGuid(0);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(guid, result.Value);
        }

        [Fact]
        public void GetNullableGuid_ByIndex_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "Uid" };
            var values = new[] { new object[] { null } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableGuid(0);

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact]
        public void GetNullableInt16_ByIndex_WithValue_ShouldReturnValue()
        {
            // Arrange
            var columnNames = new[] { "SmallNumber" };
            var values = new[] { new object[] { (short)123 } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableInt16(0);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal((short)123, result.Value);
        }

        [Fact]
        public void GetNullableInt16_ByIndex_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "SmallNumber" };
            var values = new[] { new object[] { DBNull.Value } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableInt16(0);

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact]
        public void GetNullableInt32_ByIndex_WithValue_ShouldReturnValue()
        {
            // Arrange
            var columnNames = new[] { "Number" };
            var values = new[] { new object[] { 12345 } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableInt32(0);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(12345, result.Value);
        }

        [Fact]
        public void GetNullableInt32_ByIndex_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "Number" };
            var values = new[] { new object[] { null } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableInt32(0);

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact]
        public void GetNullableInt64_ByIndex_WithValue_ShouldReturnValue()
        {
            // Arrange
            var columnNames = new[] { "BigNumber" };
            var values = new[] { new object[] { 9876543210L } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableInt64(0);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(9876543210L, result.Value);
        }

        [Fact]
        public void GetNullableInt64_ByIndex_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "BigNumber" };
            var values = new[] { new object[] { DBNull.Value } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableInt64(0);

            // Assert
            Assert.False(result.HasValue);
        }

        #endregion

        #region Nullable Getters Tests - By Name

        [Fact]
        public void GetNullableBoolean_ByName_WithValue_ShouldReturnValue()
        {
            // Arrange
            var columnNames = new[] { "IsActive" };
            var values = new[] { new object[] { true } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableBoolean("IsActive");

            // Assert
            Assert.True(result.HasValue);
            Assert.True(result.Value);
        }

        [Fact]
        public void GetNullableBoolean_ByName_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "IsActive" };
            var values = new[] { new object[] { null } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableBoolean("IsActive");

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact]
        public void GetNullableByte_ByName_WithValue_ShouldReturnValue()
        {
            // Arrange
            var columnNames = new[] { "ByteValue" };
            var values = new[] { new object[] { (byte)255 } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableByte("ByteValue");

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal((byte)255, result.Value);
        }

        [Fact]
        public void GetNullableByte_ByName_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "ByteValue" };
            var values = new[] { new object[] { DBNull.Value } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableByte("ByteValue");

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact]
        public void GetNullableChar_ByName_WithValue_ShouldReturnValue()
        {
            // Arrange
            var columnNames = new[] { "CharValue" };
            var values = new[] { new object[] { 'A' } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableChar("CharValue");

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal('A', result.Value);
        }

        [Fact]
        public void GetNullableChar_ByName_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "CharValue" };
            var values = new[] { new object[] { null } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableChar("CharValue");

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact]
        public void GetNullableDateTime_ByName_WithValue_ShouldReturnValue()
        {
            // Arrange
            var date = new DateTime(2024, 1, 1);
            var columnNames = new[] { "CreatedAt" };
            var values = new[] { new object[] { date } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableDateTime("CreatedAt");

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(date, result.Value);
        }

        [Fact]
        public void GetNullableDateTime_ByName_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "CreatedAt" };
            var values = new[] { new object[] { DBNull.Value } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableDateTime("CreatedAt");

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact]
        public void GetNullableDecimal_ByName_WithValue_ShouldReturnValue()
        {
            // Arrange
            var columnNames = new[] { "Price" };
            var values = new[] { new object[] { 99.99m } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableDecimal("Price");

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(99.99m, result.Value);
        }

        [Fact]
        public void GetNullableDecimal_ByName_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "Price" };
            var values = new[] { new object[] { null } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableDecimal("Price");

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact]
        public void GetNullableDouble_ByName_WithValue_ShouldReturnValue()
        {
            // Arrange
            var columnNames = new[] { "Temperature" };
            var values = new[] { new object[] { 36.5 } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableDouble("Temperature");

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(36.5, result.Value);
        }

        [Fact]
        public void GetNullableDouble_ByName_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "Temperature" };
            var values = new[] { new object[] { DBNull.Value } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableDouble("Temperature");

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact]
        public void GetNullableFloat_ByName_WithValue_ShouldReturnValue()
        {
            // Arrange
            var columnNames = new[] { "FloatValue" };
            var values = new[] { new object[] { 3.14f } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableFloat("FloatValue");

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(3.14f, result.Value);
        }

        [Fact]
        public void GetNullableFloat_ByName_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "FloatValue" };
            var values = new[] { new object[] { null } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableFloat("FloatValue");

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact]
        public void GetNullableGuid_ByName_WithValue_ShouldReturnValue()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var columnNames = new[] { "Uid" };
            var values = new[] { new object[] { guid } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableGuid("Uid");

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(guid, result.Value);
        }

        [Fact]
        public void GetNullableGuid_ByName_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "Uid" };
            var values = new[] { new object[] { DBNull.Value } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableGuid("Uid");

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact]
        public void GetNullableInt16_ByName_WithValue_ShouldReturnValue()
        {
            // Arrange
            var columnNames = new[] { "SmallNumber" };
            var values = new[] { new object[] { (short)123 } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableInt16("SmallNumber");

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal((short)123, result.Value);
        }

        [Fact]
        public void GetNullableInt16_ByName_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "SmallNumber" };
            var values = new[] { new object[] { null } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableInt16("SmallNumber");

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact]
        public void GetNullableInt32_ByName_WithValue_ShouldReturnValue()
        {
            // Arrange
            var columnNames = new[] { "Number" };
            var values = new[] { new object[] { 12345 } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableInt32("Number");

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(12345, result.Value);
        }

        [Fact]
        public void GetNullableInt32_ByName_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "Number" };
            var values = new[] { new object[] { DBNull.Value } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableInt32("Number");

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact]
        public void GetNullableInt64_ByName_WithValue_ShouldReturnValue()
        {
            // Arrange
            var columnNames = new[] { "BigNumber" };
            var values = new[] { new object[] { 9876543210L } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableInt64("BigNumber");

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(9876543210L, result.Value);
        }

        [Fact]
        public void GetNullableInt64_ByName_WithNull_ShouldReturnNull()
        {
            // Arrange
            var columnNames = new[] { "BigNumber" };
            var values = new[] { new object[] { null } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act
            var result = collection.GetNullableInt64("BigNumber");

            // Assert
            Assert.False(result.HasValue);
        }

        [Fact]
        public void GetNullableGetters_ByName_IsCaseInsensitive()
        {
            // Arrange
            var columnNames = new[] { "Number", "Price" };
            var values = new[] { new object[] { 123, 99.99m } };
            var collection = new RowDataReader(columnNames, values);
            collection.Read();

            // Act & Assert
            Assert.Equal(123, collection.GetNullableInt32("Number").Value);
            Assert.Equal(123, collection.GetNullableInt32("NUMBER").Value);
            Assert.Equal(123, collection.GetNullableInt32("number").Value);

            Assert.Equal(99.99m, collection.GetNullableDecimal("Price").Value);
            Assert.Equal(99.99m, collection.GetNullableDecimal("PRICE").Value);
            Assert.Equal(99.99m, collection.GetNullableDecimal("price").Value);
        }

        [Fact]
        public void GetNullableGetters_ByName_WithInvalidColumnName_ShouldThrowException()
        {
            // Arrange
            var collection = CreateTestCollection();
            collection.Read();

            // Act & Assert
            Assert.Throws<DataReaderColumnException>(() => collection.GetNullableInt32("InvalidColumn"));
            Assert.Throws<DataReaderColumnException>(() => collection.GetNullableInt64("InvalidColumn"));
            Assert.Throws<DataReaderColumnException>(() => collection.GetNullableBoolean("InvalidColumn"));
        }

        #endregion

        #region Helper Methods

        private RowDataReader CreateTestCollection()
        {
            var columnNames = new[] { "Id", "Name", "Age" };
            var values = new[]
            {
                new object[] { 1, "Alice", 30 },
                new object[] { 2, "Bob", 25 }
            };

            return new RowDataReader(columnNames, values);
        }

        #endregion
    }
}
