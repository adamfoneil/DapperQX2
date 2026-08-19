namespace DapperQX.Attributes;

/// <summary>
/// Defines a WHERE clause expression that is appended to a query
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class WhereAttribute(string scope, string expression) : Attribute
{
    public WhereAttribute(string expression) : this(ServiceExtensions.DefaultWhereScope, expression)
    {
    }

    public string Scope { get; } = scope;
    public string Expression { get; } = expression;
}