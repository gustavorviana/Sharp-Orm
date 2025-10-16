# RowDataReader Guide: Memory-Efficient Data Reading

This guide explains how to use the `RowDataReader` class to work with collections of rows in a memory-efficient way while implementing the standard `IDataReader` interface.

## Table of Contents
- [Introduction](#introduction)
- [Core Concepts](#core-concepts)
- [Creating RowDataReader](#creating-rowdatareader)
- [Reading Data](#reading-data)
- [Accessing Column Values](#accessing-column-values)
- [Schema Inspection](#schema-inspection)
- [Enumeration](#enumeration)
- [Best Practices](#best-practices)
- [Common Patterns](#common-patterns)

---

## Introduction

### What is RowDataReader?

`RowDataReader` is a memory-efficient collection of rows that implements the `IDataReader` interface. All rows share the same column schema, storing only values to minimize memory usage.

Key features:
- **IDataReader Implementation**: Compatible with ADO.NET patterns
- **Memory Efficient**: Shared column schema across all rows
- **Multiple Sources**: Create from arrays, collections, or other IDataReaders
- **Standard Access**: Use familiar IDataReader methods
- **Type Conversion**: Automatic type conversion with typed getters
- **Enumerable**: Supports foreach iteration

### Architecture

```
┌───────────────────────────────────────────────────────┐
│                  RowDataReader                        │
│                                                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │  Raw Arrays  │  │ Row Objects  │  │ IDataReader  │ │
│  │ (object[][]) │  │(IEnumerable) │  │              │ │
│  └──────────────┘  └──────────────┘  └──────────────┘ │
│                                                       │
│                    Shared Schema                      │
│                    ┌──────────────┐                   │
│                    │ Column Names │                   │
│                    │ Column Types │                   │
│                    │ Ordinals Map │                   │
│                    └──────────────┘                   │
│                                                       │
│  Features:                                            │
│  • Standard IDataReader interface                     │
│  • Typed getters (GetInt32, GetString, etc.)          │
│  • Null-safe access with IsDBNull()                   │
│  • Schema table generation                            │
│  • IEnumerable<Row> support                           │
│  • Memory-efficient streaming                         │
└───────────────────────────────────────────────────────┘
```

---

## Core Concepts

### Shared Schema

All rows in a `RowDataReader` share the same column schema:

```csharp
// All rows must have the same columns
var columnNames = new[] { "Id", "Name", "Age" };
var rows = new[]
{
    new object[] { 1, "John", 30 },
    new object[] { 2, "Jane", 25 },
    new object[] { 3, "Bob", 35 }
};

var reader = new RowDataReader(columnNames, rows);
```

**Benefits**:
- Reduced memory footprint (column names stored once)
- Faster access (pre-computed ordinal lookup)
- Schema validation (ensures consistent structure)

### IDataReader Pattern

`RowDataReader` follows the standard ADO.NET reader pattern:

```csharp
using (var reader = new RowDataReader(columnNames, rows))
{
    while (reader.Read())
    {
        var id = reader.GetInt32(0);
        var name = reader.GetString(1);
        var age = reader.GetInt32(2);

        Console.WriteLine($"Id: {id}, Name: {name}, Age: {age}");
    }
}
```

### Internal Streaming

`RowDataReader` uses internal streaming implementations:

- **RawRowStreaming**: For object[][] sources
- **RowStreaming**: For IEnumerable<Row> sources
- **DataReaderStreaming**: For IDataReader sources

This abstraction ensures efficient memory usage regardless of the source.

---

## Creating RowDataReader

### Method 1: From Raw Arrays

Create from column names and 2D object array:

```csharp
var columnNames = new[] { "Id", "Name", "Email" };
var rows = new[]
{
    new object[] { 1, "John Doe", "john@example.com" },
    new object[] { 2, "Jane Smith", "jane@example.com" },
    new object[] { 3, "Bob Wilson", "bob@example.com" }
};

var reader = new RowDataReader(columnNames, rows);

using (reader)
{
    while (reader.Read())
    {
        Console.WriteLine($"{reader["Name"]} - {reader["Email"]}");
    }
}
```

**Validation**:
- Column names cannot be null or empty
- All rows must have the same number of values as columns
- Throws `ArgumentException` if validation fails

### Method 2: From Row Collection

Create from an `IEnumerable<Row>`:

```csharp
var rows = new List<Row>
{
    new Row(
        new Cell("Id", 1),
        new Cell("Name", "John"),
        new Cell("Active", true)
    ),
    new Row(
        new Cell("Id", 2),
        new Cell("Name", "Jane"),
        new Cell("Active", false)
    )
};

using (var reader = new RowDataReader(rows))
{
    // Schema extracted from first row
    Console.WriteLine($"Columns: {string.Join(", ", reader.ColumnNames)}");

    while (reader.Read())
    {
        Console.WriteLine($"{reader.GetInt32("Id")}: {reader.GetString("Name")}");
    }
}
```

**Automatic Schema Detection**:
- Schema extracted from the first row
- All subsequent rows validated against this schema
- Throws exception if rows have inconsistent structure

### Method 3: From Single Row

Create from a single `Row` object:

```csharp
var row = new Row(
    new Cell("Id", 1),
    new Cell("Name", "John"),
    new Cell("Status", "Active")
);

using (var reader = new RowDataReader(row))
{
    if (reader.Read())
    {
        Console.WriteLine($"Single row: {reader.GetString("Name")}");
    }
}
```

**Use Cases**:
- Testing single-row scenarios
- Converting Row to IDataReader for compatibility
- Passing single records to methods expecting IDataReader

### Method 4: From Existing IDataReader

Wrap an existing IDataReader:

```csharp
using (var dbReader = command.ExecuteReader())
{
    // Wrap database reader
    using (var reader = new RowDataReader(dbReader))
    {
        // Now you can use RowDataReader features
        while (reader.Read())
        {
            var row = reader.ToRow();
            // Process as Row object
        }
    }
}
```

**Important**:
- The original reader is NOT disposed automatically
- Useful for adding Row conversion capabilities
- Maintains streaming behavior (doesn't load all data)

### Method 5: Empty Reader

Create an empty reader with specified schema:

```csharp
var emptyReader = RowDataReader.Empty("Id", "Name", "Email");

using (emptyReader)
{
    // Has schema but no rows
    Console.WriteLine($"Columns: {emptyReader.FieldCount}");
    Console.WriteLine($"Has rows: {emptyReader.Read()}"); // false
}
```

**Use Cases**:
- Returning empty result sets with defined schema
- Testing scenarios with no data
- Initializing readers before population

---

## Reading Data

### Sequential Reading

The standard IDataReader pattern:

```csharp
var columnNames = new[] { "Id", "Name", "Score" };
var rows = new[]
{
    new object[] { 1, "John", 95.5 },
    new object[] { 2, "Jane", 87.3 },
    new object[] { 3, "Bob", 92.1 }
};

var reader = new RowDataReader(columnNames, rows);

using (reader)
{
    while (reader.Read())
    {
        // Access current row data
        var id = reader.GetInt32(0);
        var name = reader.GetString(1);
        var score = reader.GetDouble(2);

        Console.WriteLine($"{id}: {name} scored {score}");
    }
}
```

### Read() Method Behavior

```csharp
var reader = new RowDataReader(columnNames, rows);

// Before first Read(): no current row
// reader.GetValue(0); // Would throw exception

if (reader.Read())
{
    // First row is now current
    var value = reader.GetValue(0);
}

if (reader.Read())
{
    // Second row is now current
    var value = reader.GetValue(0);
}

if (!reader.Read())
{
    // No more rows
    Console.WriteLine("End of data");
}
```

**Read() Returns**:
- `true`: Successfully advanced to next row
- `false`: No more rows available

### Checking Reader State

```csharp
using (var reader = new RowDataReader(columnNames, rows))
{
    Console.WriteLine($"IsClosed: {reader.IsClosed}"); // false
    Console.WriteLine($"FieldCount: {reader.FieldCount}"); // 3

    while (reader.Read())
    {
        // Process rows
    }

    reader.Close();
    Console.WriteLine($"IsClosed: {reader.IsClosed}"); // true
}
```

### ReadToEnd() Method

Read all remaining rows as an array:

```csharp
var reader = new RowDataReader(columnNames, rows);

// Option 1: Read all rows at once
var allRows = reader.ReadToEnd();
foreach (var row in allRows)
{
    Console.WriteLine(row["Name"]);
}

// Option 2: Read some, then read rest
var reader2 = new RowDataReader(columnNames, rows);
reader2.Read();
var firstRow = reader2.ToRow();

var remainingRows = reader2.ReadToEnd();
Console.WriteLine($"Remaining: {remainingRows.Length}");
```

**Performance Note**:
- `ReadToEnd()` loads all remaining rows into memory
- Use with caution on large datasets
- For streaming, use `Read()` in a loop instead

---

## Accessing Column Values

### By Index (Fastest)

Access by zero-based column index:

```csharp
using (var reader = new RowDataReader(columnNames, rows))
{
    while (reader.Read())
    {
        // Direct access by index
        var id = reader.GetInt32(0);
        var name = reader.GetString(1);
        var age = reader.GetInt32(2);
    }
}
```

**Performance**: Fastest method (no dictionary lookup)

### By Name

Access by column name:

```csharp
using (var reader = new RowDataReader(columnNames, rows))
{
    while (reader.Read())
    {
        // Access by name (case-insensitive)
        var id = reader.GetInt32("Id");
        var name = reader.GetString("Name");
        var age = reader.GetInt32("Age");
    }
}
```

**Features**:
- Case-insensitive column name matching
- More readable code
- Throws `DataReaderColumnException` if column not found

### Using Indexer

Use array-style indexer:

```csharp
using (var reader = new RowDataReader(columnNames, rows))
{
    while (reader.Read())
    {
        // By index
        var value1 = reader[0];

        // By name
        var value2 = reader["Name"];

        // Returns object, needs casting
        var name = (string)reader["Name"];
    }
}
```

### Typed Getters

Use strongly-typed methods for automatic conversion:

```csharp
using (var reader = new RowDataReader(columnNames, rows))
{
    while (reader.Read())
    {
        // Numeric types
        var id = reader.GetInt32("Id");
        var longId = reader.GetInt64("LongId");
        var shortId = reader.GetInt16("ShortId");
        var byteValue = reader.GetByte("ByteValue");

        // Decimal types
        var price = reader.GetDecimal("Price");
        var score = reader.GetDouble("Score");
        var rate = reader.GetFloat("Rate");

        // Other types
        var name = reader.GetString("Name");
        var isActive = reader.GetBoolean("Active");
        var createdAt = reader.GetDateTime("CreatedAt");
        var guid = reader.GetGuid("UniqueId");
        var letter = reader.GetChar("Letter");
    }
}
```

**Available Typed Getters**:
- `GetBoolean()`
- `GetByte()`
- `GetChar()`
- `GetDateTime()`
- `GetDecimal()`
- `GetDouble()`
- `GetFloat()`
- `GetGuid()`
- `GetInt16()`
- `GetInt32()`
- `GetInt64()`
- `GetString()`

### Nullable Value Getters

Handle nullable values:

```csharp
using (var reader = new RowDataReader(columnNames, rows))
{
    while (reader.Read())
    {
        // Check for null before reading
        if (!reader.IsDBNull("Age"))
        {
            var age = reader.GetInt32("Age");
        }

        // Nullable getters
        var nullableAge = reader.GetNullableInt32("Age");
        var nullableLong = reader.GetNullableInt64("LongValue");

        // Safe usage
        if (nullableAge.HasValue)
        {
            Console.WriteLine($"Age: {nullableAge.Value}");
        }
        else
        {
            Console.WriteLine("Age is null");
        }
    }
}
```

### Binary and Character Data

Handle large binary or character data:

```csharp
using (var reader = new RowDataReader(columnNames, rows))
{
    while (reader.Read())
    {
        // Get bytes (for BLOB/VARBINARY)
        var buffer = new byte[1024];
        var bytesRead = reader.GetBytes(0, 0, buffer, 0, buffer.Length);

        // Get chars (for TEXT/CLOB)
        var charBuffer = new char[1024];
        var charsRead = reader.GetChars(1, 0, charBuffer, 0, charBuffer.Length);
    }
}
```

### GetValues() - Multiple Columns

Get all column values at once:

```csharp
using (var reader = new RowDataReader(columnNames, rows))
{
    while (reader.Read())
    {
        var values = new object[reader.FieldCount];
        var count = reader.GetValues(values);

        Console.WriteLine($"Retrieved {count} values");
        for (int i = 0; i < count; i++)
        {
            Console.WriteLine($"{reader.GetName(i)}: {values[i]}");
        }
    }
}
```

---

## Schema Inspection

### Column Names

```csharp
var reader = new RowDataReader(columnNames, rows);

// Get all column names
foreach (var columnName in reader.ColumnNames)
{
    Console.WriteLine($"Column: {columnName}");
}

// Get specific column name by index
var firstName = reader.GetName(0);
Console.WriteLine($"First column: {firstName}");
```

### Column Ordinals

Get the index of a column by name:

```csharp
using (var reader = new RowDataReader(columnNames, rows))
{
    // Get ordinal (zero-based index)
    var nameOrdinal = reader.GetOrdinal("Name");
    Console.WriteLine($"'Name' is at index: {nameOrdinal}");

    // Use ordinal for fast access
    while (reader.Read())
    {
        var name = reader.GetString(nameOrdinal);
    }
}
```

**Performance Tip**: Cache ordinals for repeated access in loops.

### Column Types

Get the data type of columns:

```csharp
using (var reader = new RowDataReader(columnNames, rows))
{
    for (int i = 0; i < reader.FieldCount; i++)
    {
        var name = reader.GetName(i);
        var type = reader.GetFieldType(i);
        var typeName = reader.GetDataTypeName(i);

        Console.WriteLine($"{name}: {type.Name} ({typeName})");
    }
}
```

**Type Detection**:
- Types are detected from the first row with non-null values
- If first row has nulls, fallback to `typeof(object)`

### Schema Table

Get a DataTable with schema information:

```csharp
using (var reader = new RowDataReader(columnNames, rows))
{
    var schemaTable = reader.GetSchemaTable();

    foreach (DataRow schemaRow in schemaTable.Rows)
    {
        var columnName = schemaRow["ColumnName"];
        var columnOrdinal = schemaRow["ColumnOrdinal"];
        var dataType = schemaRow["DataType"];
        var allowNull = schemaRow["AllowDBNull"];

        Console.WriteLine($"{columnName} [{columnOrdinal}]: {dataType}");
    }
}
```

**Schema Columns**:
- `ColumnName`: Name of the column
- `ColumnOrdinal`: Zero-based index
- `ColumnSize`: Always -1 (unknown)
- `DataType`: System.Type of the column
- `AllowDBNull`: Always true

### HasColumn Check

Check if a column exists:

```csharp
using (var reader = new RowDataReader(columnNames, rows))
{
    if (reader.HasColumn("Email"))
    {
        Console.WriteLine("Email column exists");
    }

    // Case-insensitive check
    if (reader.HasColumn("email"))
    {
        Console.WriteLine("Found (case-insensitive)");
    }
}
```

---

## Enumeration

### foreach Iteration

Enumerate rows as Row objects:

```csharp
var rows = new List<Row>
{
    new Row(new Cell("Id", 1), new Cell("Name", "John")),
    new Row(new Cell("Id", 2), new Cell("Name", "Jane"))
};

var reader = new RowDataReader(rows);

// Enumerate with foreach
foreach (var row in reader)
{
    Console.WriteLine($"Id: {row["Id"]}, Name: {row["Name"]}");
}
```

**Important Restrictions**:
- Can only enumerate if `Read()` has NOT been called
- Throws `DataReaderException` if you call `Read()` before enumeration
- Enumeration can only happen once

### Enumeration vs Read()

```csharp
var reader = new RowDataReader(rows);

// ✓ Correct - enumerate without Read()
foreach (var row in reader)
{
    // Process row
}

// ✗ Incorrect - Read() already called
var reader2 = new RowDataReader(rows);
reader2.Read(); // Called Read()
foreach (var row in reader2) // Throws exception!
{
    // Will never execute
}
```

### ToRow() Conversion

Convert current row to Row object:

```csharp
using (var reader = new RowDataReader(columnNames, rows))
{
    while (reader.Read())
    {
        // Convert current reader position to Row
        var row = reader.ToRow();

        // Now you can use Row methods
        foreach (var cell in row.Cells)
        {
            Console.WriteLine($"{cell.Name}: {cell.Value}");
        }
    }
}
```

**Use Cases**:
- Converting reader data to Row for manipulation
- Passing to methods that expect Row objects
- Using Row-specific functionality

---

## Best Practices

### 1. Always Use using Statement

Ensure proper disposal:

```csharp
// ✓ Correct - ensures disposal
using (var reader = new RowDataReader(columnNames, rows))
{
    while (reader.Read())
    {
        // Process rows
    }
}

// ✗ Avoid - manual disposal is error-prone
var reader = new RowDataReader(columnNames, rows);
try
{
    while (reader.Read())
    {
        // Process rows
    }
}
finally
{
    reader.Dispose();
}
```

### 2. Use Typed Getters

Prefer typed getters over casting:

```csharp
using (var reader = new RowDataReader(columnNames, rows))
{
    while (reader.Read())
    {
        // ✓ Correct - type-safe
        var age = reader.GetInt32("Age");

        // ✗ Avoid - requires casting
        var age2 = (int)reader["Age"];
    }
}
```

### 3. Cache Column Ordinals

For performance in loops:

```csharp
using (var reader = new RowDataReader(columnNames, rows))
{
    // ✓ Correct - cache ordinals outside loop
    var idOrdinal = reader.GetOrdinal("Id");
    var nameOrdinal = reader.GetOrdinal("Name");
    var ageOrdinal = reader.GetOrdinal("Age");

    while (reader.Read())
    {
        var id = reader.GetInt32(idOrdinal);
        var name = reader.GetString(nameOrdinal);
        var age = reader.GetInt32(ageOrdinal);
    }

    // ✗ Avoid - repeated ordinal lookups
    while (reader.Read())
    {
        var id = reader.GetInt32(reader.GetOrdinal("Id")); // Inefficient
    }
}
```

### 4. Check for Nulls

Always check for null values:

```csharp
using (var reader = new RowDataReader(columnNames, rows))
{
    while (reader.Read())
    {
        // ✓ Correct - check for null
        if (!reader.IsDBNull("Email"))
        {
            var email = reader.GetString("Email");
        }

        // ✓ Alternative - use nullable getter
        var age = reader.GetNullableInt32("Age");
        if (age.HasValue)
        {
            Console.WriteLine($"Age: {age.Value}");
        }

        // ✗ Risky - may throw if null
        var email2 = reader.GetString("Email"); // Throws if null
    }
}
```

### 5. Validate Schema

Ensure consistent row structure:

```csharp
var columnNames = new[] { "Id", "Name" };
var rows = new[]
{
    new object[] { 1, "John" },
    new object[] { 2, "Jane" },
    // new object[] { 3 } // ✗ Would throw - wrong column count
};

var reader = new RowDataReader(columnNames, rows); // Validates on creation
```

### 6. Choose Appropriate Access Method

```csharp
using (var reader = new RowDataReader(columnNames, rows))
{
    while (reader.Read())
    {
        // ✓ Fast - by index (no lookup)
        var id = reader.GetInt32(0);

        // ✓ Readable - by name
        var name = reader.GetString("Name");

        // ✓ Best of both - cache ordinal
        var ageOrdinal = reader.GetOrdinal("Age");
        var age = reader.GetInt32(ageOrdinal);
    }
}
```

### 7. Don't Mix Read() and Enumeration

```csharp
// ✓ Correct - use one pattern
var reader1 = new RowDataReader(rows);
while (reader1.Read())
{
    // Process with Read()
}

// ✓ Correct - use other pattern
var reader2 = new RowDataReader(rows);
foreach (var row in reader2)
{
    // Process with foreach
}

// ✗ Incorrect - don't mix
var reader3 = new RowDataReader(rows);
reader3.Read(); // Called Read()
foreach (var row in reader3) // Throws!
{
    // Never executes
}
```

---

## Common Patterns

### Pattern 1: Converting Query Results

Convert database results to RowDataReader:

```csharp
public RowDataReader GetUsersAsReader(Query query)
{
    using (var dbReader = query.ExecuteReader())
    {
        // Wrap database reader
        return new RowDataReader(dbReader);
    }
}

// Usage
var query = new Query("users", manager).Where("active", true);
using (var reader = GetUsersAsReader(query))
{
    while (reader.Read())
    {
        Console.WriteLine(reader.GetString("Name"));
    }
}
```

### Pattern 2: DataReader Compatibility

Make any data source compatible with IDataReader:

```csharp
public IDataReader GetDataAsReader(List<User> users)
{
    var columnNames = new[] { "Id", "Name", "Email", "CreatedAt" };
    var rows = users.Select(u => new object[]
    {
        u.Id,
        u.Name,
        u.Email,
        u.CreatedAt
    }).ToArray();

    return new RowDataReader(columnNames, rows);
}

// Usage with SqlBulkCopy or other IDataReader consumers
using (var reader = GetDataAsReader(userList))
{
    sqlBulkCopy.WriteToServer(reader);
}
```

### Pattern 3: Schema Validation

Validate schema before processing:

```csharp
public bool ValidateSchema(RowDataReader reader, params string[] requiredColumns)
{
    foreach (var column in requiredColumns)
    {
        if (!reader.HasColumn(column))
        {
            Console.WriteLine($"Missing required column: {column}");
            return false;
        }
    }
    return true;
}

// Usage
using (var reader = new RowDataReader(columnNames, rows))
{
    if (ValidateSchema(reader, "Id", "Name", "Email"))
    {
        while (reader.Read())
        {
            // Safe to access required columns
            var id = reader.GetInt32("Id");
            var name = reader.GetString("Name");
            var email = reader.GetString("Email");
        }
    }
}
```

## Exception Handling

### Common Exceptions

**DataReaderException**:
```csharp
try
{
    var reader = new RowDataReader(columnNames, rows);
    reader.GetValue(0); // Before Read()
}
catch (DataReaderException ex)
{
    Console.WriteLine($"Reader error: {ex.Message}");
    // "No current row is available. Call Read()..."
}
```

**DataReaderColumnException**:
```csharp
try
{
    var reader = new RowDataReader(columnNames, rows);
    reader.Read();
    var value = reader.GetValue("NonExistent");
}
catch (DataReaderColumnException ex)
{
    Console.WriteLine($"Column error: {ex.Message}");
    Console.WriteLine($"Column name: {ex.ColumnName}");
}
```

**ArgumentException**:
```csharp
try
{
    var rows = new[]
    {
        new object[] { 1, "John" },
        new object[] { 2 } // Wrong column count!
    };
    var reader = new RowDataReader(columnNames, rows);
    reader.Read();
    reader.Read(); // Throws here
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid data: {ex.Message}");
}
```

---

## Performance Considerations

### Memory Efficiency

```csharp
// ✓ Memory efficient - streaming
using (var reader = new RowDataReader(dataSource))
{
    while (reader.Read())
    {
        ProcessRow(reader);
    }
}

// ✗ Less efficient - loads all into memory
var allRows = reader.ReadToEnd();
foreach (var row in allRows)
{
    ProcessRow(row);
}
```

### Access Speed

```csharp
// Fastest: By index
var value1 = reader.GetInt32(0); // ~1x

// Fast: By cached ordinal
var ordinal = reader.GetOrdinal("Name");
var value2 = reader.GetString(ordinal); // ~1.5x

// Slower: By name (dictionary lookup)
var value3 = reader.GetString("Name"); // ~2x
```

### Large Datasets

For large datasets:
- Use `Read()` in loops (streaming)
- Avoid `ReadToEnd()` unless necessary
- Cache column ordinals
- Use typed getters (avoid boxing)
- Process in batches if needed