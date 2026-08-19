namespace DapperQX.Attributes;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
public class CaseAttribute(string scope, object value, string expression) : Attribute
{
    public CaseAttribute(object value, string expression) : this(Extensions.DefaultWhereScope, value, expression)
    {
    }

    public string Scope { get; } = scope;

    public object Value { get; } = value;

    public string Expression { get; } = expression;
}
