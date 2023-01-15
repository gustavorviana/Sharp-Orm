Sharp Orm is a **BETA** library that simplifies the creation of database query. Tested with Mysql and Microsoft Sql Server, but also has sqlite compatibility.

There are two ways to work with the library, defining a global configuration or passing the configuration in each query.

In both cases, it is necessary to define a class that implements the interface "SharpOrm.Builder.IQueryConfig" in order to work with the database, this step is necessary for the library to know how to transform objects into a query.

## Using global configuration

To use a global configuration you need to create a new instance of ConnectionCreator, you can create your own class to implement custom rules but in most cases you can use **ConnectionCreator<**T**>** class.

### Configuring the global configuration
```CSharp
using SharpOrm;
using SharpOrm.Builder;

//For Mysql and Sqlite
ConnectionCreator.Default = new ConnectionCreator<SqlConnection>(new MysqlQueryConfig(false), connectionString);
//For Microsoft Sql Server
ConnectionCreator.Default = new ConnectionCreator<SqlConnection>(new SqlServerQueryConfig(false), connectionString);
```

### Using global configuration
```CSharp
using SharpOrm;
using SharpOrm.Builder;

//Class responsible for performing the request in the database.
using(Query query = new Query("Users"))
{
    //Filter that must be used to retrieve the rows from the database.
    query.Where("active", "=", 1);
    Row[] users = query.ReadRows();
}
```

## Using query configuration

```CSharp
using SharpOrm;
using SharpOrm.Builder;

var connection = //You connection instance here.
//For mysql
IQueryConfig config = new MysqlQueryConfig();
//For Microsoft Sql Server
IQueryConfig config = new SqlServerQueryConfig();

using(Query query = new Query(connection, config, "Users"))
{
    query.Where("active", "=", 1);
    Row[] users = query.ReadRows();
}
```

## It is possible to create a Query for a specific model
### User class
```CSharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Users")]//It is recommended to use this attribute, but it is not required.
public class User
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    [Column("record_created")]
    public DateTime CreatedAt { get; set; }
}
```
### Sample code

```CSharp
using SharpOrm;
using SharpOrm.Builder;

using(Query<User> query = new Query<User>())
{
    User user = query.Find(1);//Retrieving a user by id (to use this function, it is necessary that some property has the Key attribute)
}
```

## Inserting values

It is possible to use a C# object with the same structure as the database or use Cell to insert the values.

### Using C# objects

```CSharp
using SharpOrm;
using SharpOrm.Builder;

using(Query<User> query = new Query<User>())
{
    //Single insert
    query.Insert(new TestTable
    {
        Id = 1,
        Name = "My name",
        Nick = "My nick",
        CreatedAt = System.DateTime.Now
    });

    //Multiple insert
    query.BulkInsert(
        new TestTable{ ... },
        new TestTable{ ... },
        new TestTable{ ... }
    );
}
```

### Using Cell and Row (for multiple insert)

```CSharp
using(Query query = new Query("Users"))
{
    //Single insert
    query.Insert(new Cell("Id", 1), new Cell("Name", "My name"), new Cell("Nick", "My nick"), new Cell("CreatedAt", System.DateTime.Now));

    //Multiple insert
    query.BulkInsert(
        new Row(new Cell("Id", 1), new Cell("Name", "My name"), new Cell("Nick", "My nick"), new Cell("CreatedAt", System.DateTime.Now)),
        new Row(...),
        new Row(...)
    );
}
```