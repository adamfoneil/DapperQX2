This is a rebuild in progress of my [Dapper.QX](https://github.com/adamfoneil/Dapper.QX) library. V1 assumed SQL Server, so in this version I'm removing that assumption, and modernizing the approach overall.

The problem being solved here is making inline SQL testable and usable in a strong-typed way where LINQ to entities is not a good fit. LINQ is good for 85-90% of query scenarios I think, so this library is for those complex gap cases. The idea is to do something like this:

```csharp
internal class MyResult
{
  public string FirstName { get; set; }
  public string LastName { get; set; }
}

internal class MyQuery<MyResult>() : Query("SELECT * FROM MyTable {where}")
{
  public string? FirstNameLike { get; set; }
  public string? LastNameLike { get; set; }

  protected override WhereClauseTerms => [
    new(FirstNameLike, "[FirstName] LIKE '%' + @firstNameLike + '%'"),
    new(LastNameLike, "[LastName] LIKE '%' + @lastNameLike + '%'")
  ]
}
```

In your app startup, you add queries to your DI container like this:

```csharp
builder.Services.AddQueries();
```

You'd use it like this. There are different injection patterns. This is a Blazor-ish example using `@inject` syntax.

```csharp
@inject MyQuery MyQuery

using var cn = GetConnection();

MyQuery.LastNameLike = "Sm";

var results = await MyQuery.ExecuteAsync(cn);
```
