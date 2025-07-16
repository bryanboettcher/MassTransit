# Custom Persistence

[NUGET BADGE HERE]

## Quick-start

The MassTransit ecosystem is vast, complex, and well-integrated.  Sometimes, a team just wants to plug in their own repository or service layer.  To start, install the additional NuGet package for the database you're using:

```
Install-Package MassTransit.Persistence.SqlServer
Install-Package MassTransit.Persistence.Postgres
Install-Package MassTransit.Persistence.MySql
```

In your service registrations, use the `.CustomRepository` extension method, and call whatever is needed to properly configure the repository:
```csharp
services.AddMassTransit(bus =>
{
    bus.AddSagaStateMachine<OrderStateMachine, OrderSaga>()
        .CustomRepository(conf => 
            conf.UsingSqlServer(opt => opt.SetConnectionString("my connection string"))
        );
});
```

Support has also been included for JobConsumers and MessageData:
```csharp
services.AddMassTransit(bus =>
{
    bus.AddJobSagaStateMachines()
        .CustomRepository(conf => conf.UsingSqlServer(
            opt => opt.SetConnectionString("my connection string")
        ));

    bus.UsingInMemory((ctx, cfg) =>
    {
        cfg.UseMessageData(conf => conf.UsingSqlServer(
            opt => opt.SetConnectionString("my connection string")
        ));

        cfg.ConfigureEndpoints(ctx);
    });
});
```

::alert{type="warning"}
Using a database as an `IMessageDataRepostiory` is not advised.  The implementations are presented for teams who need them -- but if a better solution can be used, it should.
::

## Repository Behaviors

The default settings are pessimistic concurrency with RepeatableRead isolation, a table named the same as the saga + "s"  (such as `OrderSagas`), and primary key named `CorrelationId`.  Optimistic concurrency is available for any custom saga repository, and requires an appropriate versioning property in the model (dependent on the database).  Each setting is available as methods on the inner configurator.

```csharp
bus.AddSagaStateMachine<OrderStateMachine, OrderSaga>()
    .CustomRepository(conf => conf.UsingSqlServer(opt => opt
        .SetConnectionString("my connection string")
        .SetTableName("Orders")
        .SetIdentityColumnName("OrderId")
        .SetOptimisticConcurrency(m => m.RowVersion)
    ));
```

## Limitations in Default Implementations

### Parameter mapping

Parameters are mapped 1:1 to/from columns of the same case-insensitive name.  Only a few columns allow for changing the name: the primary key and concurrency token.  Everything else must match.  No conversion of data types is done either -- if it's a `DATETIME` in the database, it better map to a `DateTime` in the model.  Override the reader/writer adapters if this behavior is problematic.

### Correlation expressions

Correlations are a little more powerful, but are far from supporting the full query grammar of actual ORMs like Entity Framework.  The following types of expressions are supported:
```csharp
x => x.CorrelationId == id;
x => x.IsDone;
x => !x.IsRunning;
x => x.Threshold < mininum;  // supports  <, <=, ==, !=, >=, >
x => x.IsDone && !x.IsRunning && x.Threshold < minimum;
```

## Custom Repositories

The included repositories are intended to be good enough for simple tasks, but what if you really need to roll up your sleeves and tweak it?  Building a fully custom repository is ultimately a matter of implementing the `DatabaseContext<TSaga>` interface, but there are a lot of helpers along the way.

To start, any custom repository needs to be set as the "context factory" for a particular saga.  The context factory is a delegate, providing an `IServiceProvider` and expecting a `Task<DatabaseContext<TSaga>>` as a response.  Include the repository in the DI container, and then specify it as a lambda:

```csharp
bus.AddScoped<DatabaseContext<OrderSaga>, OrderSagaRepository>();

bus.AddSagaStateMachine<OrderStateMachine, OrderSaga>()
    .CustomRepository(conf => conf.SetContextFactory(
        async ctx => ctx.GetRequiredService<DatabaseContext<OrderSaga>>()
    ));
```

