using DapperQX;

namespace Testing;

[TestClass]
public sealed class WhereClauseTests
{
    [TestMethod]
    public void SimpleExample()
    {
        var (whereClause, _) = WhereClause.Build([
            new("[EmployeeId]=@employeeId", 23),
            new("[Amount]>@amount", 100),
            new("[Name] LIKE '%' + @name + '%'", null)
        ]);

        Assert.AreEqual("[EmployeeId]=@employeeId AND [Amount]>@amount", whereClause);
    }

    [TestMethod]
    [DataRow("[EmployeeId] = @employeeId", "@employeeId")]
    [DataRow("BETWEEN @start AND @end", "@start;@end")]
    [DataRow("[Amount]>=@minAmount", "@minAmount")]
    [DataRow("COALESCE([e].[DateModified], [e].[DateCreated]) > @date AND [Amount] <= @amount", "@date;@amount")]
    public void ExtractParamNames(string input, string expected)
    {
        var names = WhereClause.ExtractParamNames(input);
        var actual = string.Join(";", names);
        Assert.AreEqual(expected, actual);
    }
}
