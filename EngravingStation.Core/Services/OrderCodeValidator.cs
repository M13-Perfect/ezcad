using System.Text.RegularExpressions;

namespace EngravingStation.Core.Services;

public sealed class OrderCodeValidator
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(250);
    private readonly CompiledOrderCodeRule[] _rules;

    public OrderCodeValidator(IEnumerable<string> regexRules)
        : this(regexRules.Select((pattern, index) => new OrderCodeRule($"Rule {index + 1}", pattern)))
    {
    }

    public OrderCodeValidator(IEnumerable<OrderCodeRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        _rules = rules.Select(CompileRule).ToArray();
        if (_rules.Length == 0)
        {
            throw new ArgumentException("At least one order-code regex rule must be configured.", nameof(rules));
        }
    }

    public static OrderCodeValidator Default { get; } = new([
        new OrderCodeRule("Order number", "^ORD-[A-Z0-9]{4,}$"),
        new OrderCodeRule("Tracking number", "^TRK[0-9]{6,}$")
    ]);

    public IReadOnlyList<OrderCodeRule> Rules => _rules.Select(rule => rule.Rule).ToArray();

    public bool IsValid(string? normalizedCode) => Match(normalizedCode) is not null;

    public OrderCodeRule? Match(string? normalizedCode)
    {
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return null;
        }

        return _rules.FirstOrDefault(rule => rule.Regex.IsMatch(normalizedCode))?.Rule;
    }

    private static CompiledOrderCodeRule CompileRule(OrderCodeRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (string.IsNullOrWhiteSpace(rule.Name))
        {
            throw new ArgumentException("Order-code regex rule names must not be empty.", nameof(rule));
        }

        if (string.IsNullOrWhiteSpace(rule.Pattern))
        {
            throw new ArgumentException($"Order-code regex rule '{rule.Name}' must include a pattern.", nameof(rule));
        }

        try
        {
            var regex = new Regex(rule.Pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, MatchTimeout);
            return new CompiledOrderCodeRule(rule, regex);
        }
        catch (ArgumentException exception)
        {
            throw new ArgumentException($"Order-code regex rule '{rule.Name}' is invalid: {exception.Message}", nameof(rule), exception);
        }
    }

    private sealed record CompiledOrderCodeRule(OrderCodeRule Rule, Regex Regex);
}
