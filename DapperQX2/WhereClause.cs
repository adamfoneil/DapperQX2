using Dapper;
using System.Text.RegularExpressions;

namespace DapperQX;

public static partial class WhereClause
{
    /// <summary>
    /// associates a criteria expression with a value
    /// </summary>
    public record Term(object? Value, string Expression, object? NullEquivalent = null);

    /// <summary>
    /// associates multiple targeted criteria expressions with a value
    /// </summary>
    public record ScopedTerm(object? Value, Dictionary<string, string> Expressions, object? NullEquivalent = null);

    /// <summary>
    /// concatenates multiple expression into a single WHERE clause for values provided
    /// </summary>
    public static (string Criteria, DynamicParameters Parameters) Build(IEnumerable<Term> terms)
    {
        DynamicParameters dp = new();
        List<string> useTerms = [];
        foreach (var term in terms.Where(t => t.Value is not null && !Equals(t.Value, t.NullEquivalent)))
        {
            var parameters = ExtractParamNames(term.Expression);
            if (parameters.Length > 1) throw new NotSupportedException("Can't have multiple parameters in expression");

            dp.Add(parameters[0], term.Value);
            useTerms.Add(term.Expression);
        }

        return (string.Join(" AND ", useTerms), dp);
    }

    /// <summary>
    /// this is for when you have disjointed WHERE clauses in a query -- such as criteria applied to an outer query and an inner derived table.
    /// See https://github.com/adamfoneil/Dapper.QX/issues/24 for example
    /// </summary>
    public static (Dictionary<string, string> Criteria, DynamicParameters Parameters) BuildScoped(IEnumerable<ScopedTerm> terms)
    {
        var termsByKey = terms
            .SelectMany(t => t.Expressions.Select(e => new { e.Key, Term = new Term(t.Value, e.Value, t.NullEquivalent) }))
            .ToLookup(item => item.Key, item => item.Term);

        DynamicParameters dp = new();
        Dictionary<string, string> result = [];
        
        foreach (var scope in termsByKey)
        {
            var (criteria, scopeDp) = Build(scope);
            result[scope.Key] = criteria;
            dp = scopeDp; // we need only the last DynamicParameters obj, so the last loop iteration works here
        }

        return (result, dp);
    }

    public static string[] ExtractParamNames(string expression)
    {
        var regex = ParamRegex();
        return [..regex.Matches(expression).Select(m => m.Value)];
    }

    [GeneratedRegex(@"@\w+")]
    private static partial Regex ParamRegex();
}
