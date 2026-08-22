using Dapper;
using Microsoft.Extensions.Logging;
using System.Data;

namespace DapperQX;

public abstract class Query<T>(string sqlTemplate, ILogger<Query<T>> logger)
{    
    private readonly ILogger<Query<T>> _logger = logger;

    public string SqlTemplate { get; init; } = sqlTemplate;

    /// <summary>
    /// Defines named sort options for the query. 
    /// Keys are arbitrary identifiers referenced by the SortKey property to determine a query's sort behavior.
    /// Values are SQL expressions
    /// </summary>
    protected virtual Dictionary<string, string> SortOptions { get; set; } = [];

    /// <summary>
    /// which SortOptions Key is applied?
    /// </summary>
    public string? SortKey { get; set; }

    protected abstract WhereClause.Term[] WhereClauseTerms { get; }

    private const string WhereToken = "{where}";
    private const string AndWhereToken = "{andWhere}";

    private (string, DynamicParameters) ResolveQuery()
    {
        string result = SqlTemplate;
        var (whereClause, dp) = WhereClause.Build(WhereClauseTerms);

        if (result.Contains(WhereToken))
        {
            result = result.Replace(WhereToken, whereClause);
        }
        
        if (result.Contains(AndWhereToken))
        {
            result = result.Replace(AndWhereToken, whereClause);
        }


        var orderBy = SortKey is not null ? SortOptions.GetValueOrDefault(SortKey) : default;

        throw new NotImplementedException();
    }

    protected virtual CommandType CommandType => CommandType.Text;

    protected virtual int Timeout => 30;

    protected virtual DynamicParameters BuildParameters() => throw new NotImplementedException();

    public async Task<T> ExecuteSingleAsync(IDbConnection connection, IDbTransaction? txn = null, string? correlationId = null) =>
        await ExecuteInnerAsync(async (sql, dp, txn, cmdType, timeout) => 
            await connection.QuerySingleAsync<T>(sql, dp, txn, timeout, cmdType), txn, correlationId);

    public async Task<T?> ExecuteSingleOrDefaultAsync(IDbConnection connection, IDbTransaction? txn = null, string? correlationId = null) =>
        await ExecuteInnerAsync(async (sql, dp, txn, cmdType, timeout) => 
            await connection.QuerySingleOrDefaultAsync<T>(sql, dp, txn, timeout, cmdType), txn, correlationId);

    public async Task<IEnumerable<T>> ExecucuteAsync(IDbConnection connection, IDbTransaction? txn = null, string? correlationId = null) =>
        await ExecuteInnerAsync(async (sql, dp, txn, cmdType, timeout) => 
            await connection.QueryAsync<T>(sql, dp, txn, timeout, cmdType), txn, correlationId);    

    private async Task<TInner> ExecuteInnerAsync<TInner>(        
        Func<string, object?, IDbTransaction?, CommandType?, int, Task<TInner>> dapperMethod, 
        IDbTransaction? txn = null, string? correlationId = null)
    {
        var (sql, parameters) = ResolveQuery();        
        var queryType = GetType().Name;

        return await QueryExtensions.ExecuteInternalAsync(_logger,
            queryType, sql, parameters,
            dapperMethod,
            correlationId, txn, CommandType, Timeout);
    }    
}
