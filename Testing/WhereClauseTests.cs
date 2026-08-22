using DapperQX;

namespace Testing;

[TestClass]
public sealed class WhereClauseTests
{
    [TestMethod]
    public void SimpleExample()
    {
        var (whereClause, _) = WhereClause.Build([
            new(23, "[EmployeeId]=@employeeId"),
            new(100, "[Amount]>@amount"),
            new(null, "[Name] LIKE '%' + @name + '%'"),
            new(0, "[DistributionAmount]>=@distributionAmount", NullEquivalent: 0) // omitted because 0 is "equivalent" to null here
        ]);

        Assert.AreEqual("[EmployeeId]=@employeeId AND [Amount]>@amount", whereClause);
    }

    [TestMethod]
    public void AnotherExample()
    {
        var (whereClause, _) = WhereClause.Build([
            new(null, "[EmployeeId]=@employeeId"),
            new(100, "[Amount]>@amount"),
            new("frank", "[Name] LIKE '%' + @name + '%'"),
            new(15, "[DistributionAmount]>=@distributionAmount", NullEquivalent: 0)
        ]);

        Assert.AreEqual("[Amount]>@amount AND [Name] LIKE '%' + @name + '%' AND [DistributionAmount]>=@distributionAmount", whereClause);
    }

    [TestMethod]
    public void ScopedExample()
    {
        WhereClause.ScopedTerm[] terms = [
            new(new DateTime(2026, 1, 1), new() 
            {
                ["prj"] = "[p].[Date]>=@minWorkDate",
                ["wr"] = "[wr].[Date]>=@minWorkDate"
            }),
            new(new DateTime(2026, 12, 31), new()
            {
                ["prj"] = "[p].[Date]<=@maxWorkDate",
                ["wr"] = "[wr].[Date]<=@maxWorkDate"
            })
        ];

        var (criteria, dp) = WhereClause.BuildScoped(terms);

        Assert.AreEqual("[p].[Date]>=@minWorkDate AND [p].[Date]<=@maxWorkDate", criteria["prj"]);
        Assert.AreEqual("[wr].[Date]>=@minWorkDate AND [wr].[Date]<=@maxWorkDate", criteria["wr"]);

        Assert.AreEqual(new DateTime(2026, 1, 1), dp.Get<DateTime>("minWorkDate"));
        Assert.AreEqual(new DateTime(2026, 12, 31), dp.Get<DateTime>("maxWorkDate"));
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
