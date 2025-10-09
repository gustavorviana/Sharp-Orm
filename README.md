# Sharp-ORM

A lightweight and intuitive ORM library for .NET that simplifies database operations.

[![.NET](https://img.shields.io/badge/.NET-5C2D91?style=flat&logo=.net&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-239120?style=flat&logo=c-sharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![Downloads](https://img.shields.io/nuget/dt/SharpOrm.svg)](https://www.nuget.org/packages/SharpOrm/)

## Features

- ✅ Multi-Database Support (SQL Server, MySQL, SQLite)
- ✅ Type-Safe Queries with Lambda Expressions
- ✅ Fluent API and LINQ-like Syntax
- ✅ Built-in Pagination Support
- ✅ Foreign Key Relationships
- ✅ Soft Deletes
- ✅ Bulk Operations
- ✅ Full Async/Await Support
- ✅ CASE Expressions

## Installation

```bash
dotnet add package SharpOrm
```

## Quick Start

### 1. Define Your Model

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Users")]
public class User
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public bool Active { get; set; }
}
```

### 2. Configure Connection

```csharp
using SharpOrm.Builder;
using SharpOrm.Connection;

// SQL Server
ConnectionCreator.Default = new SingleConnectionCreator(
    new SqlServerQueryConfig(),
    connectionString
);

// MySQL
ConnectionCreator.Default = new SingleConnectionCreator<MySql.Data.MySqlClient.MySqlConnection>(
    new MysqlQueryConfig(),
    connectionString
);

// SQLite
ConnectionCreator.Default = new SingleConnectionCreator<System.Data.SQLite.SQLiteConnection>(
    new SqliteQueryConfig(),
    connectionString
);
```

**💡 Recommended:** Use `ConnectionManager.GetQuery<T>()` for better resource management:

```csharp
using (var manager = new ConnectionManager(new SqlServerQueryConfig(), connection))
{
    using (var query = manager.GetQuery<User>())
    {
        var users = query.Get();
    }
}
```

### 3. Query Data

```csharp
using SharpOrm;

// Get all active users
using (var query = new Query<User>())
{
    var users = query
        .Where(u => u.Active, true)
        .OrderBy(u => u.Name)
        .Get();
}

// Find by ID
using (var query = new Query<User>())
{
    var user = query.Find(1);
}

// Complex query
using (var query = new Query<User>())
{
    var result = query
        .Select(u => new { u.Id, u.Name, u.Email })
        .Where(u => u.Active, true)
        .WhereNotNull(u => u.Email)
        .OrderBy(u => u.Name)
        .Skip(10)
        .Take(20)
        .Get();
}
```

### 4. Insert Data

```csharp
using (var query = new Query<User>())
{
    // Single insert
    query.Insert(new User
    {
        Name = "John Doe",
        Email = "john@example.com",
        Active = true
    });

    // Bulk insert
    query.BulkInsert(
        new User { Name = "User 1", Email = "user1@example.com" },
        new User { Name = "User 2", Email = "user2@example.com" }
    );
}
```

### 5. Update Data

```csharp
using (var query = new Query<User>())
{
    query
        .Where(u => u.Id, 1)
        .Update(new { Active = false });
}
```

### 6. Delete Data

```csharp
using (var query = new Query<User>())
{
    query.Where(u => u.Active, false).Delete();
}
```

## Core Features

### Pagination

```csharp
// Simple pagination with Skip/Take
using (var query = new Query<User>())
{
    var users = query
        .OrderBy(u => u.Id)
        .Skip(20)
        .Take(20)
        .Get();
}

// Advanced pagination with metadata
using (var query = new Query<User>())
{
    using (var pager = query.Paginate(peerPage: 10, currentPage: 1))
    {
        Console.WriteLine($"Total: {pager.Total}, Pages: {pager.Pages}");

        foreach (var user in pager)
        {
            Console.WriteLine(user.Name);
        }

        // Navigate to next page
        pager.GoToPage(2);
    }
}
```

📖 See [PAGINATION.md](PAGINATION.md) for complete documentation.

### Foreign Keys

```csharp
[Table("Orders")]
public class Order
{
    [Key]
    public int Id { get; set; }

    [Column("customer_id")]
    public int CustomerId { get; set; }

    public Customer Customer { get; set; }
}

[Table("Customers")]
public class Customer
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; }

    [Column("address_id")]
    public int? AddressId { get; set; }

    public Address Address { get; set; }
}

// Load single relationship
using (var query = new Query<Order>())
{
    var orders = query
        .Include(o => o.Customer)
        .Get();

    Console.WriteLine(orders[0].Customer.Name);
}

// Load nested relationships
using (var query = new Query<Order>())
{
    var orders = query
        .Include(o => o.Customer)
        .ThenInclude(c => c.Address)
        .Get();

    Console.WriteLine(orders[0].Customer.Address.Street);
}

// Load collections
using (var query = new Query<Customer>())
{
    var customers = query
        .Include(c => c.Orders)
        .ThenInclude(o => o.Product)
        .Get();

    foreach (var order in customers[0].Orders)
    {
        Console.WriteLine(order.Product.Name);
    }
}
```

### Soft Deletes

```csharp
using (var query = new Query<User>())
{
    // Exclude soft deleted (default)
    var activeUsers = query.Get();

    // Include soft deleted
    var allUsers = query.WithTrashed().Get();

    // Only soft deleted
    var deletedUsers = query.OnlyTrashed().Get();

    // Soft delete
    query.Where(u => u.Id, 1).SoftDelete();

    // Restore
    query.Where(u => u.Id, 1).Restore();
}
```

### Async Operations

```csharp
using (var query = new Query<User>())
{
    var users = await query
        .Where(u => u.Active, true)
        .GetAsync();

    await query.InsertAsync(new User { Name = "Async User" });

    await query.Where(u => u.Id, 1).UpdateAsync(new { Active = false });

    var count = await query.CountAsync();
}
```

### Raw SQL Queries

```csharp
using (var query = new Query("Users"))
{
    query.Where("active", "=", 1);
    Row[] rows = query.ReadRows();

    foreach (var row in rows)
    {
        Console.WriteLine($"{row["id"]}: {row["name"]}");
    }
}
```

## Advanced Features

### Joins

```csharp
query.Join("Orders", "Users.Id", "Orders.UserId");
query.LeftJoin("Addresses", "Users.AddressId", "Addresses.Id");
```

### Temporary Tables

```csharp
using SharpOrm.Builder.Tables;

var builder = new TemporaryTableBuilder();
builder.SetName("TempUsers");
builder.AddColumn<int>("Id").IsPrimaryKey().IsAutoIncrement();
builder.AddColumn<string>("Name").HasSize(100);
builder.AddColumn<bool>("Active").HasDefault(true);

var schema = builder.Build();

using (var table = DbTable.Create(schema))
using (var query = table.GetQuery())
{
    query.Insert(new Cell("Name", "Test User"));
    var rows = query.ReadRows();
}
```

## Database Support

| Database | Supported | Configuration |
|----------|-----------|---------------|
| SQL Server | ✅ | `SqlServerQueryConfig` |
| MySQL | ✅ | `MysqlQueryConfig` |
| SQLite | ✅ | `SqliteQueryConfig` |

## Best Practices

```csharp
// ✅ Always dispose queries
using (var query = new Query<User>()) { }

// ✅ Use async for I/O operations
await query.GetAsync();

// ✅ Order before pagination
query.OrderBy(u => u.Id).Paginate(10, 1);

// ✅ Use bulk operations
query.BulkInsert(users);
```