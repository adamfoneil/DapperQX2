using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;

namespace DapperQX;

public static class QueryExtensions
{
    internal static async Task<TResult> ExecuteInternalAsync<TResult, TQuery>(
        ILogger<TQuery> logger,    
        string queryType, string sql, DynamicParameters parameters, string paramValueStr,
        Func<string, DynamicParameters, IDbTransaction?, Task<TResult>> dapperMethod,
        string? correlationId = null, IDbTransaction? txn = null)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            var results = await dapperMethod.Invoke(sql, parameters, txn);
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
