using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace QueenZone.Data;

/// <summary>
/// Helpers for invoking SQL Server stored procedures (including output parameters)
/// through the EF-managed connection without Dapper.
/// </summary>
[ExcludeFromCodeCoverage] // SQL Server-only ADO.NET glue; not exercised by SQLite unit tests.
internal static class EfSql
{
    public static async Task<IReadOnlyList<T>> QueryProcAsync<T>(
        QueenZoneDbContext dbContext,
        string procedureName,
        Action<SqlCommand>? configure = null,
        int? commandTimeoutSeconds = null,
        CancellationToken cancellationToken = default)
        where T : class, new()
    {
        var connection = await OpenSqlConnectionAsync(dbContext, cancellationToken);
        await using var command = CreateProcCommand(connection, procedureName, commandTimeoutSeconds);
        configure?.Invoke(command);

        var rows = new List<T>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(MapRow<T>(reader));
        }

        return rows;
    }

    public static async Task<T?> QueryProcSingleOrDefaultAsync<T>(
        QueenZoneDbContext dbContext,
        string procedureName,
        Action<SqlCommand>? configure = null,
        int? commandTimeoutSeconds = null,
        CancellationToken cancellationToken = default)
        where T : class, new()
    {
        var rows = await QueryProcAsync<T>(
            dbContext,
            procedureName,
            configure,
            commandTimeoutSeconds,
            cancellationToken);
        return rows.FirstOrDefault();
    }

    public static async Task<int> ExecuteScalarProcAsync(
        QueenZoneDbContext dbContext,
        string procedureName,
        Action<SqlCommand>? configure = null,
        int? commandTimeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        var connection = await OpenSqlConnectionAsync(dbContext, cancellationToken);
        await using var command = CreateProcCommand(connection, procedureName, commandTimeoutSeconds);
        configure?.Invoke(command);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? 0 : Convert.ToInt32(result);
    }

    public static async Task<int> ExecuteScalarSqlAsync(
        QueenZoneDbContext dbContext,
        string sql,
        Action<SqlCommand>? configure = null,
        int? commandTimeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        var connection = await OpenSqlConnectionAsync(dbContext, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;
        if (commandTimeoutSeconds is not null)
        {
            command.CommandTimeout = commandTimeoutSeconds.Value;
        }

        configure?.Invoke(command);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? 0 : Convert.ToInt32(result);
    }

    public static async Task<bool> ExecuteScalarBoolSqlAsync(
        string connectionString,
        string sql,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null and not DBNull && Convert.ToBoolean(result);
    }

    public static async Task<T> QuerySingleSqlAsync<T>(
        string connectionString,
        string sql,
        CancellationToken cancellationToken = default)
        where T : class, new()
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Expected a single result row.");
        }

        return MapRow<T>(reader);
    }

    public static SqlParameter Input(string name, object? value) =>
        new(name, value ?? DBNull.Value);

    public static SqlParameter OutputInt(string name) =>
        new(name, SqlDbType.Int) { Direction = ParameterDirection.Output };

    public static SqlParameter OutputByte(string name) =>
        new(name, SqlDbType.TinyInt) { Direction = ParameterDirection.Output };

    public static SqlParameter OutputString(string name, int size) =>
        new(name, SqlDbType.NVarChar, size) { Direction = ParameterDirection.Output };

    public static int? GetNullableInt(SqlParameter parameter) =>
        parameter.Value is null or DBNull ? null : Convert.ToInt32(parameter.Value);

    public static string? GetNullableString(SqlParameter parameter) =>
        parameter.Value is null or DBNull ? null : Convert.ToString(parameter.Value);

    private static async Task<SqlConnection> OpenSqlConnectionAsync(
        QueenZoneDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection is not SqlConnection sqlConnection)
        {
            throw new InvalidOperationException(
                $"Stored procedure calls require SQL Server. Provider: {dbContext.Database.ProviderName}");
        }

        if (sqlConnection.State != ConnectionState.Open)
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
        }

        return sqlConnection;
    }

    private static SqlCommand CreateProcCommand(
        SqlConnection connection,
        string procedureName,
        int? commandTimeoutSeconds)
    {
        var command = connection.CreateCommand();
        command.CommandText = procedureName;
        command.CommandType = CommandType.StoredProcedure;
        if (commandTimeoutSeconds is not null)
        {
            command.CommandTimeout = commandTimeoutSeconds.Value;
        }

        return command;
    }

    private static T MapRow<T>(DbDataReader reader)
        where T : class, new()
    {
        var item = new T();
        var properties = typeof(T).GetProperties()
            .Where(property => property.CanWrite)
            .ToArray();

        for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
        {
            var columnName = reader.GetName(ordinal);
            var property = properties.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, columnName, StringComparison.OrdinalIgnoreCase));
            if (property is null || reader.IsDBNull(ordinal))
            {
                continue;
            }

            var value = reader.GetValue(ordinal);
            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            property.SetValue(item, Convert.ChangeType(value, targetType));
        }

        return item;
    }
}
