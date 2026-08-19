using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;

namespace DapperQX;

public static class QueryExtensions
{
    public static async Task<IEnumerable<TResult>> LogQueryAsync<TResult, TLogger>(
        this IDbConnection connection, ILogger<TLogger> logger, 
        string sql, object? parameters, 
        IDbTransaction? txn = null, CommandType? commandType = null, int timeout = 30, string? correlationId = null) =>
        await ExecuteInternalAsync(
            logger, 
            nameof(LogQueryAsync), 
            sql, 
            parameters, 
            parameters?.ToString() ?? string.Empty, // todo: build static method and use inside Query class
            async (s, p, t, ct, to) => await connection.QueryAsync<TResult>(s, p, t, to, ct), 
            correlationId: correlationId, 
            txn: txn, 
            commandType: commandType, 
            timeout: timeout);

    internal static async Task<TResult> ExecuteInternalAsync<TResult, TQuery>(
        ILogger<TQuery> logger,    
        string queryType, string sql, object? parameters, string paramValueStr,
        Func<string, object?, IDbTransaction?, CommandType?, int, Task<TResult>> dapperMethod,
        string? correlationId = null, IDbTransaction? txn = null, CommandType? commandType = null, int timeout = 30)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var results = await dapperMethod.Invoke(sql, parameters, txn, commandType, timeout);
            sw.Stop();

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("{queryType}: {sql} {elapsed}ms with parameters {parameters}, correlationId {correlationId}", queryType, sql, sw.ElapsedMilliseconds, paramValueStr, correlationId);
            }

            return results;
        }
        catch (Exception exc)
        {
            logger.LogError(exc, "{queryType}: {sql} with parameters {parameters} correlationId {correlationId}", queryType, sql, paramValueStr, correlationId);
            throw;
        }
    }
}
