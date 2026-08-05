namespace RockPaperScissors.Api.Data;

public interface IPostgres
{
    Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null);

    Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null);

    Task<int> ExecuteAsync(string sql, object? parameters = null);
}
