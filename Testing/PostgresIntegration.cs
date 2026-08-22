using DapperQX;
using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Testing;

[TestClass]
[DoNotParallelize]
public sealed class PostgresIntegration
{
    private static readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("dapperqx")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext _)
    {
        await _postgres.StartAsync();

        await using var connection = await OpenConnectionAsync();
        await InitializeSchemaAsync(connection);
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await _postgres.DisposeAsync();
    }

    [TestMethod]
    public async Task LogQueryAsync_returns_rows_and_writes_information_log()
    {
        List<LogEntry> entries = [];
        using var loggerFactory = CreateLoggerFactory(entries);
        var logger = loggerFactory.CreateLogger<PostgresIntegration>();

        await using var connection = await OpenConnectionAsync();
        var results = (await connection.LogQueryAsync<Person, PostgresIntegration>(
            logger,
            """
            SELECT id AS Id, name AS Name, is_active AS IsActive
            FROM people
            WHERE is_active = @isActive
            ORDER BY id
            """,
            new { isActive = true },
            correlationId: "corr-success"))
            .ToList();

        Assert.AreEqual(2, results.Count);
        CollectionAssert.AreEqual(new[] { "Ada", "Grace" }, results.Select(x => x.Name).ToList());
        Assert.IsTrue(results.All(x => x.IsActive));

        var infoLog = entries.Single(x => x.Level == LogLevel.Information);
        StringAssert.Contains(infoLog.Message, "LogQueryAsync");
        StringAssert.Contains(infoLog.Message, "FROM people");
        StringAssert.Contains(infoLog.Message, "isActive=True");
        StringAssert.Contains(infoLog.Message, "corr-success");
    }

    [TestMethod]
    public async Task LogQueryAsync_logs_error_and_rethrows_when_query_fails()
    {
        List<LogEntry> entries = [];
        using var loggerFactory = CreateLoggerFactory(entries);
        var logger = loggerFactory.CreateLogger<PostgresIntegration>();

        await using var connection = await OpenConnectionAsync();

        try
        {
            await connection.LogQueryAsync<Person, PostgresIntegration>(
                logger,
                "SELECT missing_column FROM people WHERE is_active = @isActive",
                new { isActive = true },
                correlationId: "corr-error");

            Assert.Fail("Expected PostgresException to be thrown.");
        }
        catch (PostgresException)
        {
        }

        var errorLog = entries.Single(x => x.Level == LogLevel.Error);
        Assert.IsNotNull(errorLog.Exception);
        StringAssert.Contains(errorLog.Message, "missing_column");
        StringAssert.Contains(errorLog.Message, "isActive=True");
        StringAssert.Contains(errorLog.Message, "corr-error");
    }

    private static async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        return connection;
    }

    private static async Task InitializeSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            DROP TABLE IF EXISTS people;

            CREATE TABLE people
            (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                is_active BOOLEAN NOT NULL
            );

            INSERT INTO people (id, name, is_active)
            VALUES
                (1, 'Ada', TRUE),
                (2, 'Grace', TRUE),
                (3, 'Linus', FALSE);
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static ILoggerFactory CreateLoggerFactory(ICollection<LogEntry> entries) =>
        LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddProvider(new CapturingLoggerProvider(entries));
        });

    private sealed record Person(int Id, string Name, bool IsActive);

    private sealed record LogEntry(LogLevel Level, string CategoryName, string Message, Exception? Exception);

    private sealed class CapturingLoggerProvider(ICollection<LogEntry> entries) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, entries);

        public void Dispose()
        {
        }
    }

    private sealed class CapturingLogger(string categoryName, ICollection<LogEntry> entries) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            entries.Add(new(logLevel, categoryName, formatter(state, exception), exception));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
