# Complete Guide: Creating Support for New Databases

This guide explains in detail how to create support for new databases in Sharp-Orm through the implementation of `QueryConfig` and `Grammar`.

## Table of Contents
- [Introduction](#introduction)
- [System Architecture](#system-architecture)
- [QueryConfig - The Configurator](#queryconfig---the-configurator)
- [Grammar - The Coordinator](#grammar---the-coordinator)
- [QueryBuilder - The SQL Builder](#querybuilder---the-sql-builder)
- [Specific Grammars](#specific-grammars)
- [Complete Execution Flow](#complete-execution-flow)
- [Step-by-Step Implementation](#step-by-step-implementation)
- [Special Cases](#special-cases)

---

## Introduction

### Why does this architecture exist?

Sharp-Orm was designed to be extensible and support different SQL databases. Each database has its peculiarities:

**Syntax differences:**
- **Identifier escaping**: MySQL uses \`backticks\`, SQL Server uses `[brackets]`, PostgreSQL uses `"quotes"`
- **Pagination**: MySQL uses `LIMIT/OFFSET`, modern SQL Server uses `OFFSET/FETCH`, old SQL Server uses `ROW_NUMBER()`
- **UPSERT**: MySQL has `ON DUPLICATE KEY UPDATE`, SQL Server has `MERGE`, PostgreSQL has `ON CONFLICT`
- **Auto-increment**: MySQL returns ID with `LAST_INSERT_ID()`, SQL Server with `SCOPE_IDENTITY()`, PostgreSQL with `RETURNING id`

**Semantic differences:**
- Some databases allow `UPDATE` with `JOIN`, others don't
- Some databases support `DELETE` from multiple tables in a JOIN
- Parameter limits vary (MySQL ~65k, SQL Server ~2100)
- Some databases are case-sensitive, others aren't

The Sharp-Orm architecture separates **business logic** (what to do) from **SQL syntax** (how to do it), allowing new databases to be added without modifying the core.

---

## System Architecture

The system consists of **four main layers**:

```
┌────────────────────────────────────────────────────────────┐
│                      USER CODE                             │
│  query.Where(x => x.Id > 10).Select()                      │
└───────────────────────┬────────────────────────────────────┘
                        │
                        ↓
┌────────────────────────────────────────────────────────────┐
│                    QueryConfig                             │
│  • General settings (limits, timeouts)                     │
│  • Grammar factory                                         │
│  • Escape and naming rules                                 │
│  • Custom SQL method registration                          │
└───────────────────────┬────────────────────────────────────┘
                        │ NewGrammar()
                        ↓
┌────────────────────────────────────────────────────────────┐
│                      Grammar                               │
│  • Operation coordinator                                   │
│  • Specific grammar factory                                │
│  • Parameter interceptors                                  │
└───────────────────────┬────────────────────────────────────┘
                        │ GetSelectGrammar(), etc
                        ↓
┌────────────────────────────────────────────────────────────┐
│              Specific Grammars                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│  │SelectGrammar │  │InsertGrammar │  │UpdateGrammar │    │
│  └──────────────┘  └──────────────┘  └──────────────┘    │
│  • Implement construction logic                           │
│  • Use QueryBuilder to assemble SQL                       │
└───────────────────────┬────────────────────────────────────┘
                        │ Builder.Add()
                        ↓
┌────────────────────────────────────────────────────────────┐
│                    QueryBuilder                            │
│  • StringBuilder for SQL                                   │
│  • Parameter list                                          │
│  • Helper methods (Add, AddParameter, etc)                 │
│  • Returns SqlExpression                                   │
└────────────────────────────────────────────────────────────┘
```

### Responsibilities of each layer:

1. **QueryConfig**: "How does this database work?"
   - Defines capabilities (supports UPSERT? Supports UPDATE with JOIN?)
   - Provides escape rules
   - Creates the appropriate Grammar

2. **Grammar**: "What type of operation do I need to perform?"
   - Receives request (Select, Insert, Update, etc)
   - Instantiates the appropriate specific grammar
   - Applies parameter transformations

3. **Specific Grammars**: "How to build this specific SQL?"
   - Implements SQL construction logic
   - Handles database peculiarities
   - Uses QueryBuilder to assemble the final string

4. **QueryBuilder**: "Build the SQL string and manage parameters"
   - Concatenates SQL strings
   - Manages parameter placeholders (`?`)
   - Generates final SqlExpression

---

## QueryConfig - The Configurator

`QueryConfig` is the **abstract base class** that defines how the ORM interacts with a specific database.

### Concept

Think of `QueryConfig` as an **instruction manual** for the database. It answers questions like:

- "How do I escape a table name?"
- "How do I escape a string?"
- "How many parameters can I have in a query?"
- "Does this database support native UPSERT?"
- "How do I create a Grammar for this database?"

### Main Properties

#### 1. NativeUpsertRows

Indicates whether the database supports native UPSERT operation (INSERT with UPDATE on conflict).

```csharp
protected internal override bool NativeUpsertRows => true;
```

**MySQL**: `true` - Uses `INSERT ... ON DUPLICATE KEY UPDATE`
**SQL Server**: `true` - Uses `MERGE`
**SQLite**: `true` - Uses `INSERT OR REPLACE` / `ON CONFLICT`
**PostgreSQL**: `true` - Uses `ON CONFLICT ... DO UPDATE`

If `false`, Sharp-Orm will simulate UPSERT with separate SELECT + INSERT/UPDATE.

#### 2. CanUpdateJoin

Indicates whether the database allows using `JOIN` in `UPDATE` and `DELETE` operations.

```csharp
public override bool CanUpdateJoin => true;
```

**MySQL**: `true` - Supports `UPDATE t1 JOIN t2 ON ... SET`
**SQL Server**: `true` - Supports `UPDATE t1 FROM t1 JOIN t2`
**SQLite**: `false` - Does not support JOIN in UPDATE

If `false`, the ORM will not allow using `.Join()` in modification operations.

#### 3. DbParamsLimit

Maximum number of parameters allowed in a single query.

```csharp
public int DbParamsLimit { get; set; } = 2000;
```

**SQL Server**: ~2100 parameters
**MySQL**: ~65535 parameters
**PostgreSQL**: ~32767 parameters

The ORM uses this value to split bulk operations when necessary.

#### 4. CommandTimeout

Maximum time in seconds for command execution.

```csharp
public int CommandTimeout { get; set; } = 30;
```

### Required Abstract Methods

#### 1. ApplyNomenclature

Defines how **identifiers** (table names, columns, aliases) are escaped.

**Why is it necessary?**
SQL allows identifiers with spaces, reserved words, or special characters, but each database uses different delimiters.

```csharp
public override string ApplyNomenclature(string name)
```

**Practical examples:**

```csharp
// MySQL uses backticks
ApplyNomenclature("user name")  → `user name`
ApplyNomenclature("select")     → `select`  // reserved word

// SQL Server uses brackets
ApplyNomenclature("user name")  → [user name]
ApplyNomenclature("order")      → [order]   // reserved word

// PostgreSQL uses double quotes
ApplyNomenclature("user name")  → "user name"
ApplyNomenclature("User")       → "User"    // preserves case
```

The typical implementation uses `SanitizeSqlName`:

```csharp
public override string ApplyNomenclature(string name)
{
    return name.SanitizeSqlName('`', '`');  // MySQL
    // or
    return name.SanitizeSqlName('[', ']');  // SQL Server
    // or
    return name.SanitizeSqlName('"', '"');  // PostgreSQL
}
```

#### 2. EscapeString

Defines how **string values** are escaped when `EscapeStrings = true` (mode without parameters).

**Why is it necessary?**
When not using SQL parameters (prepared statements), it's necessary to escape strings manually to prevent SQL injection.

```csharp
public override string EscapeString(string value)
```

**Standard implementation (using single quotes):**

```csharp
public override string EscapeString(string value)
{
    return BasicEscapeString(value, '\'');
}
```

The `BasicEscapeString` method duplicates the escape character:

```csharp
// Input: O'Reilly
// Output: 'O''Reilly'
```

**MySQL requires additional escape:**

```csharp
public override string EscapeString(string value)
{
    StringBuilder build = new StringBuilder(value.Length + 2);
    build.Append('"');

    for (int i = 0; i < value.Length; i++)
    {
        var c = value[i];
        // Characters that need backslash in MySQL
        if (unsafeChars.Contains(c))
            build.Append("\\");
        build.Append(c);
    }

    return build.Append('"').ToString();
}

// unsafeChars = "\\¥Š₩∖﹨＼\"'`´ʹʺʻʼˈˊˋ˙̀́''‚′‵❛❜＇";
```

#### 3. NewGrammar

Factory method that creates the appropriate `Grammar` instance for this database.

```csharp
public override Grammar NewGrammar(Query query)
{
    return new MysqlGrammar(query);
}
```

This method is called every time a SQL operation needs to be built.

#### 4. NewTableGrammar

Factory method to create a `TableGrammar` (used for DDL - CREATE TABLE, ALTER TABLE, etc).

```csharp
public override TableGrammar NewTableGrammar(ITableSchema schema)
{
    return new MysqlTableGrammar(this, schema);
}
```

If your database doesn't support DDL operations through the ORM, throw `NotSupportedException`:

```csharp
public override TableGrammar NewTableGrammar(ITableSchema schema)
{
    throw new NotSupportedException("DDL not supported for this database");
}
```

#### 5. Clone

Creates a copy of the configuration (necessary for thread-safe operations).

```csharp
public override QueryConfig Clone(bool? safeOperations = null)
{
    var clone = new MysqlQueryConfig(
        safeOperations ?? this.OnlySafeModifications,
        Methods
    );
    this.CopyTo(clone);
    return clone;
}
```

**Important**: Use the private constructor that accepts `SqlMethodRegistry` to avoid recreating methods.

### RegisterMethods Method

Registers custom SQL methods that can be used in LINQ expressions.

```csharp
protected override void RegisterMethods()
{
    Methods.Add(new MysqlStringMethods());    // CONCAT, SUBSTRING, etc
    Methods.Add(new MysqlDateProperties());   // .Year, .Month, .Day
    Methods.Add(new MysqlDateMethods());      // DATEDIFF, DATEADD, etc
}
```

This allows writing queries like:

```csharp
query.Where(x => x.Name.ToUpper(), "JOHN");
// Generates: WHERE UPPER(`name`) = 'JOHN'

query.Where(x => x.CreatedAt.Year, 2024);
// Generates: WHERE YEAR(`created_at`) = 2024
```

### Complete Example: MySQL

```csharp
public class MysqlQueryConfig : QueryConfig
{
    // Characters that need escape in MySQL
    private static string unsafeChars = "\\¥Š₩∖﹨＼\"'`´ʹʺʻʼˈˊˋ˙̀́''‚′‵❛❜＇";

    // MySQL supports ON DUPLICATE KEY UPDATE
    protected internal override bool NativeUpsertRows => true;

    // MySQL supports UPDATE/DELETE with JOIN
    public override bool CanUpdateJoin => true;

    // Default public constructor
    public MysqlQueryConfig() { }

    // Private constructor for Clone (avoids recreating SqlMethodRegistry)
    private MysqlQueryConfig(bool safeModificationsOnly, SqlMethodRegistry methods)
        : base(safeModificationsOnly, methods)
    {
    }

    // Constructor with safety flag
    public MysqlQueryConfig(bool safeModificationsOnly)
        : base(safeModificationsOnly)
    {
    }

    // Registers MySQL SQL methods
    protected override void RegisterMethods()
    {
        Methods.Add(new MysqlStringMethods());
        Methods.Add(new MysqlDateProperties());
        Methods.Add(new MysqlDateMethods());
    }

    // MySQL uses backticks for identifiers
    public override string ApplyNomenclature(string name)
    {
        return name.SanitizeSqlName('`', '`');
    }

    // MySQL needs special escape for backslashes and quotes
    public override string EscapeString(string value)
    {
        StringBuilder build = new StringBuilder(value.Length + 2);
        build.Append('"');

        for (int i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (unsafeChars.Contains(c))
                build.Append("\\");
            build.Append(c);
        }

        return build.Append('"').ToString();
    }

    public override Grammar NewGrammar(Query query)
    {
        return new MysqlGrammar(query);
    }

    public override TableGrammar NewTableGrammar(ITableSchema schema)
    {
        return new MysqlTableGrammar(this, schema);
    }

    public override QueryConfig Clone(bool? safeOperations = null)
    {
        var clone = new MysqlQueryConfig(
            safeOperations ?? this.OnlySafeModifications,
            Methods
        );
        this.CopyTo(clone);
        return clone;
    }
}
```

---

## Grammar - The Coordinator

`Grammar` is the **abstract class** that **coordinates** the creation of different SQL types.

### Concept

If `QueryConfig` is the "database manual", `Grammar` is the **operation coordinator**. It doesn't generate SQL directly, but:

1. Receives a request (SELECT, INSERT, UPDATE, DELETE)
2. Creates the appropriate specific grammar
3. Applies parameter transformations if necessary
4. Returns the final `SqlExpression`

### Structure

```csharp
public abstract class Grammar
{
    protected Query Query { get; }
    public QueryInfo Info => Query.Info;
    protected Func<object, object> ParamInterceptor { get; set; }

    protected Grammar(Query query)
    {
        Query = query;
    }

    // Abstract methods that must return specific grammars
    protected abstract ISelectGrammar GetSelectGrammar();
    protected abstract IInsertGrammar GetInsertGrammar();
    protected abstract IUpdateGrammar GetUpdateGrammar();
    protected abstract IDeleteGrammar GetDeleteGrammar();
    protected abstract IUpsertGrammar GetUpsertGrammar();
    protected abstract IBulkInsertGrammar GetBulkInsertGrammar();

    // Public methods that coordinate generation
    public SqlExpression Select(bool configureWhereParams = true) { ... }
    public SqlExpression Insert(IEnumerable<Cell> cells, bool returnsInsetionId = true) { ... }
    public SqlExpression Update(IEnumerable<Cell> cells) { ... }
    public SqlExpression Delete() { ... }
    // ... etc
}
```

### ParamInterceptor - Parameter Transformation

The `ParamInterceptor` is a **delegate** that allows transforming values **before** they are added as SQL parameters.

**Use cases:**

1. **Timezone conversion** (DateTimeOffset to DateTime in specific timezone)
2. **Value normalization** (null handling)

**MySQL Example - DateTimeOffset Conversion:**

```csharp
public class MysqlGrammar : Grammar
{
    public MysqlGrammar(Query query) : base(query)
    {
        ParamInterceptor += (original) =>
        {
            // MySQL doesn't have native DateTimeOffset type
            // Converts to DateTime in configured timezone
            if (original is DateTimeOffset offset)
                return TimeZoneInfo.ConvertTime(
                    offset.UtcDateTime,
                    Info.Config.Translation.DbTimeZone
                );

            return original;
        };
    }
}
```

**Flow:**

```
User: query.Where(x => x.CreatedAt > DateTimeOffset.Now)
         ↓
ParamInterceptor: DateTimeOffset → DateTime (in specific timezone)
         ↓
SQL: WHERE `created_at` > @p0
Param: @p0 = DateTime(2024, 1, 15, 10, 30, 0)
```

### Coordination Methods

Each public method in `Grammar` follows a pattern:

1. Get the specific grammar
2. Configure the `ParamInterceptor` in the `QueryBuilder`
3. Call the construction method
4. Return the `SqlExpression`

**Example: Select()**

```csharp
public SqlExpression Select(bool configureWhereParams = true)
{
    // Apply default columns if necessary
    if (Query is IQueryColumnConfigurator w)
        w.ApplyDefaultSelect();

    // Use the standard BuildExpression
    return BuildExpression(GetSelectGrammar(), x => x.BuildSelect(configureWhereParams));
}

protected SqlExpression BuildExpression<T>(T grammar, Action<T> action) where T : IGrammarBase
{
    // Configure the parameter interceptor
    grammar.Builder.paramInterceptor = ParamInterceptor;

    // Execute the construction
    action(grammar);

    // Return the final expression
    return grammar.Builder.ToExpression(true, true);
}
```

### Complete Example: MySQL Grammar

```csharp
public class MysqlGrammar : Grammar
{
    public MysqlGrammar(Query query) : base(query)
    {
        // Configure interceptor for DateTimeOffset conversion
        ParamInterceptor += (original) =>
        {
            if (original is DateTimeOffset offset)
                return TimeZoneInfo.ConvertTime(
                    offset.UtcDateTime,
                    Info.Config.Translation.DbTimeZone
                );

            return original;
        };
    }

    // Returns the MySQL implementation of each grammar
    protected override ISelectGrammar GetSelectGrammar()
        => new MysqlSelectGrammar(Query);

    protected override IInsertGrammar GetInsertGrammar()
        => new MysqlInsertGrammar(Query);

    protected override IUpdateGrammar GetUpdateGrammar()
        => new MysqlUpdateGrammar(Query);

    protected override IDeleteGrammar GetDeleteGrammar()
        => new MysqlDeleteGrammar(Query);

    protected override IUpsertGrammar GetUpsertGrammar()
        => new MysqlUpsertGrammar(Query);

    protected override IBulkInsertGrammar GetBulkInsertGrammar()
        => new BulkInsertGrammar(Query);  // Default implementation
}
```

### Complete Example: SQL Server Grammar

```csharp
public class SqlServerGrammar : Grammar
{
    public SqlServerGrammar(Query query) : base(query)
    {
        // SQL Server doesn't need special ParamInterceptor
        // (supports DateTimeOffset natively)
    }

    protected override ISelectGrammar GetSelectGrammar()
        => new SqlServerSelectGrammar(Query);

    protected override IInsertGrammar GetInsertGrammar()
        => new SqlServerInsertGrammar(Query);

    protected override IUpdateGrammar GetUpdateGrammar()
        => new SqlServerUpdateGrammar(Query);

    protected override IDeleteGrammar GetDeleteGrammar()
        => new SqlServerDeleteGrammar(Query);

    protected override IUpsertGrammar GetUpsertGrammar()
        => new SqlServerUpsertGrammar(Query);

    protected override IBulkInsertGrammar GetBulkInsertGrammar()
        => new BulkInsertGrammar(Query);
}
```

---

## QueryBuilder - The SQL Builder

`QueryBuilder` is the class that **actually builds** the SQL string and manages parameters.

### Concept

Think of `QueryBuilder` as a **smart StringBuilder** for SQL:

- Concatenates query parts
- Manages placeholders (`?`) for parameters
- Substitutes direct values when possible (numbers, NULL, booleans)
- Maintains synchronized parameter list
- Generates final `SqlExpression`

### Main Components

```csharp
public class QueryBuilder
{
    // SQL string being built
    private readonly StringBuilder query = new StringBuilder();

    // Parameter list
    private readonly List<object> parameters = new List<object>();

    // Query information (config, table, etc)
    private readonly IReadonlyQueryInfo _info;

    // Function to transform parameters
    internal Func<object, object> paramInterceptor;
}
```

### Main Methods

#### 1. Add(string) - Adds literal SQL

```csharp
Builder.Add("SELECT * FROM ");
Builder.Add(tableName);
```

Used to add literal SQL without any transformation. **Use with care**, as it doesn't apply escaping or validation.

#### 2. AddTable(string | DbName) - Adds escaped table name

```csharp
// Simple form (string)
Builder.AddTable("users");
// MySQL: Generates `users`
// SQL Server: Generates [users]

// With alias
Builder.AddTable("users u");
// MySQL: Generates `users` `u`
// SQL Server: Generates [users] [u]

// Advanced form (DbName)
var tableName = new DbName("users", "u");
Builder.AddTable(tableName);
// MySQL: Generates `users` `u`

// Without alias (useful in UPDATE/INSERT)
Builder.AddTable("users u", withAlias: false);
// MySQL: Generates only `users`
```

**When to use:**
- In `FROM`, `JOIN`, `INSERT INTO`, `UPDATE`, `DELETE FROM` clauses
- Whenever you need to reference a table
- Automatically applies database nomenclature (backticks, brackets, quotes)

#### 3. AddColumn(string) - Adds escaped column name

```csharp
Builder.AddColumn("user_id");
// MySQL: Generates `user_id`
// SQL Server: Generates [user_id]

Builder.AddColumn("table.column");
// MySQL: Generates `table.column`
// Note: doesn't automatically split into parts
```

**When to use:**
- In `SELECT`, `WHERE`, `ORDER BY`, `GROUP BY` clauses
- For individual column names
- Automatically applies database nomenclature

**Difference between AddTable and AddColumn:**
- `AddTable`: For **tables**, supports alias, uses `DbName.GetName()`
- `AddColumn`: For **columns**, only escapes the name with `ApplyNomenclature()`

#### 4. AddParameter(object) - Adds a parameter

```csharp
// Adds value as parameter
Builder.AddParameter(userId);
// Generates: ? and adds userId to parameter list

// Special values are converted directly:
Builder.AddParameter(null);    // Generates: NULL (no parameter)
Builder.AddParameter(true);    // Generates: 1 (no parameter)
Builder.AddParameter(false);   // Generates: 0 (no parameter)
Builder.AddParameter(42);      // Generates: 42 (no parameter)
Builder.AddParameter("text");  // Generates: ? (with parameter)

// Collections are expanded
Builder.AddParameter(new[] {1, 2, 3});
// Generates: (?, ?, ?) (3 parameters)
```

#### 5. Add(SqlExpression) - Adds sub-expression

```csharp
var subQuery = otherQuery.ToExpression();
Builder.Add(subQuery);
// Adds the SQL and parameters of the sub-expression
```

#### 6. ToExpression() - Generates final result

```csharp
SqlExpression result = Builder.ToExpression(true, true);
// Returns: { Sql: "SELECT...", Parameters: [...] }
```

### Construction Example

```csharp
var builder = new QueryBuilder(query);

builder.Add("SELECT ");
builder.Add("*");
builder.Add(" FROM ");
builder.AddTable("users");        // ← Correction: use AddTable for tables
builder.Add(" WHERE ");
builder.AddColumn("age");
builder.Add(" > ");
builder.AddParameter(18);
builder.Add(" AND ");
builder.AddColumn("active");
builder.Add(" = ");
builder.AddParameter(true);

var expression = builder.ToExpression();

// MySQL:
// SQL: "SELECT * FROM `users` WHERE `age` > ? AND `active` = 1"
// Parameters: [18]

// SQL Server:
// SQL: "SELECT * FROM [users] WHERE [age] > ? AND [active] = 1"
// Parameters: [18]
```

### Internal Optimizations

`QueryBuilder` makes automatic optimizations:

**1. Literal values for simple types:**
```csharp
AddParameter(null)   → "NULL"      (no parameter)
AddParameter(true)   → "1"         (no parameter)
AddParameter(false)  → "0"         (no parameter)
AddParameter(42)     → "42"        (no parameter)
AddParameter("text") → "?"         (with parameter)
```

**2. String escape when `EscapeStrings = true`:**
```csharp
config.EscapeStrings = true;
AddParameter("O'Reilly")  → "'O''Reilly'"  (no parameter)
```

**3. Collection conversion:**
```csharp
AddParameter(new[] {1, 2, 3})  → "(?, ?, ?)"  (3 parameters)
```

### Difference: Direct QueryBuilder vs Inside Grammar

It's important to understand the difference between using `QueryBuilder` **directly** vs **inside a Grammar**:

**Direct Use (outside Grammar):**
```csharp
var config = new MysqlQueryConfig();
var builder = new QueryBuilder(config, new DbName("users"));

// Use AddTable and AddColumn
builder.Add("SELECT * FROM ");
builder.AddTable("users u");       // ✓ Correct
builder.Add(" WHERE ");
builder.AddColumn("age");          // ✓ Correct
builder.Add(" > ");
builder.AddParameter(18);
```

**Inside a Grammar (inherits from GrammarBase):**
```csharp
internal class MysqlSelectGrammar : MysqlGrammarBase, ISelectGrammar
{
    public void BuildSelect(bool configureWhereParams)
    {
        Builder.Add("SELECT * FROM ");

        // GetTableName() is a helper that returns already formatted string
        Builder.Add(GetTableName(true));  // ✓ Correct - returns "`users` `u`"

        // Can also use AddTable if desired
        Builder.AddTable("users u");      // ✓ Also correct

        Builder.Add(" WHERE ");

        // FixColumnName() returns already formatted string
        Builder.Add(FixColumnName("age")); // ✓ Correct - returns "`age`"

        // Can also use AddColumn
        Builder.AddColumn("age");          // ✓ Also correct

        Builder.Add(" > ");
        Builder.AddParameter(18);
    }
}
```

**Why use the helpers?**

The `GetTableName()`, `FixTableName()`, and `FixColumnName()` methods in `GrammarBase`:
- Already return formatted strings ready to use
- Have direct access to Query information
- Handle special cases (aliases, schemas, etc)
- Make the code cleaner inside grammars

The `AddTable()` and `AddColumn()` methods in `QueryBuilder`:
- Are more convenient when building SQL from scratch
- Do formatting internally
- Are necessary when you don't have GrammarBase helpers

**Summary:**
- **Inside Grammar**: Use `GetTableName()` + `Add()` **OR** `AddTable()` - both work
- **Outside Grammar**: Use `AddTable()` and `AddColumn()` - helpers are not available

---

## Specific Grammars

Each type of SQL operation has its own **Specific Grammar** that implements the construction logic.

### GrammarBase - The Base Class

All specific grammars inherit from `GrammarBase`:

```csharp
public abstract class GrammarBase : IGrammarBase
{
    // QueryBuilder to build the SQL
    protected QueryBuilder Builder { get; private set; }

    // Query being processed
    protected Query Query { get; }

    // Query information
    public QueryInfo Info => Query.Info;

    // Table information (if available)
    public TableInfo TableInfo => (Query as IWithTableInfo)?.TableInfo;
}
```

**Important Helper Methods:**

```csharp
// Applies nomenclature to table name
protected string FixTableName(string name);

// Applies nomenclature to column name
protected string FixColumnName(string name);

// Gets table name (with or without alias)
protected string GetTableName(bool withAlias);

// Writes WHERE clause
protected void WriteWhere(bool configureParameters);

// Writes ORDER BY clause
protected void ApplyOrderBy();

// Writes GROUP BY clause
protected void WriteGroupBy();

// Writes JOINs
protected void ApplyJoins();

// Writes column in SELECT
protected void WriteColumn(Column column, bool allowAlias = true);

// Writes cell in UPDATE
protected void WriteUpdateCell(Cell cell);
```

> **Note about code examples:**
>
> In the examples below, you'll see code like `Builder.Add(GetTableName(true))`. This is valid **inside classes that inherit from `GrammarBase`**, as `GetTableName()` is a helper method available in these classes.
>
> If you're using `QueryBuilder` directly (outside a grammar), use:
> - `Builder.AddTable("users")` instead of `Builder.Add(GetTableName(...))`
> - `Builder.AddColumn("name")` for columns
>
> The `GetTableName()`, `FixTableName()`, and `FixColumnName()` methods are `GrammarBase` helpers that **return already formatted strings**. That's why, in the examples below, they are used with `.Add()` and not with `.AddTable()` or `.AddColumn()`.

### SelectGrammar - Building SELECT

**Responsibilities:**
- Build SELECT clause
- Apply DISTINCT
- Apply JOINs
- Apply WHERE
- Apply GROUP BY / HAVING
- Apply ORDER BY
- Apply pagination (LIMIT/OFFSET or alternatives)

**Interface:**
```csharp
public interface ISelectGrammar
{
    void BuildSelect(bool configureWhereParams);
    void BuildCount(Column column);
}
```

**MySQL Example:**

```csharp
internal class MysqlSelectGrammar : MysqlGrammarBase, ISelectGrammar
{
    public void BuildSelect(bool configureWhereParams)
    {
        // SELECT [DISTINCT] columns
        Builder.Add("SELECT ");

        if (Query.Distinct)
            Builder.Add("DISTINCT ");

        WriteSelectColumns();

        // FROM table
        Builder.Add(" FROM ").Add(GetTableName(true));

        // [JOIN ...]
        ApplyJoins();

        // [WHERE ...]
        WriteWhere(configureWhereParams);

        // [GROUP BY ... HAVING ...]
        WriteGroupBy();

        // [ORDER BY ...]
        if (CanWriteOrderby())
            ApplyOrderBy();

        // [LIMIT ... OFFSET ...]
        WritePagination();
    }

    private void WritePagination()
    {
        if (Query.Limit is null && Query.Offset is null)
            return;

        // MySQL uses LIMIT and OFFSET
        Builder.Add(" LIMIT ").Add(Query.Limit ?? int.MaxValue);

        if (Query.Offset != null)
            Builder.Add(" OFFSET ").Add(Query.Offset);
    }

    public void BuildCount(Column column)
    {
        // If has DISTINCT, needs subquery
        bool needsSubquery = (column == null || column.IsAll()) && Query.Distinct;

        if (needsSubquery)
        {
            Builder.Add("SELECT COUNT(*) FROM (");
            BuildSelect(true);
            Builder.Add(") ").Add(FixTableName("count"));
        }
        else
        {
            Builder.Add("SELECT COUNT(");
            WriteColumn(column);
            Builder.Add(")");
            Builder.Add(" FROM ").Add(GetTableName(true));
            ApplyJoins();
            WriteWhere(true);
            WriteGroupBy();
        }
    }
}
```

**SQL Server Example (with old pagination):**

SQL Server has **two pagination modes**:
- **Modern** (≥ SQL Server 2012): `OFFSET ... ROWS FETCH NEXT ... ROWS ONLY`
- **Old** (< SQL Server 2012): `ROW_NUMBER() OVER (ORDER BY ...)`

```csharp
internal class SqlServerSelectGrammar : SqlServerGrammarBase, ISelectGrammar
{
    private Version useNewPaginationAt = new Version(11, 0);

    public void BuildSelect(bool configureWhereParams)
    {
        // Decide which mode to use
        if (HasOffset && UseOldPagination())
            WriteSelectWithOldPagination(configureWhereParams);
        else
            WriteSelectModern(configureWhereParams);
    }

    private void WriteSelectModern(bool configureWhereParams)
    {
        Builder.Add("SELECT");

        if (Query.Distinct)
            Builder.Add(" DISTINCT");

        // SQL Server allows TOP without OFFSET
        if (!HasOffset)
            AddLimit();  // SELECT TOP 10

        Builder.Add(' ');
        WriteSelectColumns();
        Builder.Add(" FROM ").Add(GetTableName(true));

        ApplyJoins();
        WriteWhere(configureWhereParams);
        WriteGroupBy();
        ApplyOrderBy();

        // OFFSET ... ROWS FETCH NEXT ... ROWS ONLY
        WritePagination();
    }

    private void WriteSelectWithOldPagination(bool configureWhereParams)
    {
        // Uses ROW_NUMBER() for pagination
        Builder.Add("SELECT * FROM (");
        Builder.Add("SELECT ROW_NUMBER() OVER(ORDER BY ");
        ApplyOrderBy(Info.Orders, true);
        Builder.Add(") AS [grammar_rownum], ");

        WriteSelectColumns();
        Builder.Add(" FROM ").Add(GetTableName(true));

        ApplyJoins();
        WriteWhere(configureWhereParams);
        WriteGroupBy();

        Builder.Add(") ").Add(TryGetTableAlias(Query));

        // WHERE [grammar_rownum] BETWEEN X AND Y
        ApplyPaginationFilter();
    }

    private void WritePagination()
    {
        if (!HasOffset)
            return;

        Builder.Add(" OFFSET ").Add(Query.Offset).Add(" ROWS");

        if (Query.Limit >= 0)
            Builder.Add(" FETCH NEXT ").Add(Query.Limit).Add(" ROWS ONLY");
    }

    private bool UseOldPagination()
    {
        // Check configuration or server version
        if (Config.UseOldPagination == null)
            return Query.Manager.GetServerVersion().Major < useNewPaginationAt.Major;

        return Config.UseOldPagination == true;
    }
}
```

### InsertGrammar - Building INSERT

**Responsibilities:**
- Build INSERT with values
- Return generated ID (auto-increment)
- Support INSERT from subquery

**Interface:**
```csharp
public interface IInsertGrammar
{
    void Build(IEnumerable<Cell> cells, bool returnsInsetionId);
    void Build(SqlExpression expression, string[] columnNames);
    void Build(QueryBase query, string[] columnNames);
}
```

**MySQL Example:**

```csharp
internal class MysqlInsertGrammar : InsertGrammar
{
    public override void Build(IEnumerable<Cell> cells, bool getGeneratedId)
    {
        // Call base implementation
        Build(cells);

        // MySQL returns last ID with LAST_INSERT_ID()
        if (getGeneratedId && Query.ReturnsInsetionId)
            Builder.Add("; SELECT LAST_INSERT_ID();");
    }
}

// Base implementation (InsertGrammar):
protected void Build(IEnumerable<Cell> cells)
{
    var cellList = cells.ToList();

    // INSERT INTO table (col1, col2, col3)
    Builder.Add("INSERT INTO ");
    Builder.Add(GetTableName(false));
    Builder.Add(" (");
    Builder.AddJoin(", ", cellList.Select(c => FixColumnName(c.Name)));
    Builder.Add(")");

    // VALUES (?, ?, ?)
    Builder.Add(" VALUES (");
    Builder.AddJoin(", ", cellList.Select(c => (object)c.Value));
    Builder.Add(")");
}
```

**SQL Server:**

```csharp
internal class SqlServerInsertGrammar : InsertGrammar
{
    public override void Build(IEnumerable<Cell> cells, bool getGeneratedId)
    {
        Build(cells);

        // SQL Server returns ID with OUTPUT or SCOPE_IDENTITY()
        if (getGeneratedId && Query.ReturnsInsetionId)
        {
            // Option 1: OUTPUT INSERTED.id
            // Option 2: ; SELECT SCOPE_IDENTITY()
            Builder.Add("; SELECT SCOPE_IDENTITY();");
        }
    }
}
```

### UpdateGrammar - Building UPDATE

**Responsibilities:**
- Build UPDATE with SET
- Support UPDATE with JOIN
- Apply WHERE

**MySQL Example:**

```csharp
internal class MysqlUpdateGrammar : MysqlGrammarBase, IUpdateGrammar
{
    public void Build(IEnumerable<Cell> cells)
    {
        var cellList = cells.ToList();

        if (cellList.Count == 0)
            throw new InvalidOperationException("No columns to update");

        // UPDATE table [alias]
        Builder.Add("UPDATE ").Add(GetTableName(false));

        if (Info.Joins.Count > 0 && !string.IsNullOrEmpty(Info.TableName.Alias))
            Builder.Add(' ').Add(FixTableName(Info.TableName.Alias));

        // [JOIN ...]
        ApplyJoins();

        // SET col1 = ?, col2 = ?
        Builder.Add(" SET ");

        using (var en = cellList.GetEnumerator())
        {
            en.MoveNext();
            WriteUpdateCell(en.Current);

            while (en.MoveNext())
            {
                Builder.Add(", ");
                WriteUpdateCell(en.Current);
            }
        }

        // WHERE ...
        WriteWhere(true);

        // MySQL allows LIMIT in UPDATE
        AddLimit();
    }
}
```

### DeleteGrammar - Building DELETE

**Responsibilities:**
- Build DELETE
- Support DELETE with JOIN
- Apply WHERE
- Support DELETE from multiple tables

**MySQL Example:**

```csharp
internal class MysqlDeleteGrammar : MysqlGrammarBase, IDeleteGrammar
{
    public void Build()
    {
        Builder.Add("DELETE");

        // If has JOIN, specify which table to delete
        if (Query.Info.Joins.Count > 0)
            Builder.Add(' ').Add(TryGetAlias(Query));

        Builder.Add(" FROM ").Add(Info.TableName.GetName(true, Info.Config));

        ApplyJoins();
        WriteWhere(true);

        if (CanWriteOrderby())
            ApplyOrderBy();

        // MySQL allows LIMIT in DELETE
        AddLimit();
    }

    public void BuildIncludingJoins(DbName[] joinNames)
    {
        // DELETE t1, t2 FROM t1 JOIN t2 ON ... WHERE ...
        Builder.Add("DELETE");

        Builder.Add(' ').Add(TryGetAlias(Query));

        foreach (var join in joinNames)
            Builder.Add(", ").Add(TryGetAlias(join));

        Builder.Add(" FROM ").Add(Info.TableName.GetName(true, Info.Config));

        ApplyJoins();
        WriteWhere(true);

        if (CanWriteOrderby())
            ApplyOrderBy();

        AddLimit();
    }
}
```

---

## Complete Execution Flow

Let's follow the flow of a query from start to finish:

### Example: SELECT with WHERE and pagination

```csharp
// 1. User code
var config = new MysqlQueryConfig();
var query = new Query("users", config)
    .Where("age", ">", 18)
    .OrderBy("name")
    .Limit(10)
    .Offset(20);

var result = query.Get();
```

**Internal flow:**

```
1. query.Get()
     ↓
2. query.Select()
     ↓
3. config.NewGrammar(query)
     → Returns: new MysqlGrammar(query)
     ↓
4. grammar.Select(configureWhereParams: true)
     ↓
5. grammar.GetSelectGrammar()
     → Returns: new MysqlSelectGrammar(query)
     ↓
6. selectGrammar.BuildSelect(true)
     ↓
7. QueryBuilder builds:
     Builder.Add("SELECT ");
     WriteSelectColumns();           // "*"
     Builder.Add(" FROM ");
     Builder.Add("`users`");
     WriteWhere(true);               // "WHERE `age` > ?"
     ApplyOrderBy();                 // "ORDER BY `name` ASC"
     WritePagination();              // "LIMIT 10 OFFSET 20"
     ↓
8. Builder.ToExpression()
     → Returns: SqlExpression {
         Sql: "SELECT * FROM `users` WHERE `age` > ? ORDER BY `name` ASC LIMIT 10 OFFSET 20",
         Parameters: [18]
     }
     ↓
9. SqlExpression is executed on database
     ↓
10. Results are mapped to objects
```

---

## Step-by-Step Implementation

Let's create support for **PostgreSQL** from scratch.

### Step 1: Create PostgresQueryConfig

```csharp
using SharpOrm.Builder;
using SharpOrm.Builder.Grammars;
using SharpOrm.SqlMethods;

namespace SharpOrm.Databases.Postgres
{
    public class PostgresQueryConfig : QueryConfig
    {
        // PostgreSQL supports ON CONFLICT
        protected internal override bool NativeUpsertRows => true;

        // PostgreSQL does NOT support UPDATE/DELETE with direct JOIN
        public override bool CanUpdateJoin => false;

        public PostgresQueryConfig() { }

        private PostgresQueryConfig(bool safeModificationsOnly, SqlMethodRegistry methods)
            : base(safeModificationsOnly, methods)
        {
        }

        protected override void RegisterMethods()
        {
            // Register PostgreSQL SQL methods
            Methods.Add(new PostgresStringMethods());
            Methods.Add(new PostgresDateProperties());
            Methods.Add(new PostgresDateMethods());
        }

        // PostgreSQL uses double quotes
        public override string ApplyNomenclature(string name)
        {
            return name.SanitizeSqlName('"', '"');
        }

        // PostgreSQL uses standard single quotes
        public override string EscapeString(string value)
        {
            return BasicEscapeString(value, '\'');
        }

        public override Grammar NewGrammar(Query query)
        {
            return new PostgresGrammar(query);
        }

        public override TableGrammar NewTableGrammar(ITableSchema schema)
        {
            return new PostgresTableGrammar(this, schema);
        }

        public override QueryConfig Clone(bool? safeOperations = null)
        {
            var clone = new PostgresQueryConfig(
                safeOperations ?? this.OnlySafeModifications,
                Methods
            );
            this.CopyTo(clone);
            return clone;
        }
    }
}
```

### Step 2: Create PostgresGrammar

```csharp
using SharpOrm.Builder.Grammars;
using SharpOrm.Builder.Grammars.Interfaces;

namespace SharpOrm.Databases.Postgres
{
    public class PostgresGrammar : Grammar
    {
        public PostgresGrammar(Query query) : base(query)
        {
            // PostgreSQL doesn't need special interceptor
        }

        protected override ISelectGrammar GetSelectGrammar()
            => new PostgresSelectGrammar(Query);

        protected override IInsertGrammar GetInsertGrammar()
            => new PostgresInsertGrammar(Query);

        protected override IUpdateGrammar GetUpdateGrammar()
            => new PostgresUpdateGrammar(Query);

        protected override IDeleteGrammar GetDeleteGrammar()
            => new PostgresDeleteGrammar(Query);

        protected override IUpsertGrammar GetUpsertGrammar()
            => new PostgresUpsertGrammar(Query);

        protected override IBulkInsertGrammar GetBulkInsertGrammar()
            => new BulkInsertGrammar(Query);
    }
}
```

### Step 3: Create PostgresSelectGrammar

```csharp
using SharpOrm.Builder.Grammars;
using SharpOrm.Builder.Grammars.Interfaces;

namespace SharpOrm.Databases.Postgres
{
    internal class PostgresSelectGrammar : GrammarBase, ISelectGrammar
    {
        public PostgresSelectGrammar(Query query) : base(query)
        {
        }

        public void BuildSelect(bool configureWhereParams)
        {
            Builder.Add("SELECT ");

            if (Query.Distinct)
                Builder.Add("DISTINCT ");

            WriteSelectColumns();

            Builder.Add(" FROM ");
            Builder.Add(GetTableName(true));

            ApplyJoins();
            WriteWhere(configureWhereParams);
            WriteGroupBy();

            if (CanWriteOrderby())
                ApplyOrderBy();

            WritePagination();
        }

        public void BuildCount(Column column)
        {
            bool needsSubquery = (column == null || column.IsAll()) && Query.Distinct;

            if (needsSubquery)
            {
                Builder.Add("SELECT COUNT(*) FROM (");
                BuildSelect(true);
                Builder.Add(") AS \"count\"");
            }
            else
            {
                Builder.Add("SELECT COUNT(");
                WriteColumn(column);
                Builder.Add(") FROM ");
                Builder.Add(GetTableName(true));
                ApplyJoins();
                WriteWhere(true);
                WriteGroupBy();
            }
        }

        private void WritePagination()
        {
            if (Query.Limit is null && Query.Offset is null)
                return;

            // PostgreSQL uses LIMIT and OFFSET like MySQL
            if (Query.Limit != null)
                Builder.Add(" LIMIT ").Add(Query.Limit);

            if (Query.Offset != null)
                Builder.Add(" OFFSET ").Add(Query.Offset);
        }
    }
}
```

### Step 4: Create PostgresInsertGrammar

```csharp
namespace SharpOrm.Databases.Postgres
{
    internal class PostgresInsertGrammar : InsertGrammar
    {
        public PostgresInsertGrammar(Query query) : base(query)
        {
        }

        public override void Build(IEnumerable<Cell> cells, bool getGeneratedId)
        {
            Build(cells);

            // PostgreSQL uses RETURNING to get generated ID
            if (getGeneratedId && Query.ReturnsInsetionId)
            {
                var pkColumn = TableInfo?.GetPrimaryKeys().FirstOrDefault();
                if (pkColumn != null)
                {
                    Builder.Add(" RETURNING ");
                    Builder.Add(FixColumnName(pkColumn.Name));
                }
            }
        }
    }
}
```

### Step 5: Create PostgresUpdateGrammar and PostgresDeleteGrammar

```csharp
namespace SharpOrm.Databases.Postgres
{
    internal class PostgresUpdateGrammar : GrammarBase, IUpdateGrammar
    {
        public PostgresUpdateGrammar(Query query) : base(query)
        {
        }

        public void Build(IEnumerable<Cell> cells)
        {
            var cellList = cells.ToList();

            if (cellList.Count == 0)
                throw new InvalidOperationException("No columns to update");

            // PostgreSQL doesn't support direct JOIN in UPDATE
            // But supports FROM with subquery
            ThrowJoinNotSupported();

            Builder.Add("UPDATE ");
            Builder.Add(GetTableName(false));
            Builder.Add(" SET ");

            using (var en = cellList.GetEnumerator())
            {
                en.MoveNext();
                WriteUpdateCell(en.Current);

                while (en.MoveNext())
                {
                    Builder.Add(", ");
                    WriteUpdateCell(en.Current);
                }
            }

            WriteWhere(true);

            // PostgreSQL doesn't support LIMIT in UPDATE
        }
    }

    internal class PostgresDeleteGrammar : GrammarBase, IDeleteGrammar
    {
        public PostgresDeleteGrammar(Query query) : base(query)
        {
        }

        public void Build()
        {
            ThrowJoinNotSupported();

            Builder.Add("DELETE FROM ");
            Builder.Add(GetTableName(false));

            WriteWhere(true);

            // PostgreSQL doesn't support LIMIT in DELETE
        }

        public void BuildIncludingJoins(DbName[] joinNames)
        {
            throw new NotSupportedException("PostgreSQL does not support DELETE with JOIN");
        }
    }
}
```

### Step 6: Use the new configuration

```csharp
var config = new PostgresQueryConfig();
var query = new Query("users", config);

var users = query
    .Where("age", ">", 18)
    .OrderBy("name")
    .Limit(10)
    .Get();

// Generates: SELECT * FROM "users" WHERE "age" > $1 ORDER BY "name" ASC LIMIT 10
// Parameters: [18]
```

---

## Special Cases

### 1. Databases with multiple pagination syntaxes

Some databases changed pagination syntax between versions (e.g., SQL Server).

**Solution**: Add configuration flag and check server version

```csharp
public class SqlServerQueryConfig : QueryConfig
{
    public bool? UseOldPagination { get; set; }
}

internal class SqlServerSelectGrammar : GrammarBase, ISelectGrammar
{
    private bool UseOldPagination()
    {
        if (Config.UseOldPagination == null)
        {
            // Detect automatically
            var version = Query.Manager.GetServerVersion();
            return version.Major < 11;  // < SQL Server 2012
        }

        return Config.UseOldPagination.Value;
    }
}
```

### 2. Databases with JOIN limitations for UPDATE/DELETE

**Solution**: Configure `CanUpdateJoin = false` and throw exception if they try to use it

```csharp
public class PostgresQueryConfig : QueryConfig
{
    public override bool CanUpdateJoin => false;
}

internal class PostgresUpdateGrammar : GrammarBase, IUpdateGrammar
{
    public void Build(IEnumerable<Cell> cells)
    {
        // Throw exception if trying to use JOIN
        ThrowJoinNotSupported();

        // ... rest of implementation
    }
}
```

### 3. Specific type conversion

Use `ParamInterceptor` to convert types before sending to database:

```csharp
public class OracleGrammar : Grammar
{
    public OracleGrammar(Query query) : base(query)
    {
        ParamInterceptor += (original) =>
        {
            // Oracle doesn't support boolean
            if (original is bool b)
                return b ? 1 : 0;

            // Oracle has limit of 4000 chars for VARCHAR2
            if (original is string s && s.Length > 4000)
                return s.Substring(0, 4000);

            return original;
        };
    }
}
```

### 4. Different UPSERT syntaxes

Each database has its own UPSERT syntax:

**MySQL**:
```sql
INSERT INTO users (id, name) VALUES (1, 'John')
ON DUPLICATE KEY UPDATE name = VALUES(name)
```

**PostgreSQL**:
```sql
INSERT INTO users (id, name) VALUES (1, 'John')
ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name
```

**SQL Server**:
```sql
MERGE INTO users AS target
USING (SELECT 1 AS id, 'John' AS name) AS source
ON target.id = source.id
WHEN MATCHED THEN UPDATE SET name = source.name
WHEN NOT MATCHED THEN INSERT (id, name) VALUES (source.id, source.name);
```

Implement `IUpsertGrammar` for each specific syntax.

---

## Implementation Checklist

When creating support for a new database, follow this list:

### QueryConfig

- [ ] Create class inheriting from `QueryConfig`
- [ ] Define `NativeUpsertRows` (supports UPSERT?)
- [ ] Define `CanUpdateJoin` (supports UPDATE/DELETE with JOIN?)
- [ ] Implement `ApplyNomenclature()` with correct delimiters
- [ ] Implement `EscapeString()` with appropriate escape
- [ ] Implement `NewGrammar()` returning your grammar
- [ ] Implement `NewTableGrammar()` or throw `NotSupportedException`
- [ ] Implement `Clone()` using private constructor
- [ ] Implement `RegisterMethods()` with database SQL methods
- [ ] Adjust `DbParamsLimit` if necessary

### Grammar

- [ ] Create class inheriting from `Grammar`
- [ ] Add `ParamInterceptor` if necessary (type conversion)
- [ ] Implement all `Get*Grammar()` methods
- [ ] Test that it returns correct grammars

### SelectGrammar

- [ ] Implement `BuildSelect()`
- [ ] Implement `BuildCount()`
- [ ] Implement pagination (LIMIT/OFFSET or alternative)
- [ ] Test DISTINCT
- [ ] Test JOIN
- [ ] Test GROUP BY / HAVING
- [ ] Test ORDER BY
- [ ] Test pagination in different scenarios

### InsertGrammar

- [ ] Inherit from `InsertGrammar` or implement `IInsertGrammar`
- [ ] Implement `Build()` to return generated ID
- [ ] Test simple INSERT
- [ ] Test INSERT with ID return
- [ ] Test INSERT from subquery

### UpdateGrammar

- [ ] Implement `Build()`
- [ ] Handle JOIN (if supported) or throw exception
- [ ] Test simple UPDATE
- [ ] Test UPDATE with WHERE
- [ ] Test UPDATE with JOIN (if supported)

### DeleteGrammar

- [ ] Implement `Build()`
- [ ] Implement `BuildIncludingJoins()` or throw exception
- [ ] Test simple DELETE
- [ ] Test DELETE with WHERE
- [ ] Test DELETE with JOIN (if supported)

### UpsertGrammar

- [ ] Implement specific UPSERT syntax
- [ ] Test UPSERT with different keys
- [ ] Test batch UPSERT

### General Tests

- [ ] Test all CRUD operations
- [ ] Test with different data types
- [ ] Test with NULL values
- [ ] Test with collections
- [ ] Test pagination with different limits
- [ ] Test batch operations
- [ ] Test transactions
- [ ] Test edge cases (empty strings, maximum values, etc)

---

## Useful Resources

### Base Classes

- **`QueryConfig`**: Abstract base configuration
- **`Grammar`**: Abstract base grammar
- **`GrammarBase`**: Base for specific grammars with helpers
- **`InsertGrammar`**: Base INSERT implementation
- **`BulkInsertGrammar`**: Default batch INSERT implementation

### Interfaces

- **`ISelectGrammar`**: Interface for SELECT
- **`IInsertGrammar`**: Interface for INSERT
- **`IUpdateGrammar`**: Interface for UPDATE
- **`IDeleteGrammar`**: Interface for DELETE
- **`IUpsertGrammar`**: Interface for UPSERT
- **`IBulkInsertGrammar`**: Interface for batch INSERT

### Utilities

- **`QueryBuilder`**: SQL string builder
- **`SqlExpression`**: Container for SQL + parameters
- **`Column`**: Represents columns
- **`DbName`**: Represents table/database names with alias support
- **`Cell`**: Name/value pair for INSERT/UPDATE
- **`Row`**: Collection of cells (row)

### QueryBuilder Methods

- `Add(string)`: Adds literal SQL without transformation
- `AddTable(string | DbName, bool withAlias = true)`: Adds table with nomenclature and alias
- `AddColumn(string)`: Adds column with nomenclature
- `AddParameter(object)`: Adds parameter or direct value
- `Add(SqlExpression)`: Adds sub-expression with its parameters
- `AddJoin(string, IEnumerable)`: Joins values with separator
- `ToExpression()`: Generates final SqlExpression

### Helper Methods in GrammarBase

- `FixTableName(string)`: Applies nomenclature to table (returns string)
- `FixColumnName(string)`: Applies nomenclature to column (returns string)
- `GetTableName(bool withAlias)`: Gets table name with/without alias (returns string)
- `WriteWhere(bool)`: Writes WHERE clause to Builder
- `ApplyOrderBy()`: Writes ORDER BY clause to Builder
- `WriteGroupBy()`: Writes GROUP BY clause to Builder
- `ApplyJoins()`: Writes JOIN clauses to Builder
- `WriteColumn(Column)`: Writes column to Builder
- `WriteUpdateCell(Cell)`: Writes cell in UPDATE (col = ?)
- `ThrowJoinNotSupported()`: Throws exception if has JOIN
- `ThrowOffsetNotSupported()`: Throws exception if has OFFSET
- `ThrowLimitNotSupported()`: Throws exception if has LIMIT

---

Use the **MySQL** and **SQL Server** implementations as reference - they cover most cases (simple and complex databases, with and without limitations).
