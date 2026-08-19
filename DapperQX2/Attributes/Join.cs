namespace DapperQX.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class JoinAttribute(string sql) : Attribute
{
    public string Sql { get; } = sql;
}