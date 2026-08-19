using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;

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
        await ExecuteInnerAsync(async (sql, dp, txn) => await connection.QuerySingleAsync<T>(sql, dp, txn), txn, correlationId);

    public async Task<T?> ExecuteSingleOrDefaultAsync(IDbConnection connection, IDbTransaction? txn = null, string? correlationId = null) =>
        await ExecuteInnerAsync(async (sql, dp, txn) => await connection.QuerySingleOrDefaultAsync<T>(sql, dp, txn), txn, correlationId);

    public async Task<IEnumerable<T>> ExecucuteAsync(IDbConnection connection, IDbTransaction? txn = null, string? correlationId = null) =>
        await ExecuteInnerAsync(async (sql, dp, txn) => await connection.QueryAsync<T>(sql, dp, txn), txn, correlationId);    

    private async Task<TInner> ExecuteInnerAsync<TInner>(        
        Func<string, DynamicParameters, IDbTransaction?, Task<TInner>> dapperMethod, 
        IDbTransaction? txn = null, string? correlationId = null)
    {
        var sql = BuildQuery();
        var parameters = BuildParameters();
        var paramValues = GetParameterValues(parameters);
        var paramValueStr = string.Join(", ", paramValues.Select(kp => $"{kp.Key}={kp.Value}"));
        var queryType = GetType().Name;

        return await QueryExtensions.ExecuteInternalAsync(_logger, 
            queryType, sql, parameters, paramValueStr,
            dapperMethod, 
            correlationId, txn);
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
