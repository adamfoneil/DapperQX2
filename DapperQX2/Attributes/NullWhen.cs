namespace DapperQX.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class NullWhenAttribute(params object[] values) : Attribute
{
    public object[] NullValues { get; } = values;
}