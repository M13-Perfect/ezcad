using EngravingStation.Core.Results;

namespace EngravingStation.App.ViewModels;

public sealed class IssueViewModel
{
    public IssueViewModel(OperationIssue issue)
    {
        Severity = issue.Severity.ToString();
        Code = issue.Code;
        Message = issue.Message;
    }

    public string Severity { get; }
    public string Code { get; }
    public string Message { get; }
    public string DisplayText => $"[{Severity}] {Code}: {Message}";
}
