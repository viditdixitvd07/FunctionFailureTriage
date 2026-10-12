
using System.Text.Json;
using System.Text.RegularExpressions;

var log = args.Length > 0 && File.Exists(args[0])
    ? File.ReadAllText(args[0])
    : string.Join(Environment.NewLine, ReadLines());

if (string.IsNullOrWhiteSpace(log))
{
    Console.WriteLine("Usage: dotnet run -- sample-log.txt");
    return;
}

var findings = LogTriage.Analyze(log);

var report = new
{
    Tool = "FunctionFailureTriage",
    AnalyzedAtUtc = DateTime.UtcNow,
    TotalCharacters = log.Length,
    Findings = findings,
    OverallSeverity = findings.Any(x => x.Severity == "Critical")
        ? "Critical"
        : findings.Any(x => x.Severity == "High")
            ? "High"
            : findings.Any(x => x.Severity == "Medium")
                ? "Medium"
                : findings.Count == 0 ? "Unknown" : "Low"
};

Console.WriteLine(JsonSerializer.Serialize(report,
    new JsonSerializerOptions { WriteIndented = true }));

static IEnumerable<string> ReadLines()
{
    Console.WriteLine("Paste log text. Enter END on a separate line to analyze:");
    string? line;
    while ((line = Console.ReadLine()) is not null &&
           !line.Equals("END", StringComparison.OrdinalIgnoreCase))
    {
        yield return line;
    }
}

record Finding(
    string Category,
    string Severity,
    string Evidence,
    string LikelyCause,
    string[] RecommendedActions);

static class LogTriage
{
    private static readonly (string Category, string Severity,
        string Pattern, string Cause, string[] Actions)[] Rules =
    [
        ("Authentication", "High",
            @"(401 Unauthorized|403 Forbidden|AuthenticationFailed|AccessDenied)",
            "The identity or credentials may not have the required access.",
            [
                "Verify the configured identity and authentication method.",
                "Check RBAC roles and resource permissions.",
                "Confirm credentials or tokens are valid without logging secrets."
            ]),

        ("Dependency timeout", "High",
            @"(TimeoutException|timed out|TaskCanceledException|504 Gateway Timeout)",
            "A dependency may be slow, unavailable, or unreachable.",
            [
                "Inspect dependency latency and timeout settings.",
                "Check network connectivity and service health.",
                "Use bounded retries with backoff where appropriate."
            ]),

        ("Configuration", "High",
            @"(ConfigurationErrorsException|KeyNotFoundException|missing configuration|setting .* not found)",
            "A required setting may be missing or incorrectly configured.",
            [
                "Compare required settings with the deployed environment.",
                "Validate setting names and values.",
                "Check deployment configuration without exposing secrets."
            ]),

        ("Service Bus", "High",
            @"(ServiceBusException|MessageLockLostException|QuotaExceededException|MessagingEntityNotFoundException)",
            "A messaging operation may have failed because of entity, quota, or lock issues.",
            [
                "Inspect the exception reason and Service Bus metrics.",
                "Check entity names, permissions, and namespace configuration.",
                "Review lock duration, message processing time, and retry behavior."
            ]),

        ("Database", "High",
            @"(SqlException|NpgsqlException|database connection failed|connection pool exhausted)",
            "Database connectivity, capacity, or query execution may be failing.",
            [
                "Inspect database availability and connection errors.",
                "Review connection pool usage and query duration.",
                "Check transient-fault handling and connection disposal."
            ]),

        ("Memory pressure", "Critical",
            @"(OutOfMemoryException|process.*out of memory|memory limit exceeded)",
            "The process may be exceeding its available memory.",
            [
                "Inspect memory usage and large allocations.",
                "Process large datasets in bounded batches or streams.",
                "Review memory-intensive transformations and object retention."
            ]),

        ("Throttling", "Medium",
            @"(429 Too Many Requests|TooManyRequests|throttl(ed|ing))",
            "A service may be limiting the request rate.",
            [
                "Inspect rate limits and Retry-After headers where available.",
                "Use exponential backoff with jitter.",
                "Reduce concurrency if the downstream service is overloaded."
            ]),

        ("Unhandled exception", "Medium",
            @"(Unhandled exception|NullReferenceException|InvalidOperationException)",
            "An unexpected application error may have interrupted processing.",
            [
                "Inspect the exception type, stack trace, and surrounding logs.",
                "Validate assumptions about null values and application state.",
                "Add targeted error handling where recovery is safe."
            ])
    ];

    public static List<Finding> Analyze(string log)
    {
        var results = new List<Finding>();

        foreach (var rule in Rules)
        {
            var match = Regex.Match(log, rule.Pattern,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (!match.Success)
                continue;

            results.Add(new Finding(
                rule.Category,
                rule.Severity,
                match.Value.Length > 160
                    ? match.Value[..160]
                    : match.Value,
                rule.Cause,
                rule.Actions));
        }

        return results
            .OrderByDescending(x => SeverityRank(x.Severity))
            .ToList();
    }

    private static int SeverityRank(string severity) => severity switch
    {
        "Critical" => 4,
        "High" => 3,
        "Medium" => 2,
        _ => 1
    };
}
