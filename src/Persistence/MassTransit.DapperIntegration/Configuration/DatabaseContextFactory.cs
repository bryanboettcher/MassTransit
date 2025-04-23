namespace MassTransit;

using DapperIntegration.Saga;
using Microsoft.Data.SqlClient;

public delegate DatabaseContext<TSaga> DatabaseContextFactory<TSaga>(SqlConnection connection, SqlTransaction transaction)
    where TSaga : class, ISaga;
