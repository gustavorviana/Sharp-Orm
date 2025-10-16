using SharpOrm.DataTranslation;
using SharpOrm.Errors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SharpOrm
{
    /// <summary>
    /// Represents a memory-efficient collection of rows with a shared schema that implements IDataReader.
    /// All rows share the same column names, storing only values to minimize memory usage.
    /// </summary>
    public class RowDataReader : IDataReader, IEnumerable<Row>
    {
        private readonly IDataStreaming _dataStreaming;
        private bool _isFirstRead = true;

        /// <summary>
        /// Gets the column names shared by all rows in this collection.
        /// </summary>
        public string[] ColumnNames => _dataStreaming.Names;

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the RowCollection class with the specified column names and values.
        /// </summary>
        /// <param name="columnNames">The names of the columns shared by all rows.</param>
        /// <param name="values">A 2D array where each sub-array represents the values for one row.</param>
        /// <exception cref="ArgumentNullException">Thrown when columnNames or values is null.</exception>
        /// <exception cref="ArgumentException">Thrown when any row has a different number of values than columns.</exception>
        public RowDataReader(string[] columnNames, object[][] rows)
        {
            if (columnNames == null || columnNames.Length == 0)
                throw new ArgumentNullException(nameof(rows));

            if (rows == null)
                throw new ArgumentNullException(nameof(rows));

            _dataStreaming = new RawRowStreaming(columnNames, rows);
        }

        /// <summary>
        /// Initializes a new instance of the RowCollection class from a collection of Row objects.
        /// Extracts the schema from the first row and validates that all rows have the same columns.
        /// </summary>
        /// <param name="rows">The collection of rows to convert.</param>
        /// <exception cref="ArgumentNullException">Thrown when rows is null.</exception>
        /// <exception cref="ArgumentException">Thrown when rows is empty or rows have inconsistent schemas.</exception>
        public RowDataReader(IEnumerable<Row> rows)
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));

            _dataStreaming = new RowStreaming(rows);
        }

        /// <summary>
        /// Initializes a new instance of the RowCollection class from a single Row object.
        /// </summary>
        /// <param name="row">The row to convert.</param>
        public RowDataReader(Row row) : this(new Row[] { row })
        {

        }

        /// <summary>
        /// Initializes a new instance of the RowCollection class from an IDataReader.
        /// Reads all rows from the reader into memory.
        /// </summary>
        /// <param name="reader">The data reader to read from.</param>
        /// <exception cref="ArgumentNullException">Thrown when reader is null.</exception>
        /// <remarks>
        /// This constructor reads all available rows from the reader. The reader is NOT disposed after reading.
        /// </remarks>
        public RowDataReader(IDataReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            _dataStreaming = new DataReaderStreaming(reader);
        }

        /// <summary>
        /// Creates an empty RowCollection with the specified column schema.
        /// </summary>
        /// <param name="columnNames">The names of the columns.</param>
        /// <returns>An empty RowCollection with the specified schema.</returns>
        public static RowDataReader Empty(params string[] columnNames)
        {
            return new RowDataReader(columnNames, DotnetUtils.EmptyArray<object[]>());
        }

        #endregion

        #region IDataReader Core Methods

        /// <summary>
        /// Gets the number of columns in the current row.
        /// </summary>
        public int FieldCount => _dataStreaming.FieldCount;

        /// <summary>
        /// Gets a value indicating whether the data reader is closed.
        /// </summary>
        public bool IsClosed => _dataStreaming.IsClosed;

        /// <summary>
        /// Gets the number of rows changed, inserted, or deleted by execution of the SQL statement.
        /// Always returns -1 as this is an in-memory collection.
        /// </summary>
        public int RecordsAffected => -1;

        /// <summary>
        /// Gets the depth of nesting for the current row. Always returns 0.
        /// </summary>
        public int Depth => 0;

        /// <summary>
        /// Gets the value of the specified column in its native format.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        /// <exception cref="InvalidOperationException">Thrown when Read has not been called or returned false.</exception>
        /// <exception cref="IndexOutOfRangeException">Thrown when the index is out of range.</exception>
        public object this[int i] => GetValue(i);

        /// <summary>
        /// Gets the value of the specified column in its native format.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column.</returns>
        /// <exception cref="InvalidOperationException">Thrown when Read has not been called or returned false.</exception>
        /// <exception cref="IndexOutOfRangeException">Thrown when the column name is not found.</exception>
        public object this[string name] => GetValue(GetOrdinal(name));

        /// <summary>
        /// Advances the reader to the next record.
        /// </summary>
        /// <returns>true if there are more rows; otherwise, false.</returns>
        public bool Read()
        {
            if (IsClosed)
                throw new InvalidOperationException("The reader is closed.");

            return _dataStreaming.Read();
        }

        /// <summary>
        /// Closes the IDataReader object.
        /// </summary>
        public void Close() => _dataStreaming.Close();

        /// <summary>
        /// Not supported. Always returns null.
        /// </summary>
        public DataTable GetSchemaTable()
        {
            var schemaTable = new DataTable("SchemaTable");

            schemaTable.Columns.Add("ColumnName", typeof(string));
            schemaTable.Columns.Add("ColumnOrdinal", typeof(int));
            schemaTable.Columns.Add("ColumnSize", typeof(int));
            schemaTable.Columns.Add("DataType", typeof(Type));
            schemaTable.Columns.Add("AllowDBNull", typeof(bool));

            for (int i = 0; i < ColumnNames.Length; i++)
            {
                var row = schemaTable.NewRow();
                row["ColumnName"] = ColumnNames[i];
                row["ColumnOrdinal"] = i;
                row["ColumnSize"] = -1;
                row["DataType"] = GetFieldType(i);
                row["AllowDBNull"] = true;
                schemaTable.Rows.Add(row);
            }

            return schemaTable;
        }

        /// <summary>
        /// Advances the data reader to the next result, when reading the results of batch SQL statements.
        /// Always returns false as this collection contains a single result set.
        /// </summary>
        public bool NextResult() => false;

        /// <summary>
        /// Disposes the reader.
        /// </summary>
        public void Dispose() => _dataStreaming.Dispose();

        #endregion

        #region IDataRecord Implementation

        /// <summary>
        /// Gets the value of the specified column in its native format.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no current row is available.</exception>
        /// <exception cref="IndexOutOfRangeException">Thrown when the index is out of range.</exception>
        public object GetValue(int i)
        {
            ValidateCurrentRow();
            if (i < 0 || i >= ColumnNames.Length)
                throw new IndexOutOfRangeException($"Column index {i} is out of range.");

            return IsDBNull(i) ? null : _dataStreaming.GetValue(i);
        }

        /// <summary>
        /// Populates an array of objects with the column values of the current row.
        /// </summary>
        /// <param name="values">An array of Object to copy the attribute fields into.</param>
        /// <returns>The number of instances of Object in the array.</returns>
        public int GetValues(object[] values)
        {
            ValidateCurrentRow();
            return _dataStreaming.GetValues(values);
        }

        /// <summary>
        /// Gets the name of the specified column.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The name of the specified column.</returns>
        public string GetName(int i)
        {
            if (i < 0 || i >= ColumnNames.Length)
                throw new IndexOutOfRangeException($"Column index {i} is out of range.");

            return ColumnNames[i];
        }

        /// <summary>
        /// Gets the column ordinal, given the name of the column.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The zero-based column ordinal.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when the column name is not found.</exception>
        public int GetOrdinal(string name)
        {
            if (_dataStreaming.ColumnOrdinals.TryGetValue(name, out int ordinal))
                return ordinal;

            throw new DataReaderColumnException($"Column '{name}' not found.", name);
        }

        /// <summary>
        /// Gets the data type information for the specified field.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The data type information for the specified field.</returns>
        public Type GetFieldType(int i)
        {
            if (i < 0 || i >= ColumnNames.Length)
                throw new DataReaderColumnException($"Column index {i} is out of range.", i);

            return _dataStreaming.ColumnTypes[i];
        }

        /// <summary>
        /// Gets the data type name for the specified field.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The data type name for the specified field.</returns>
        public string GetDataTypeName(int i)
        {
            return GetFieldType(i).Name;
        }

        /// <summary>
        /// Return whether the specified field is set to null.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>true if the specified field is set to null; otherwise, false.</returns>
        public bool IsDBNull(int i)
        {
            ValidateCurrentRow();

            return _dataStreaming.IsDBNull(i);
        }

        #endregion

        #region Typed Getters

        // By index (int)
        /// <summary>
        /// Gets the value of the specified column as a Boolean.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public bool GetBoolean(int i) => Convert.ToBoolean(GetValue(i));

        /// <summary>
        /// Gets the value of the specified column as a byte.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public byte GetByte(int i) => Convert.ToByte(GetValue(i));

        /// <summary>
        /// Gets the value of the specified column as a character.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public char GetChar(int i) => Convert.ToChar(GetValue(i));

        /// <summary>
        /// Gets the value of the specified column as a DateTime.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public DateTime GetDateTime(int i) => Convert.ToDateTime(GetValue(i));

        /// <summary>
        /// Gets the value of the specified column as a Decimal.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public decimal GetDecimal(int i) => Convert.ToDecimal(GetValue(i));

        /// <summary>
        /// Gets the value of the specified column as a double-precision floating point number.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public double GetDouble(int i) => Convert.ToDouble(GetValue(i));

        /// <summary>
        /// Gets the value of the specified column as a single-precision floating point number.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public float GetFloat(int i) => Convert.ToSingle(GetValue(i));

        /// <summary>
        /// Gets the value of the specified column as a globally unique identifier (GUID).
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public Guid GetGuid(int i)
        {
            var value = GetValue(i);
            if (value is Guid guid)
                return guid;
            return Guid.Parse(value.ToString());
        }

        /// <summary>
        /// Gets the value of the specified column as a 16-bit signed integer.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public short GetInt16(int i) => Convert.ToInt16(GetValue(i));

        /// <summary>
        /// Gets the value of the specified column as a 32-bit signed integer.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public int GetInt32(int i) => Convert.ToInt32(GetValue(i));

        /// <summary>
        /// Gets the value of the specified column as a 64-bit signed integer.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public long GetInt64(int i) => Convert.ToInt64(GetValue(i));

        /// <summary>
        /// Gets the value of the specified column as a string.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        public string GetString(int i) => GetValue(i)?.ToString();

        // By name (string) - Overloads
        /// <summary>
        /// Gets the value of the specified column in its native format.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the specified column.</returns>
        public object GetValue(string name) => GetValue(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a Boolean.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column.</returns>
        public bool GetBoolean(string name) => GetBoolean(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a byte.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column.</returns>
        public byte GetByte(string name) => GetByte(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a character.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column.</returns>
        public char GetChar(string name) => GetChar(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a DateTime.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column.</returns>
        public DateTime GetDateTime(string name) => GetDateTime(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a Decimal.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column.</returns>
        public decimal GetDecimal(string name) => GetDecimal(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a double-precision floating point number.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column.</returns>
        public double GetDouble(string name) => GetDouble(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a single-precision floating point number.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column.</returns>
        public float GetFloat(string name) => GetFloat(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a globally unique identifier (GUID).
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column.</returns>
        public Guid GetGuid(string name) => GetGuid(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a 16-bit signed integer.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column.</returns>
        public short GetInt16(string name) => GetInt16(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a 32-bit signed integer.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column.</returns>
        public int GetInt32(string name) => GetInt32(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a 64-bit signed integer.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column.</returns>
        public long GetInt64(string name) => GetInt64(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a string.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column.</returns>
        public string GetString(string name) => GetString(GetOrdinal(name));

        /// <summary>
        /// Return whether the specified field is set to null.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>true if the specified field is set to null; otherwise, false.</returns>
        public bool IsDBNull(string name) => IsDBNull(GetOrdinal(name));



        #region Nullable Getters

        /// <summary>
        /// Gets the value of the specified column as a nullable Boolean.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public bool? GetNullableBoolean(int i)
        {
            var value = GetValue(i);
            if (value == null || value is DBNull) return null;
            return Convert.ToBoolean(value);
        }

        /// <summary>
        /// Gets the value of the specified column as a nullable Boolean.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public bool? GetNullableBoolean(string name) => GetNullableBoolean(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a nullable byte.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public byte? GetNullableByte(int i)
        {
            var value = GetValue(i);
            if (value == null || value is DBNull) return null;
            return Convert.ToByte(value);
        }

        /// <summary>
        /// Gets the value of the specified column as a nullable byte.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public byte? GetNullableByte(string name) => GetNullableByte(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a nullable character.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public char? GetNullableChar(int i)
        {
            var value = GetValue(i);
            if (value == null || value is DBNull) return null;
            return Convert.ToChar(value);
        }

        /// <summary>
        /// Gets the value of the specified column as a nullable character.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public char? GetNullableChar(string name) => GetNullableChar(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a nullable DateTime.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public DateTime? GetNullableDateTime(int i)
        {
            var value = GetValue(i);
            if (value == null || value is DBNull) return null;
            return Convert.ToDateTime(value);
        }

        /// <summary>
        /// Gets the value of the specified column as a nullable DateTime.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public DateTime? GetNullableDateTime(string name) => GetNullableDateTime(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a nullable Decimal.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public decimal? GetNullableDecimal(int i)
        {
            var value = GetValue(i);
            if (value == null || value is DBNull) return null;
            return Convert.ToDecimal(value);
        }

        /// <summary>
        /// Gets the value of the specified column as a nullable Decimal.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public decimal? GetNullableDecimal(string name) => GetNullableDecimal(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a nullable double-precision floating point number.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public double? GetNullableDouble(int i)
        {
            var value = GetValue(i);
            if (value == null || value is DBNull) return null;
            return Convert.ToDouble(value);
        }

        /// <summary>
        /// Gets the value of the specified column as a nullable double-precision floating point number.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public double? GetNullableDouble(string name) => GetNullableDouble(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a nullable single-precision floating point number.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public float? GetNullableFloat(int i)
        {
            var value = GetValue(i);
            if (value == null || value is DBNull) return null;
            return Convert.ToSingle(value);
        }

        /// <summary>
        /// Gets the value of the specified column as a nullable single-precision floating point number.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public float? GetNullableFloat(string name) => GetNullableFloat(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a nullable globally unique identifier (GUID).
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public Guid? GetNullableGuid(int i)
        {
            var value = GetValue(i);
            if (value == null || value is DBNull) return null;
            if (value is Guid guid) return guid;
            return Guid.Parse(value.ToString());
        }

        /// <summary>
        /// Gets the value of the specified column as a nullable globally unique identifier (GUID).
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public Guid? GetNullableGuid(string name) => GetNullableGuid(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a nullable 16-bit signed integer.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public short? GetNullableInt16(int i)
        {
            var value = GetValue(i);
            if (value == null || value is DBNull) return null;
            return Convert.ToInt16(value);
        }

        /// <summary>
        /// Gets the value of the specified column as a nullable 16-bit signed integer.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public short? GetNullableInt16(string name) => GetNullableInt16(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a nullable 32-bit signed integer.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public int? GetNullableInt32(int i)
        {
            var value = GetValue(i);
            if (value == null || value is DBNull) return null;
            return Convert.ToInt32(value);
        }

        /// <summary>
        /// Gets the value of the specified column as a nullable 32-bit signed integer.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public int? GetNullableInt32(string name) => GetNullableInt32(GetOrdinal(name));

        /// <summary>
        /// Gets the value of the specified column as a nullable 64-bit signed integer.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public long? GetNullableInt64(int i)
        {
            var value = GetValue(i);
            if (value == null || value is DBNull) return null;
            return Convert.ToInt64(value);
        }

        /// <summary>
        /// Gets the value of the specified column as a nullable 64-bit signed integer.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The value of the column, or null if the value is null or DBNull.</returns>
        public long? GetNullableInt64(string name) => GetNullableInt64(GetOrdinal(name));

        #endregion

        /// <summary>
        /// Reads a stream of bytes from the specified column offset into the buffer as an array, starting at the given buffer offset.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <param name="fieldOffset">The index within the field from which to start the read operation.</param>
        /// <param name="buffer">The buffer into which to read the stream of bytes.</param>
        /// <param name="bufferoffset">The index within the buffer where the write operation is to start.</param>
        /// <param name="length">The maximum length to copy into the buffer.</param>
        /// <returns>The actual number of bytes read.</returns>
        /// <exception cref="DataReaderColumnException">Thrown when the column is not a byte array.</exception>
        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            var value = GetValue(i);
            if (value == null || value is DBNull)
                return 0;

            if (!(value is byte[] bytes))
                throw new DataReaderColumnException($"Column at index {i} is not a byte array.", i);

            if (buffer == null)
                return bytes.Length;

            long bytesToCopy = Math.Min(length, bytes.Length - fieldOffset);
            Array.Copy(bytes, fieldOffset, buffer, bufferoffset, bytesToCopy);
            return bytesToCopy;
        }

        /// <summary>
        /// Reads a stream of characters from the specified column offset into the buffer as an array, starting at the given buffer offset.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <param name="fieldoffset">The index within the field from which to start the read operation.</param>
        /// <param name="buffer">The buffer into which to read the stream of characters.</param>
        /// <param name="bufferoffset">The index within the buffer where the write operation is to start.</param>
        /// <param name="length">The maximum length to copy into the buffer.</param>
        /// <returns>The actual number of characters read.</returns>
        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            var value = GetValue(i);
            if (value == null || value is DBNull)
                return 0;

            var str = value.ToString();
            if (str == null)
                return 0;

            if (buffer == null)
                return str.Length;

            long charsToRead = Math.Min(length, str.Length - fieldoffset);
            str.CopyTo((int)fieldoffset, buffer, bufferoffset, (int)charsToRead);
            return charsToRead;
        }

        /// <summary>
        /// Returns an IDataReader for the specified column ordinal.
        /// This method is not supported and always throws NotSupportedException.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>An IDataReader.</returns>
        /// <exception cref="NotSupportedException">Always thrown as this method is not supported.</exception>
        public IDataReader GetData(int i)
        {
            throw new NotSupportedException("GetData is not supported");
        }

        #endregion

        #region Row Conversion

        /// <summary>
        /// Converts the current row to a Row object.
        /// </summary>
        /// <returns>A Row object representing the current row.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no current row is available.</exception>
        public Row ToRow()
        {
            ValidateCurrentRow();

            Cell[] cells = new Cell[ColumnNames.Length];
            for (int i = 0; i < ColumnNames.Length; i++)
                cells[i] = new Cell(ColumnNames[i], GetValue(i));

            return new Row(cells);
        }

        /// <summary>
        /// Converts all rows in this collection to an array of Row objects.
        /// </summary>
        /// <returns>An array of Row objects.</returns>
        public Row[] ReadToEnd()
        {
            if (_dataStreaming.Finished)
                return DotnetUtils.EmptyArray<Row>();

            var rows = new List<Row>();

            while (_dataStreaming.Read())
                rows.Add(ToRow());

            return rows.ToArray();
        }

        #endregion

        /// <summary>
        /// Returns an enumerator that iterates through the collection as Row objects.
        /// </summary>
        /// <returns>An enumerator for the collection.</returns>
        public IEnumerator<Row> GetEnumerator()
        {
            if (!_isFirstRead)
                ThrowCannotEnumerateAfterReadException();

            while (Read())
                yield return ToRow();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Determines whether the row contains a column with the specified name.
        /// </summary>
        /// <param name="columnName">The name of the column to check for existence.</param>
        /// <returns>True if the column exists in the row; otherwise, false.</returns>
        public bool HasColumn(string columnName)
        {
            return _dataStreaming.ColumnOrdinals.ContainsKey(columnName);
        }

        private void ValidateCurrentRow()
        {
            if (_dataStreaming.IsClosed)
                throw new DataReaderException("The reader is closed.");

            if (_dataStreaming.Finished)
                throw new DataReaderException("Invalid attempt to read when no data is present.");
        }

        #region Static Helper Methods for Exceptions

        /// <summary>
        /// Throws an exception when no current row is available for reading.
        /// </summary>
        private static void ThrowNoCurrentRowException()
        {
            throw new DataReaderException("No current row is available. Call Read() and ensure it returns true before accessing data.");
        }

        /// <summary>
        /// Throws an exception when the column index is out of range.
        /// </summary>
        private static void ThrowColumnIndexOutOfRangeException(int index, int columnCount)
        {
            throw new DataReaderColumnException($"Column index {index} is out of range. Valid range is 0 to {columnCount - 1}.", index);
        }

        /// <summary>
        /// Throws an exception when a row has mismatched column count.
        /// </summary>
        private static void ThrowRowColumnCountMismatchException(int rowIndex, int actualCount, int expectedCount)
        {
            throw new DataReaderException($"Row at index {rowIndex} has {actualCount} values but expected {expectedCount} to match column count.");
        }

        /// <summary>
        /// Throws an exception when attempting to enumerate after Read() has been called.
        /// </summary>
        private static void ThrowCannotEnumerateAfterReadException()
        {
            throw new DataReaderException("Cannot enumerate after Read() has been called. The reader can only be enumerated once from the beginning.");
        }

        #endregion

        private class RawRowStreaming : IDataStreaming
        {
            private readonly string[] _columnNames;
            private readonly IEnumerator<object[]> _enumerator;
            private readonly Dictionary<int, Type> _columnTypes;
            private readonly Dictionary<string, int> _columnOrdinals;

            private int _index = -1;
            private bool _isClosed;
            private bool _finished;
            private bool _peekedFirst;

            public RawRowStreaming(string[] columnNames, IEnumerable<object[]> rows)
            {
                _columnNames = columnNames ?? throw new ArgumentNullException(nameof(columnNames));
                _enumerator = (rows ?? throw new ArgumentNullException(nameof(rows))).GetEnumerator();

                _columnTypes = new Dictionary<int, Type>(_columnNames.Length);
                _columnOrdinals = new Dictionary<string, int>(_columnNames.Length, StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < _columnNames.Length; i++)
                    _columnOrdinals[_columnNames[i]] = i;

                if (!_enumerator.MoveNext())
                {
                    for (int i = 0; i < _columnNames.Length; i++)
                        _columnTypes[i] = typeof(object);

                    return;
                }

                _peekedFirst = true;

                for (int i = 0; i < _columnNames.Length; i++)
                    _columnTypes[i] = _enumerator.Current[i]?.GetType() ?? typeof(object);
            }

            public string[] Names => _columnNames;
            public bool IsClosed => _isClosed;
            public int FieldCount => _columnNames.Length;
            public bool Finished => _finished;
            public IReadOnlyDictionary<string, int> ColumnOrdinals => _columnOrdinals;
            public IReadOnlyDictionary<int, Type> ColumnTypes => _columnTypes;

            public void Close()
            {
                _isClosed = true;
                _finished = true;
                _enumerator.Dispose();
            }

            public void Dispose() => Close();

            public bool Read()
            {
                if (_isClosed) return false;

                if (_peekedFirst)
                {
                    _peekedFirst = false;
                }
                else if (!_enumerator.MoveNext())
                {
                    _finished = true;
                    return false;
                }

                _index++;

                if (_enumerator.Current == null)
                    throw new DataReaderException($"Row at index {_index} is null.");

                if (_enumerator.Current.Length != _columnNames.Length)
                    try
                    {
                        ThrowRowColumnCountMismatchException(_index, _enumerator.Current.Length, _columnNames.Length);
                    }
                    finally
                    {
                        Close();
                    }

                for (int i = 0; i < _columnNames.Length; i++)
                    if (!_columnTypes.ContainsKey(i))
                        _columnTypes[i] = _enumerator.Current[i]?.GetType() ?? typeof(object);

                return true;
            }

            public object GetValue(int index)
            {
                Validate();

                if (index < 0 || index >= _columnNames.Length)
                    ThrowColumnIndexOutOfRangeException(index, _columnNames.Length);

                return _enumerator.Current[index];
            }

            public int GetValues(object[] values)
            {
                Validate();

                int count = Math.Min(values.Length, _columnNames.Length);
                Array.Copy(_enumerator.Current, values, count);

                return count;
            }

            private void Validate()
            {
                if (_index < 0)
                    ThrowNoCurrentRowException();
            }

            public bool IsDBNull(int i) => TranslationUtils.IsNull(GetValue(i));
        }

        private class RowStreaming : IDataStreaming
        {
            private IEnumerator<Row> _enumerator;
            private bool _peekedFirst;
            private bool _isClosed;
            private bool _finished;
            private int _index = -1;

            private string[] _columnNames;
            private object[] _currentValues;

            private readonly Dictionary<string, int> _columnOrdinals;
            private readonly Dictionary<int, Type> _columnTypes;

            public RowStreaming(IEnumerable<Row> rows)
            {
                _enumerator = rows.GetEnumerator();
                _isClosed = false;

                _columnNames = DotnetUtils.EmptyArray<string>();
                _columnOrdinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                _columnTypes = new Dictionary<int, Type>();

                if (!_enumerator.MoveNext())
                    return;

                var firstRowPeek = _enumerator.Current;
                _peekedFirst = true;

                _columnNames = firstRowPeek.Cells.Select(c => c.Name).ToArray();
                for (int i = 0; i < _columnNames.Length; i++)
                {
                    _columnOrdinals[_columnNames[i]] = i;
                    _columnTypes[i] = firstRowPeek.Cells[i].Value?.GetType() ?? typeof(object);
                }
            }

            public string[] Names => _columnNames;
            public bool IsClosed => _isClosed;
            public int FieldCount => _columnNames.Length;
            public bool Finished => _isClosed;

            public IReadOnlyDictionary<string, int> ColumnOrdinals => _columnOrdinals;
            public IReadOnlyDictionary<int, Type> ColumnTypes => _columnTypes;

            public void Close()
            {
                _isClosed = true;
                _finished = true;
                _enumerator.Dispose();
                _currentValues = null;
            }

            public void Dispose() => Close();

            public bool Read()
            {
                if (_isClosed) return false;

                if (_peekedFirst)
                {
                    _peekedFirst = false;
                }
                else if (!_enumerator.MoveNext())
                {
                    _finished = true;
                    return false;
                }

                _index++;
                _currentValues = _enumerator.Current.Select(x => x.Value).ToArray();
                if (_currentValues.Length != FieldCount)
                {
                    try
                    {
                        ThrowRowColumnCountMismatchException(_index, _currentValues.Length, _columnNames.Length);
                    }
                    finally
                    {
                        Close();
                    }
                }

                for (int i = 0; i < _columnNames.Length; i++)
                    if (_columnNames[i] != _enumerator.Current[i].Name)
                        throw new DataReaderException($"Row at index {_index} has column '{_enumerator.Current[i].Name}' at position {i} but expected '{_columnNames[i]}'.");

                return true;
            }

            public object GetValue(int index)
            {
                Validate();

                if (index < 0 || index >= _columnNames.Length)
                    ThrowColumnIndexOutOfRangeException(index, _columnNames.Length);

                return _currentValues[index];
            }

            public int GetValues(object[] values)
            {
                Validate();

                if (_currentValues == null)
                    ThrowNoCurrentRowException();

                int count = Math.Min(values.Length, _columnNames.Length);
                Array.Copy(_currentValues, values, count);
                return count;
            }

            private void Validate()
            {
                if (_index < 0)
                    ThrowNoCurrentRowException();
            }

            public bool IsDBNull(int i) => TranslationUtils.IsNull(GetValue(i));
        }

        private class DataReaderStreaming : IDataStreaming
        {
            private readonly IDataReader _reader;
            public string[] Names { get; }

            private readonly Dictionary<int, Type> _columnTypes;
            private readonly Dictionary<string, int> _columnOrdinals;
            public int FieldCount => _reader.FieldCount;
            private bool _finished = false;

            public DataReaderStreaming(IDataReader reader)
            {
                _reader = reader;

                int fieldCount = reader.FieldCount;
                Names = new string[fieldCount];
                _columnTypes = new Dictionary<int, Type>(fieldCount);
                _columnOrdinals = new Dictionary<string, int>(fieldCount, StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < fieldCount; i++)
                {
                    var name = reader.GetName(i);

                    Names[i] = name;
                    _columnTypes[i] = reader.GetFieldType(i);
                    _columnOrdinals[name] = i;
                }
            }

            public bool IsClosed => _reader.IsClosed;

            public IReadOnlyDictionary<string, int> ColumnOrdinals => _columnOrdinals;

            public IReadOnlyDictionary<int, Type> ColumnTypes => _columnTypes;

            public bool Finished => _finished;

            public void Dispose() => _reader.Dispose();

            public object GetValue(int index) => _reader.GetValue(index);

            public bool Read()
            {
                if (_reader.Read())
                    return true;

                _finished = true;
                return false;
            }

            public void Close() => _reader.Close();

            public int GetValues(object[] values) => _reader.GetValues(values);

            public bool IsDBNull(int i) => _reader.IsDBNull(i);
        }

        private interface IDataStreaming : IDisposable
        {
            bool IsClosed { get; }
            int FieldCount { get; }
            bool Finished { get; }
            string[] Names { get; }
            IReadOnlyDictionary<string, int> ColumnOrdinals { get; }
            IReadOnlyDictionary<int, Type> ColumnTypes { get; }
            bool Read();
            object GetValue(int index);
            void Close();
            int GetValues(object[] values);
            bool IsDBNull(int i);
        }
    }
}
