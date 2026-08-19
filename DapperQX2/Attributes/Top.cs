namespace DapperQX.Attributes;

/// <summary>
/// use the {top} token in your query to indicate where to insert the TOP clause
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class TopAttribute : Attribute
{
}