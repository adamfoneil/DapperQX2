using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;

namespace DapperQX;

public class Query<T>(string sqlTemplate, ILogger<Query<T>> logger)
{    
    private readonly ILogger<Query<T>> _logger = logger;

    public string SqlTemplate { get; init; } = sqlTemplate;

    protected virtual string BuildQuery()
    {
        // apply properties
        return SqlTemplate;
    }

    protected virtual DynamicParameters BuildParameters() => throw new NotImplementedException();

    public async Task<T> ExecuteSingleAsync(IDbConnection connection, IDbTransaction? txn = null, string? correlationId = null) =>
        await ExecuteInnerAsync(connection, async (cn, sql, dp, txn) => await cn.QuerySingleAsync<T>(sql, dp, txn), txn, correlationId);
    
    public async Task<IEnumerable<T>> ExecucuteAsync(IDbConnection connection, IDbTransaction? txn = null, string? correlationId = null) =>
        await ExecuteInnerAsync(connection, async (cn, sql, dp, txn) => await cn.QueryAsync<T>(sql, dp, txn), txn, correlationId);    

    private async Task<TInner> ExecuteInnerAsync<TInner>(
        IDbConnection connection, 
        Func<IDbConnection, string, DynamicParameters, IDbTransaction?, Task<TInner>> dapperMethod, 
        IDbTransaction? txn = null, string? correlationId = null)
    {
        var sql = BuildQuery();
        var parameters = BuildParameters();
        var paramValues = GetParameterValues(parameters);
        var paramValueStr = string.Join(", ", paramValues.Select(kp => $"{kp.Key}={kp.Value}"));
        var queryType = GetType().Name;

        try
        {
            var sw = Stopwatch.StartNew();
            var results = await dapperMethod.Invoke(connection, sql, parameters, txn);
            sw.Stop();

            if (_logger.IsEnabled(LogLevel.Information))
            {                
                _logger.LogInformation("{queryType}: {sql} {elapsed}ms with parameters {parameters}, correlationId {correlationId}", queryType, sql, sw.ElapsedMilliseconds, paramValueStr, correlationId);
            }

            return results;
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "{queryType}: {sql} with parameters {parameters} correlationId {correlationId}", queryType, sql, paramValueStr, correlationId);
            throw;
        }
    }

    private static Dictionary<string, object> GetParameterValues(DynamicParameters parameters)
    {
        var result = new Dictionary<string, object>();
        if (parameters == null) return result;

        foreach (var paramName in parameters.ParameterNames)
        {
            try
            {
                result[paramName] = parameters.Get<dynamic>(paramName);
            }
            catch
            {
                result[paramName] = "<unable to retrieve>";
            }
        }

        return result;
    }
}
