Sharp Orm is a **BETA** library that simplifies the creation of database query. Tested with Mysql and Microsoft Sql Server, but also has sqlite compatibility.

There are two ways to work with the library, defining a global configuration or passing the configuration in each query.

In both cases, it is necessary to define a class that implements the interface "SharpOrm.Builder.IQueryConfig" in order to work with the database, this step is necessary for the library to know how to transform objects into a query.

## Using global configuration

To use a global configuration, it is necessary to define an "IQueryConfig" in "SharpOrm.QueryDefaults.Config" and a database connection in "SharpOrm.QueryDefaults.Connection".

### Configuring the global configuration
```CSharp
using SharpOrm;
using SharpOrm.Builder;

//For Mysql and Sqlite
QueryDefaults.Config = new MysqlQueryConfig()
//For Microsoft Sql Server
QueryDefaults.Config = new SqlServerQueryConfig()

//Configure the connection that should be used globally
QueryDefaults.Connection = ...
```

### Using global configuration
```CSharp
using SharpOrm;
using SharpOrm.Builder;

//Class responsible for performing the request in the database.
Query query = new Query("Users");
//Filter that must be used to retrieve the rows from the database.
query.Where("active", "=", 1);
Row[] users = query.ReadRows();
```

## Using query configuration

```CSharp
using SharpOrm;
using SharpOrm.Builder;

var connection = ...
//For mysql
IQueryConfig config = new MysqlQueryConfig();
//For Microsoft Sql Server
IQueryConfig config = new SqlServerQueryConfig();

Query query = new Query(connection, config, "Users");
query.Where("active", "=", 1);
Row[] users = query.ReadRows();
```