There are several base classes to help implement a custom repository, although they are not required.  Each database provider includes an Optimistic and Pessimistic variant of the base repository.

```csharp
public class OrderSagaRepository : OptimisticSqlServerDatabaseContext<OrderSaga>
{
    const string MyTableName = "Orders";
    const string MyIdColumnName = "OrderId";
    const string MyVersionColumnName = "RowVersion";
    const string MyVersionPropertyName = nameof(OrderSaga.RowVersion);

    public OrderSagaRepository(string connectionString)
        : base(connectionString, MyTableName, MyIdColumnName, MyVersionColumnName, MyVersionPropertyName)
    {
    }
}
```

This implementation is no different than what the configuration methods provide, but this is the smallest code for a custom repository.  Once the custom repository has been defined, there are tons of things that can be overridden to fully control the behavior.

### Customizing the SQL

The queries used for each phase can be completely customized.  This allows calling stored procedures for each step instead of constructing SQL queries.

```csharp
protected override string BuildInsertSql() => "usp_AddOrder";
protected override string BuildUpdateSql() => "usp_UpdateOrder";
protected override string BuildDeleteSql() => "usp_DeleteOrder";
protected override string BuildLoadSql() => "usp_GetOrder";
```

It can also be used for very custom SQL grammar with more conventional statements:
```csharp
protected override string BuildInsertSql() => "INSERT INTO Orders VALUES (@correlationid, @rowversion, GETUTCDATE(), dbo.ufn_GetTenantId());";
```

### Customizing the Object Mapping

Many times, a saga domain object has different needs than the underlying storage engine.  Things like small lists of numbers, custom objects, etc.  While a NoSQL engine will happily save the nested objects, a relational engine can't necessarily do so.  Customizing the mappings allows a developer to include custom object formats, serialized columns, etc as part of the data model, without having to implement joined tables where it's not appropriate.  

Each repository has a Reader Adapter and a Writer Adapter, a small shim to convert to/from the types of objects the database speaks natively.  Reader Adapters are used to convert from the database to the saga, and Writer Adapters go the opposite direction.  Both are customizable via an overridable method.

If using one of the base classes, the reader passed in will be of the specific provider's type (`SqlDataReader`, `NpgsqlDataReader`, or `MySqlDataReader`).  Specifying a custom reader adapter is one of the most performant ways to map objects from the database.

```csharp
protected override Func<IDataReader, OrderSaga> CreateReaderAdapter() => MapFrom;
static OrderSaga MapFrom(IDataReader reader)
{
    if (reader is not SqlDataReader r)
        throw new InvalidOperationException("Only SqlDataReader is supported in this custom adapter");

    return new OrderSaga
    {
        CorrelationId = r.GetFieldValue<Guid>("OrderId"),
        RowVersion = r.GetFieldValue<byte[]>("RowVersion"),
        CreatedOn = r.GetFieldValue<DateTime>("CreatedOn"),
    };
}
```
::alert{type="info"}
The passed-in `IDataReader` is the raw one from the database providers.  No null-checking, serialization, or anything of the sort is performed.  You are responsible for correctly handling `DBNull`, although there are lots of extension methods to help read and write (such as `GetInt32OrNull("columnName")`).
::

The writers behave similarly, although the input is not as straightforward.  When saving a saga, the input object is the entire saga instance.  When loading or querying sagas, the input object is the search parameters.  Typically, implementing the saga-specific writer and delegating everything else to the base class is sufficient.

```csharp
protected override Action<object?, SqlParameterCollection> CreateWriterAdapter() => MapTo;
static void MapTo(object? input, SqlParameterCollection parameters)
{
    if (input is OrderSaga saga)
    {
        parameters.Add("@correlationid", SqlDbType.UniqueIdentifier).Value = saga.CorrelationId;
        parameters.Add("@rowversion", SqlDbType.Timestamp).Value = saga.RowVersion;
        parameters.Add("@createdon", SqlDbType.DateTime2).Value = saga.CreatedOn;

        return;
    }

    AssignParameters(input, parameters);
}
```
::alert{type="info"}
As with the reader, custom writer implementations must handle nulls properly.  An `OrDbNull()` extension method exists to coerce any `null` values to `DBNull`.
::

