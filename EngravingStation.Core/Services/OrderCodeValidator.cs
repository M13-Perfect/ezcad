using System.Text.RegularExpressions;

namespace EngravingStation.Core.Services;

public sealed class OrderCodeValidator
{
    private readonly Regex[] _rules;

    public OrderCodeValidator(IEnumerable<string> regexRules)
    {
        _rules = regexRules.Select(rule => new Regex(rule, RegexOptions.Compiled | RegexOptions.CultureInvariant)).ToArray();
    }

    public static OrderCodeValidator Default { get; } = new(["^ORD-[A-Z0-9]{4,}$", "^TRK[0-9]{6,}$"]);

    public bool IsValid(string normalizedCode) => !string.IsNullOrWhiteSpace(normalizedCode) && _rules.Any(rule => rule.IsMatch(normalizedCode));
}
