using Dapper;
using Npgsql;

namespace RockPaperScissors.Api.Data;

public class DapperPostgres(NpgsqlDataSource dataSource) : IPostgres
{
    static DapperPostgres()
    {
        // Map snake_case columns (match_event_id) to PascalCase properties (MatchEventId).
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return (await connection.QueryAsync<T>(sql, parameters)).ToList();
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<T>(sql, parameters);
    }

    public async Task<int> ExecuteAsync(string sql, object? parameters = null)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var affected = await connection.ExecuteAsync(sql, parameters, transaction);
        await transaction.CommitAsync();
        return affected;
    }
}
