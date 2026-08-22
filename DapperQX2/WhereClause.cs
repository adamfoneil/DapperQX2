using Dapper;
using System.Text.RegularExpressions;

namespace DapperQX;

public static partial class WhereClause
{
    public record Term(string Expression, object? Value);

    /// <summary>
    /// concatenates multiple expression into a single WHERE clause
    /// </summary>
    public static (string Criteria, DynamicParameters Parameters) Build(IEnumerable<Term> terms)
    {
        DynamicParameters dp = new();
        List<string> useTerms = [];
        foreach (var term in terms.Where(t => t.Value is not null))
        {
            var parameters = ExtractParamNames(term.Expression);
            if (parameters.Length > 1) throw new NotSupportedException("Can't have multiple parameters in expression");

            dp.Add(parameters[0], term.Value);
            useTerms.Add(term.Expression);
        }

        return (string.Join(" AND ", useTerms), dp);
    }

    public static string[] ExtractParamNames(string expression)
    {
        var regex = ParamRegex();
        return [..regex.Matches(expression).Select(m => m.Value)];
    }

    [GeneratedRegex(@"@\w+")]
    private static partial Regex ParamRegex();
}
