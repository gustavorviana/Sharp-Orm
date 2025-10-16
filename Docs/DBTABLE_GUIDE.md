# DbTable Guide: Managing Database Tables

This guide explains how to use the `DbTable` class to create, manage, and interact with database tables in Sharp-Orm.

## Table of Contents
- [Introduction](#introduction)
- [Core Concepts](#core-concepts)
- [Creating Tables](#creating-tables)
- [Opening Existing Tables](#opening-existing-tables)
- [Table Operations](#table-operations)
- [Column Inspection](#column-inspection)
- [Lifecycle Management](#lifecycle-management)
- [Best Practices](#best-practices)
- [Common Patterns](#common-patterns)

---

## Introduction

### What is DbTable?

`DbTable` is a high-level abstraction for working with database tables in Sharp-Orm. It provides:

- **Table Creation**: Create permanent or temporary tables using `TableBuilder`
- **Automatic Lifecycle Management**: Temporary tables are automatically dropped on disposal
- **Query Integration**: Built-in query generation for table operations
- **Data Operations**: Support for insertions (single and bulk)
- **Metadata Access**: Retrieve column information and structure
- **Async Support**: Full asynchronous operation support

### Key Features

```
┌───────────────────────────────────────────────────────┐
│                      DbTable                          │
│                                                       │
│       ┌────────────────┐  ┌────────────────┐          │
│       │  Temporary     │  │   Permanent    │          │
│       │   Tables       │  │    Tables      │          │
│       └────────────────┘  └────────────────┘          │
│                                                       │
│  • Automatic cleanup of temporary tables              │
│  • Built-in query generation                          │
│  • Column metadata inspection                         │
│  • Bulk and single row insertions                     │
│  • Connection management                              │
│  • Asynchronous operations                            │
└───────────────────────────────────────────────────────┘
```

---

## Core Concepts

### Table Types

**Permanent Tables**:
- Created in the database permanently
- Survive beyond the application lifecycle
- Require explicit Drop() to remove
- Can be reopened in different sessions

**Temporary Tables**:
- Exist only for the duration of the connection
- Automatically dropped when DbTable is disposed
- Useful for intermediate calculations
- Require specific connection management settings

### Connection Management

When working with temporary tables, the `ConnectionManager` must use appropriate settings:

```csharp
// Correct management settings for temporary tables
var manager = new ConnectionManager
{
    Management = ConnectionManagement.LeaveOpen
    // or ConnectionManagement.CloseOnManagerDispose
    // or ConnectionManagement.DisposeOnManagerDispose
};

var schema = new TableBuilder("temp_table", temporary: true)
    .AddColumn<int>("id")
    .GetSchema();

using (var table = DbTable.Create(schema, manager))
{
    // Work with temporary table
} // Table automatically dropped here
```

**Invalid settings for temporary tables**:
- `ConnectionManagement.CloseOnDispose` - Connection closes too early
- `ConnectionManagement.AutoClose` - Connection closes prematurely

### Disposal Behavior

```csharp
using (var table = DbTable.Create(schema, manager))
{
    // Table exists and is accessible
    table.Insert(data);

} // Disposal happens here:
  // 1. If temporary: DROP TABLE executed
  // 2. If local manager: Manager.Dispose() called
  // 3. Connection cleanup (if configured)
```

---

## Creating Tables

### Method 1: Using TableBuilder (Recommended)

The modern and flexible approach using the fluent API:

```csharp
// Build table schema
var schema = new TableBuilder("users", temporary: false)
    .AddColumn<int>("id")
    .AddColumn<string>("name")
    .AddColumn<int>("age")
    .HasKey("id")  // Set primary key
    .GetSchema();

// Create the table
using (var table = DbTable.Create(schema, manager))
{
    // Table is created and ready to use
}
```

### Method 2: TableBuilder with Fluent Column Configuration

Configure columns with detailed options:

```csharp
var schema = new TableBuilder("products", temporary: false)
    .AddColumn<int>("id").IsPrimaryKey().AutoIncrement()
    .AddColumn<string>("name").HasMaxLength(100).IsRequired()
    .AddColumn<decimal>("price").HasPrecision(10, 2).IsRequired()
    .AddColumn<DateTime>("created_at").HasDefaultValue("CURRENT_TIMESTAMP")
    .HasIndex("name")  // Add index on name column
    .GetSchema();

using (var table = DbTable.Create(schema, manager))
{
    // Table created with all configurations
}
```

### Method 3: Based on Query Result

Create a table structure from a query:

```csharp
var baseQuery = new Query("source_table", manager)
    .Select("id", "name", "created_at")
    .Where("active", true);

var schema = new TableBuilder("temp_active_users", temporary: true)
    .SetBasedQuery(baseQuery)
    .GetSchema();

using (var table = DbTable.Create(schema, manager))
{
    // Table created with same structure as query result
}
```

### Method 4: Based on Existing Table

Create table with columns from another table:

```csharp
// Copy specific columns from existing table
var schema = new TableBuilder("user_backup", temporary: false)
    .SetBasedTable("users", "id", "name", "email")
    .GetSchema();

using (var table = DbTable.Create(schema, manager))
{
    // Table created with selected columns from 'users'
}
```

### Method 5: Strongly-Typed with Generic TableBuilder

Use type-safe expressions with entity classes:

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

var registry = TranslationRegistry.Default;
var schema = new TableBuilder<User>(registry, temporary: false)
    .HasKey(x => x.Id)
    .HasUnique(x => x.Email)
    .HasIndex(x => x.Name)
    .GetSchema();

using (var table = DbTable.Create(schema, manager))
{
    // Table created based on User class with constraints
}
```

### Method 6: Ignoring or Selecting Specific Columns

```csharp
// Ignore specific columns
var schema = new TableBuilder<User>(registry, temporary: true)
    .Ignore(x => x.CreatedAt)  // Exclude CreatedAt column
    .GetSchema();

// Select only specific columns
var schema2 = new TableBuilder<User>(registry, temporary: true)
    .Only(x => new { x.Id, x.Name })  // Include only Id and Name
    .GetSchema();
```

### Connection Manager Handling

```csharp
var manager = new ConnectionManager();
var schema = new TableBuilder("users", temporary: false)
    .AddColumn<int>("id")
    .GetSchema();

using (var table = DbTable.Create(schema, manager))
{
    // Table uses your manager
} // Table disposed, manager still alive

manager.Dispose(); // You dispose when ready
```

---

## Opening Existing Tables

Open a permanent table that already exists in the database:

```csharp
// Open existing permanent table (returns null if not exists)
var table = DbTable.OpenIfExists("users", manager);
if (table != null)
{
    using (table)
    {
        var query = table.GetQuery();
        var users = query.Get();
    }
}

// Alternative: check existence first
if (DbTable.Exists("users", isTemp: false, manager))
{
    var table = DbTable.OpenIfExists("users", manager);
    using (table)
    {
        // Safe to use
    }
}
```

**Important**:
- Only works with permanent tables (not temporary)
- Returns `null` if table doesn't exist
- Validates existence before opening

### Checking Table Existence

```csharp
// Static method - check before opening
if (DbTable.Exists("users", isTemp: false, manager))
{
    var table = DbTable.OpenIfExists("users", manager);
    using (table)
    {
        // Safe to use
    }
}

// Async version
if (await DbTable.ExistsAsync("users", isTemp: false, manager))
{
    var table = DbTable.OpenIfExists("users", manager);
    using (table)
    {
        // Safe to use
    }
}

// Instance method - check after opening
var table = DbTable.OpenIfExists("users", manager);
if (table != null)
{
    using (table)
    {
        if (table.Exists())
        {
            // Table confirmed to exist
        }
    }
}
```

---

## Table Operations

### Querying Data

```csharp
var schema = new TableBuilder("users", temporary: false)
    .AddColumn<int>("id")
    .AddColumn<string>("name")
    .AddColumn<int>("age")
    .GetSchema();

using (var table = DbTable.Create(schema, manager))
{
    // Get untyped query
    var query = table.GetQuery();
    query.Where("age", ">", 18);
    var rows = query.Get();

    // Get typed query
    var typedQuery = table.GetQuery<User>();
    var users = typedQuery.Where(u => u.Age > 18).Get();
}
```

**Column Selection**:
- If table was created from a query, columns are pre-configured
- If table was created from TableBuilder, all columns are selected by default

### Inserting Data

#### Single Row Insert

```csharp
var schema = new TableBuilder("users", temporary: false)
    .AddColumn<int>("id")
    .AddColumn<string>("name")
    .AddColumn<int>("age")
    .GetSchema();

using (var table = DbTable.Create(schema, manager))
{
    // Insert anonymous object
    table.Insert(new { Id = 1, Name = "John", Age = 30 });

    // Insert typed object
    var user = new User { Id = 2, Name = "Jane", Age = 25 };
    table.Insert(user);
}
```

#### Bulk Insert

```csharp
var schema = new TableBuilder("users", temporary: false)
    .AddColumn<int>("id")
    .AddColumn<string>("name")
    .AddColumn<int>("age")
    .GetSchema();

using (var table = DbTable.Create(schema, manager))
{
    var users = new List<User>
    {
        new User { Id = 1, Name = "John", Age = 30 },
        new User { Id = 2, Name = "Jane", Age = 25 },
        new User { Id = 3, Name = "Bob", Age = 35 }
    };

    // Bulk insert - more efficient for large datasets
    table.BulkInsert(users);
}
```

**Performance Considerations**:
- Use `BulkInsert()` for multiple rows (>10-20 rows)
- Use `Insert()` for single rows
- BulkInsert automatically batches based on database parameter limits

### Truncating Table

Remove all rows while keeping table structure:

```csharp
var table = DbTable.OpenIfExists("users", manager);
if (table != null)
{
    using (table)
    {
        // Synchronous
        table.Truncate();

        // Asynchronous
        await table.TruncateAsync(cancellationToken);
    }
}
```

**Truncate vs Delete**:
- TRUNCATE is faster (no row-by-row deletion)
- TRUNCATE resets auto-increment counters
- TRUNCATE cannot be used with WHERE clause
- TRUNCATE may not be recoverable in transaction (depends on database)

### Dropping Table

Permanently remove the table from database:

```csharp
var table = DbTable.OpenIfExists("temp_users", manager);
if (table != null)
{
    using (table)
    {
        // Synchronous drop
        table.Drop();

        // Asynchronous drop
        await table.DropAsync(cancellationToken);
    }
}
```

**Important**:
- For temporary tables: automatically called on Dispose if not manually dropped
- For permanent tables: must be called explicitly
- After Drop(), the table object should not be used

---

## Column Inspection

Retrieve metadata about table columns:

### Synchronous Column Inspection

```csharp
var table = DbTable.OpenIfExists("users", manager);
if (table != null)
{
    using (table)
    {
        var columns = table.GetColumns();

        foreach (var column in columns)
        {
            Console.WriteLine($"Column: {column.ColumnName}");
            Console.WriteLine($"  Type: {column.ColumnType}");
            Console.WriteLine($"  Nullable: {column.Nullable}");
            Console.WriteLine($"  Max Length: {column.MaxLength}");
            Console.WriteLine($"  Default: {column.DefaultValue}");
        }
    }
}
```

### Asynchronous Column Inspection

```csharp
var table = DbTable.OpenIfExists("users", manager);
if (table != null)
{
    using (table)
    {
        var columns = await table.GetColumnsAsync(cancellationToken);

        // Process columns...
    }
}
```

### DbColumnInfo Properties

```csharp
public class DbColumnInfo : IDbColumnInfo
{
    public string ColumnName { get; set; }      // Column name
    public string ColumnType { get; set; }      // Database-specific type (e.g., "VARCHAR", "INT")
    public bool Nullable { get; set; }          // Allows NULL values
    public int? MaxLength { get; set; }         // Maximum length (for strings)
    public int? NumericPrecision { get; set; }  // Precision (for decimals)
    public int? NumericScale { get; set; }      // Scale (for decimals)
    public string DefaultValue { get; set; }    // Default value expression
}
```

### Database Support

**Supported Databases**:
- MySQL (via `MysqlColumnInspector`)
- SQL Server (via `SqlServerColumnInspector`)
- SQLite (via `SqliteColumnInspector`)

**Unsupported Databases**:
```csharp
try
{
    var columns = table.GetColumns();
}
catch (NotSupportedException ex)
{
    // Thrown when database doesn't support column inspection
    Console.WriteLine($"Column inspection not available: {ex.Message}");
}
```

---

## Lifecycle Management

### Temporary Table Lifecycle

```csharp
// Full lifecycle example
var manager = new ConnectionManager
{
    Management = ConnectionManagement.LeaveOpen
};

var schema = new TableBuilder("temp_data", temporary: true)
    .AddColumn<int>("id")
    .AddColumn<string>("data")
    .GetSchema();

using (var table = DbTable.Create(schema, manager))
{
    // 1. Table created in database

    // 2. Populate table
    table.Insert(new { Id = 1, Data = "test" });

    // 3. Query table
    var results = table.GetQuery().Get();

    // 4. Explicit drop (optional)
    table.Drop();

} // 5. Automatic cleanup:
  //    - If not dropped: DROP TABLE executed
  //    - If local manager: Manager disposed
```

### Permanent Table Lifecycle

```csharp
// Create permanent table
var schema = new TableBuilder("users", temporary: false)
    .AddColumn<int>("id")
    .AddColumn<string>("name")
    .GetSchema();

using (var table = DbTable.Create(schema, manager))
{
    // Table created and persists in database
    table.Insert(new { Id = 1, Name = "John" });

} // Table NOT dropped on disposal (permanent table)

// Later, in another session
var table = DbTable.OpenIfExists("users", manager);
if (table != null)
{
    using (table)
    {
        // Table still exists and can be accessed
        var users = table.GetQuery().Get();
    }
}
```

### Finalizer Behavior

```csharp
// Even if Dispose() not called, finalizer ensures cleanup
var table = DbTable.Create(schema, manager);
// ... forgot to dispose

// Finalizer will:
// 1. Drop temporary table (if not already dropped)
// 2. NOT dispose manager (only in Dispose(true))
```

**Best Practice**: Always use `using` statement to ensure proper disposal:
```csharp
// ✓ Correct
using (var table = DbTable.Create(schema, manager))
{
    // Work with table
}

// ✗ Avoid
var table = DbTable.Create(schema, manager);
// ... work with table
// Forgetting to dispose
```

---

## Best Practices

### 1. Use `using` Statement

Always wrap `DbTable` in a `using` statement:

```csharp
// ✓ Correct - ensures proper cleanup
using (var table = DbTable.Create(schema, manager))
{
    // Work with table
}

// ✗ Avoid - manual disposal is error-prone
var table = DbTable.Create(schema, manager);
try
{
    // Work with table
}
finally
{
    table.Dispose();
}
```

### 2. Choose Appropriate Connection Management

```csharp
// For temporary tables
var manager = new ConnectionManager
{
    Management = ConnectionManagement.LeaveOpen // ✓ Correct
};

// ✗ Avoid for temporary tables
var manager = new ConnectionManager
{
    Management = ConnectionManagement.CloseOnDispose // Will cause errors
};
```

### 3. Use TableBuilder for All Table Creation

```csharp
// ✓ Recommended - use TableBuilder
var schema = new TableBuilder("users", temporary: false)
    .AddColumn<int>("id").IsPrimaryKey().AutoIncrement()
    .AddColumn<string>("name").HasMaxLength(100).IsRequired()
    .GetSchema();

var table = DbTable.Create(schema, manager);
```

### 4. Handle Exceptions Properly

```csharp
var table = DbTable.OpenIfExists("users", manager);
if (table == null)
{
    Console.WriteLine("Table not found");
    return;
}

try
{
    using (table)
    {
        // Table exists, work with it
        var columns = table.GetColumns();
    }
}
catch (NotSupportedException ex)
{
    // Operation not supported by database
    Console.WriteLine($"Not supported: {ex.Message}");
}
```

### 5. Bulk Operations for Performance

```csharp
var users = new List<User>();
for (int i = 0; i < 1000; i++)
{
    users.Add(new User { Id = i, Name = $"User{i}" });
}

var schema = new TableBuilder("users", temporary: false)
    .AddColumn<int>("id")
    .AddColumn<string>("name")
    .GetSchema();

using (var table = DbTable.Create(schema, manager))
{
    // ✓ Efficient - single bulk operation
    table.BulkInsert(users);

    // ✗ Inefficient - 1000 separate operations
    foreach (var user in users)
    {
        table.Insert(user);
    }
}
```

### 6. Check Table Existence

```csharp
// ✓ Use OpenIfExists (returns null if not exists)
var table = DbTable.OpenIfExists("users", manager);
if (table != null)
{
    using (table)
    {
        // Safe to use
    }
}

// ✓ Alternative: Check before opening
if (DbTable.Exists("users", false, manager))
{
    var table = DbTable.OpenIfExists("users", manager);
    using (table)
    {
        // Safe to use
    }
}
```

---

## Common Patterns

### Pattern 1: Temporary Processing Table

```csharp
public List<ProcessedData> ProcessLargeDataset(List<RawData> rawData, ConnectionManager manager)
{
    // Create temporary table for processing
    var schema = new TableBuilder("temp_processing", temporary: true)
        .AddColumn<int>("id")
        .AddColumn<string>("processed_value")
        .GetSchema();

    using (var table = DbTable.Create(schema, manager))
    {
        // 1. Bulk insert raw data
        table.BulkInsert(rawData);

        // 2. Process using SQL queries
        var query = table.GetQuery();
        // ... complex processing

        // 3. Return results
        return query.Get<ProcessedData>();

    } // Temporary table automatically dropped
}
```

### Pattern 2: Table Existence Check and Create

```csharp
public async Task<DbTable> GetOrCreateTable(string tableName, ConnectionManager manager)
{
    // Try to open existing table
    var table = DbTable.OpenIfExists(tableName, manager);
    if (table != null)
    {
        return table;
    }

    // Table doesn't exist, create it
    var schema = new TableBuilder(tableName, temporary: false)
        .AddColumn<int>("id").IsPrimaryKey()
        .AddColumn<string>("name")
        .GetSchema();

    return DbTable.Create(schema, manager);
}
```

### Pattern 3: Safe Temporary Table with Transaction

```csharp
public async Task ProcessWithTemporaryTable(ConnectionManager manager)
{
    // Ensure connection stays open for temporary table
    manager.Management = ConnectionManagement.LeaveOpen;

    using (var transaction = manager.BeginTransaction())
    {
        try
        {
            var schema = new TableBuilder("temp_data", temporary: true)
                .AddColumn<int>("id")
                .AddColumn<string>("value")
                .GetSchema();

            using (var table = DbTable.Create(schema, manager))
            {
                // Process data in temporary table
                table.BulkInsert(data);

                // Complex operations
                var results = table.GetQuery().Where(...).Get();

                // Commit if successful
                transaction.Commit();
            }
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
```

### Pattern 4: Column Metadata Inspection

```csharp
public void AnalyzeTableStructure(string tableName, ConnectionManager manager)
{
    var table = DbTable.OpenIfExists(tableName, manager);
    if (table == null)
    {
        Console.WriteLine($"Table {tableName} not found");
        return;
    }

    using (table)
    {
        var columns = table.GetColumns();

        // Find nullable columns
        var nullableColumns = columns.Where(c => c.Nullable);

        // Find string columns with their max lengths
        var stringColumns = columns
            .Where(c => c.ColumnType.Contains("VARCHAR") || c.ColumnType.Contains("TEXT"))
            .Select(c => new { c.ColumnName, c.MaxLength });

        // Analyze and report
        Console.WriteLine($"Table: {tableName}");
        Console.WriteLine($"Total Columns: {columns.Length}");
        Console.WriteLine($"Nullable Columns: {nullableColumns.Count()}");
    }
}
```

### Pattern 5: Data Migration with Temporary Table

```csharp
public async Task MigrateData(string sourceTable, string targetTable, ConnectionManager manager)
{
    // Create temporary table for transformation
    var sourceQuery = new Query(sourceTable, manager);

    var schema = new TableBuilder("temp_migration", temporary: true)
        .SetBasedQuery(sourceQuery)
        .GetSchema();

    using (var tempTable = DbTable.Create(schema, manager))
    {
        // 1. Transform data in temporary table
        var transformQuery = tempTable.GetQuery();
        // ... apply transformations

        // 2. Get transformed data
        var transformedData = await transformQuery.GetAsync();

        // 3. Insert into target table
        var targetTableObj = DbTable.OpenIfExists(targetTable, manager);
        if (targetTableObj != null)
        {
            using (targetTableObj)
            {
                targetTableObj.BulkInsert(transformedData);
            }
        }

    } // Temporary table cleaned up automatically
}
```

### Pattern 6: Table Factory Pattern

```csharp
public class TableFactory
{
    private readonly ConnectionManager _manager;

    public TableFactory(ConnectionManager manager)
    {
        _manager = manager;
    }

    public DbTable CreateUserTable(bool temporary = false)
    {
        var schema = new TableBuilder("users", temporary)
            .AddColumn<int>("id").IsPrimaryKey().AutoIncrement()
            .AddColumn<string>("username").HasMaxLength(50).IsRequired()
            .AddColumn<string>("email").HasMaxLength(100)
            .AddColumn<DateTime>("created_at")
            .GetSchema();

        return DbTable.Create(schema, _manager);
    }

    public DbTable CreateOrderTable(bool temporary = false)
    {
        var schema = new TableBuilder("orders", temporary)
            .AddColumn<int>("id").IsPrimaryKey().AutoIncrement()
            .AddColumn<int>("user_id")
            .AddColumn<decimal>("total").HasPrecision(10, 2)
            .HasForeignKey("user_id", "users", "id")
            .GetSchema();

        return DbTable.Create(schema, _manager);
    }
}

// Usage
var factory = new TableFactory(manager);

using (var userTable = factory.CreateUserTable(temporary: true))
{
    // Work with temporary user table
}
```

### Pattern 7: Strongly-Typed Table Creation

```csharp
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

public DbTable CreateTypedUserTable(ConnectionManager manager, bool temporary = false)
{
    var registry = TranslationRegistry.Default;

    var schema = new TableBuilder<User>(registry, temporary)
        .HasKey(x => x.Id)
        .HasUnique(x => x.Email, "UQ_User_Email")
        .HasIndex(x => x.Username)
        .Column(x => x.Username).HasMaxLength(50).IsRequired()
        .GetSchema();

    return DbTable.Create(schema, manager);
}
```