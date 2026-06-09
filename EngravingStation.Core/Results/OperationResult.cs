namespace EngravingStation.Core.Results;

public sealed record OperationIssue(string Code, string Message, OperationIssueSeverity Severity);

public enum OperationIssueSeverity
{
    Warning,
    Error
}

public sealed class OperationResult
{
    private readonly List<OperationIssue> _issues = [];

    public IReadOnlyList<OperationIssue> Issues => _issues;
    public bool Succeeded => _issues.All(issue => issue.Severity != OperationIssueSeverity.Error);
    public bool HasWarnings => _issues.Any(issue => issue.Severity == OperationIssueSeverity.Warning);

    public void AddError(string code, string message) => _issues.Add(new OperationIssue(code, message, OperationIssueSeverity.Error));
    public void AddWarning(string code, string message) => _issues.Add(new OperationIssue(code, message, OperationIssueSeverity.Warning));
}

public sealed class OperationResult<T>
{
    public OperationResult(T? value, IReadOnlyList<OperationIssue> issues)
    {
        Value = value;
        Issues = issues;
    }

    public T? Value { get; }
    public IReadOnlyList<OperationIssue> Issues { get; }
    public bool Succeeded => Issues.All(issue => issue.Severity != OperationIssueSeverity.Error);
    public bool HasWarnings => Issues.Any(issue => issue.Severity == OperationIssueSeverity.Warning);

    public static OperationResult<T> Success(T value) => new(value, []);
    public static OperationResult<T> Failure(params OperationIssue[] issues) => new(default, issues);
}
