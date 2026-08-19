using Dapper;

namespace DapperQX;

public static class WhereClause
{
    public record Term(string ParameterName, string Expression, object? Value);

    /// <summary>
    /// concatenates multiple expression into a single WHERE clause
    /// </summary>
    public static (string Criteria, DynamicParameters Parameters) Build(IEnumerable<Term> terms)
    {
        DynamicParameters dp = new();
        List<string> useTerms = [];
        foreach (var term in terms.Where(t => t.Value is not null))
        {
            dp.Add(term.ParameterName, term.Value);
            useTerms.Add(term.Expression);
        }

        return (string.Join(" AND ", useTerms), dp);
    }
}
