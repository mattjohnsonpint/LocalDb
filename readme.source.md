# <img src="/src/icon.png" height="30px"> LocalDb

[![Build status](https://ci.appveyor.com/api/projects/status/0shdndxc7xd14d41/branch/main?svg=true)](https://ci.appveyor.com/project/SimonCropp/LocalDb)
[![NuGet Status](https://img.shields.io/nuget/v/LocalDb.svg?label=nuget:LocalDb)](https://www.nuget.org/packages/LocalDb/)
[![NuGet Status](https://img.shields.io/nuget/v/EfLocalDb.svg?label=nuget:EfLocalDb)](https://www.nuget.org/packages/EfLocalDb/)
[![NuGet Status](https://img.shields.io/nuget/v/EfClassicLocalDb.svg?label=nuget:EfClassicLocalDb)](https://www.nuget.org/packages/EfClassicLocalDb/)

Provides a wrapper around [SqlLocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) to simplify running tests against [Entity Framework](https://docs.microsoft.com/en-us/ef/core/) or a raw SQL Database.


**SqlLocalDB is only supported on Windows**

toc
  * [Design](/pages/design.md)
  * [Raw Connection Usage](/pages/raw-usage.md)
  * [EntityFramework Classic Usage](/pages/ef-classic-usage.md)
  * [EntityFramework Core Usage](/pages/ef-usage.md)
  * [EntityFramework Core Migrations](/pages/efmigrations.md)
  * [Directory and name resolution](/pages/directory-and-name-resolution.md)
  * [Sql Management Studio](/pages/sql-management-studio.md)
  * [Logging](/pages/logging.md)
  * [Template database size](/pages/template-database-size.md)
  * [Template Re-generation](/pages/template-regen.md)


## NuGet packages

  * https://www.nuget.org/packages/LocalDb/
  * https://www.nuget.org/packages/EfLocalDb/
  * https://www.nuget.org/packages/EfClassicLocalDb/


## Why


### Goals:

 * Have a isolated SQL Server Database for each unit test method.
 * Does not overly impact performance.
 * Results in a running SQL Server Database that can be accessed via [SQL Server Management Studio ](https://docs.microsoft.com/en-us/sql/ssms/sql-server-management-studio-ssms?view=sql-server-2017) (or other tooling) to diagnose issues when a test fails.


### Why not SQLite

 * SQLite and SQL Server do not have compatible feature sets and there are [incompatibilities between their query languages](https://www.mssqltips.com/sqlservertip/4777/comparing-some-differences-of-sql-server-to-sqlite/).


### Why not SQL Express or full SQL Server

 * Control over file location. SqlLocalDB connections support AttachDbFileName property, which allows developers to specify a database file location. SqlLocalDB will attach the specified database file and the connection will be made to it. This allows database files to be stored in a temporary location, and cleaned up, as required by tests.
 * No installed service is required. Processes are started and stopped automatically when needed.
 * Automatic cleanup. A few minutes after the last connection to this process is closed the process shuts down.
 * Full control of instances using the [Command-Line Management Tool: SqlLocalDB.exe](https://docs.microsoft.com/en-us/sql/relational-databases/express-localdb-instance-apis/command-line-management-tool-sqllocaldb-exe?view=sql-server-2017).


### Why not [EntityFramework InMemory](https://docs.microsoft.com/en-us/ef/core/providers/in-memory/)

 * Difficult to debug the state. When debugging a test, or looking at the resultant state, it is helpful to be able to interrogate the Database using tooling
 * InMemory is implemented with shared mutable state between instance. This results in strange behaviors when running tests in parallel, for example when [creating keys](https://github.com/aspnet/EntityFrameworkCore/issues/6872).
 * InMemory is not intended to be an alternative to SqlServer, and as such it does not support the full suite of SqlServer features. For example:
    * Does not support [Timestamp/row version](https://docs.microsoft.com/en-us/ef/core/modeling/concurrency#timestamprow-version).
    * [Does not validate constraints](https://github.com/aspnet/EntityFrameworkCore/issues/2166).

See the official guidance: [InMemory is not a relational database](https://docs.microsoft.com/en-us/ef/core/miscellaneous/testing/in-memory#inmemory-is-not-a-relational-database).


## References:

 * [Which Edition of SQL Server is Best for Development Work?](https://www.red-gate.com/simple-talk/sql/sql-development/edition-sql-server-best-development-work/#8)
 * [Introducing SqlLocalDB, an improved SQL Express](https://blogs.msdn.microsoft.com/sqlexpress/2011/07/12/introducing-localdb-an-improved-sql-express/)
 * [SQL LocalDB 2019 Download](https://download.microsoft.com/download/7/c/1/7c14e92e-bdcb-4f89-b7cf-93543e7112d1/SqlLocalDB.msi)


## Usage

This project supports several approaches.


### Raw SqlConnection

Interactions with SqlLocalDB via a [SqlConnection](https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlconnection).

[Full Usage](/pages/raw-usage.md)


### EntityFramework Classic

Interactions with SqlLocalDB via [Entity Framework Classic](https://docs.microsoft.com/en-us/ef/ef6/).

[Full Usage](/pages/ef--classic-usage.md)


### EntityFramework Core

Interactions with SqlLocalDB via [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/).

[Full Usage](/pages/ef-usage.md)


## Debugging

To connect to a SqlLocalDB instance using [SQL Server Management Studio ](https://docs.microsoft.com/en-us/sql/ssms/sql-server-management-studio-ssms?view=sql-server-2017) use a server name with the following convention `(LocalDb)\INSTANCENAME`.

So for a instance named `MyDb` the server name would be `(LocalDb)\MyDb`. Note that the name will be different if a `name` or `instanceSuffix` have been defined for SqlInstance.

The server name will be written to [Trace.WriteLine](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.trace.writeline) when a SqlInstance is constructed. It can be accessed programmatically from `SqlInstance.ServerName`. See [Logging](/pages/logging.md).


## SqlLocalDb

The [SqlLocalDb Utility (SqlLocalDB.exe)](https://docs.microsoft.com/en-us/sql/tools/sqllocaldb-utility) is a command line tool to enable users and developers to create and manage an instance of SqlLocalDB.

Useful commands:

 * `sqllocaldb info`: list all instances
 * `sqllocaldb create InstanceName`: create a new instance
 * `sqllocaldb start InstanceName`: start an instance
 * `sqllocaldb stop InstanceName`: stop an instance
 * `sqllocaldb delete InstanceName`: delete an instance (this does not delete the file system data for the instance)


## ReSharper Test Runner

The ReSharper Test Runner has a feature that detects spawned processes, and prompts if they do not shut down when a test ends. This is problematic when using SqlLocalDB since the Sql Server process continues to run:

![](pages/resharper-spawned.png)

To avoid this error spawned processes can be ignored:

![](pages/resharper-ignore-spawned.png)


## Credits

SqlLocalDB API code sourced from https://github.com/skyguy94/Simple.LocalDb



## Icon

[Robot](https://thenounproject.com/term/robot/960055/) designed by [Creaticca Creative Agency](https://thenounproject.com/creaticca/) from [The Noun Project](https://thenounproject.com/).