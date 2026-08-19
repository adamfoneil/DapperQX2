namespace DapperQX.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class OrderByAttribute(object value, string expression) : Attribute
{
    public object Value { get; } = value;

    public string Expression { get; } = expression;
}