### Saga queries

Customizing the query behavior of sagas is much trickier because of the required `filterExpression`.  A simple visitor is provided, as the `SqlExpressionVisitor.CreateFromExpression` call shown below.  This returns a `List<SqlPredicate>`, where each `SqlPredicate` object holds the runtime name, value, and operator of the expression.  For instance, this correlation expression produces this predicate: 
```csharp
CorrelateBy((s, c) => s.Threshold < c.Message.Minimum);

new SqlPredicate { Name = "@threshold", Value = <whatever c.Message.Minimum was>, Operator = "<" };
```

After extracting the parameters and operators, the next step is creating the `WHERE` clause.  Each repository base-class provides (overridable) implementations to build the specific grammars for each engine via the `BuildQueryPredicate` method.  This method is responsible for appending `[Threshold] < @threshold` to the query in the example below.

```csharp
protected override string BuildQuerySql(Expression<Func<OrderSaga, bool>> filterExpression, Action<string, object?> parameterCallback)
{
    var sqlRoot = $"SELECT * FROM Orders";

    var predicates = SqlExpressionVisitor.CreateFromExpression(filterExpression, Mappings);

    if (predicates.Count == 0)
        return sqlRoot;

    var queryPredicate = BuildQueryPredicate(predicates, parameterCallback);
    return string.Concat(sqlRoot, " WHERE ", queryPredicate);
}
```

### Other customizations to hook

Immediately after a connection is opened, the `OnConnectionOpened` method is called, passing the newly-created connection as the first parameter.  This can be overridden as needed to set any additional properties on the connection (such as buffer sizes, notification callbacks, exception handlers, whatever) or create transactions.

```csharp
protected override ValueTask OnConnectionOpened(SqlConnection connection, CancellationToken cancellationToken)
{
    connection.StatisticsEnabled = true;
    return base.OnConnectionOpened(connection, cancellationToken);
}
```

Immediately after the writer adapter has written the parameter collection, the `OnParametersWritten` method is called with the command included.  This is the last chance to modify the command before it's sent to the database.

```csharp
protected override ValueTask OnParametersWritten(SqlCommand command, CancellationToken cancellationToken)
{
    command.Parameters.Add("@tenantid", SqlDbType.Int).Value = _tenantId;
    return base.OnParametersWritten(command, cancellationToken);
}
```

## Fully Custom Repository

If the repositories in the other packages won't work for your needs, consider directly implementing `DatabaseContext<TSaga>`, especially if you don't need the query provider.  This is a very straightforward way to utilize existing business logic with MassTransit.  The `OrderSagaRepository` acts as a facade over an existing `IOrderService`, translating calls between the two systems.

```csharp
public class OrderSagaRepository : DatabaseContext<OrderSaga>
{
    readonly IOrderService _service;

    public OrderSagaRepository(IOrderService service)
        => _service = service;

    public ValueTask DisposeAsync() 
        => _service.DisposeAsync();

    public void Dispose() 
        => _service.Dispose();

    public Task DeleteAsync(OrderSaga instance, CancellationToken cancellationToken = default)
        => _service.RemoveOrder(instance.CorrelationId, cancellationToken);

    public Task<OrderSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
        => _service.GetOrderById(correlationId, cancellationToken);

    public Task InsertAsync(OrderSaga instance, CancellationToken cancellationToken = default)
        => _service.CreateOrder(instance, cancellationToken);

    public Task UpdateAsync(OrderSaga instance, CancellationToken cancellationToken = default)
        => _service.UpdateOrder(instance, cancellationToken);

    public IAsyncEnumerable<OrderSaga> QueryAsync(Expression<Func<OrderSaga, bool>> filterExpression, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Orders do not need searches right now");

    public Task CommitAsync(CancellationToken cancellationToken = default)
        => _service.SaveChangesAsync(cancellationToken);
}
```