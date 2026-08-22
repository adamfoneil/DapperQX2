namespace DapperQX;

/// <summary>
/// defines platform-specific SQL syntax elements
/// </summary>
public interface ISqlSyntax
{
    char LeadDelimiter { get; }
    char EndDelimiter { get; }
    string ApplyOffset(string sql, int skip, int take);
}
