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
            async (s, p, t, ct, to) => await connection.QueryAsync<TResult>(s, p, t, to, ct),
            correlationId,
            txn,
            commandType,
            timeout);

    public static async Task<TResult?> LogQuerySingleOrDefaultAsync<TResult, TLogger>(
        this IDbConnection connection, ILogger<TLogger> logger,
        string sql, object? parameters,
        IDbTransaction? txn = null, CommandType? commandType = null, int timeout = 30, string? correlationId = null) =>
        await ExecuteInternalAsync(
            logger,
            nameof(LogQueryAsync),
            sql,
            parameters,
            async (s, p, t, ct, to) => await connection.QuerySingleOrDefaultAsync<TResult>(s, p, t, to, ct),
            correlationId,
            txn,
            commandType,
            timeout);

    public static string GetParameterString(object? parameters)
    {
        if (parameters == null) return "<no parametrers>";
        
        if (parameters is IDictionary<string, object> dict)
        {
            return string.Join(", ", dict.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }

        if (parameters is DynamicParameters dp)
        {
            return string.Join(", ", dp.ParameterNames.Select(p => $"{p}={dp.Get<object>(p)}"));
        }
        
        var props = parameters.GetType().GetProperties();
        if (props.Length == 0) return parameters.ToString() ?? string.Empty;
        
        return string.Join(", ", props.Select(p => $"{p.Name}={p.GetValue(parameters)}"));
    }

    internal static async Task<TResult> ExecuteInternalAsync<TResult, TQuery>(
        ILogger<TQuery> logger,
        string queryType, string sql, object? parameters,
        Func<string, object?, IDbTransaction?, CommandType?, int, Task<TResult>> dapperMethod,
        string? correlationId = null, IDbTransaction? txn = null, CommandType? commandType = null, int timeout = 30)
    {
        var paramValueStr = GetParameterString(parameters);

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
