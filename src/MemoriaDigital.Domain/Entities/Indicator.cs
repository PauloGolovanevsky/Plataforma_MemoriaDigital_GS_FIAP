using MemoriaDigital.Domain.Enums;

namespace MemoriaDigital.Domain.Entities;

public sealed class Indicator
{
    private Indicator()
    {
    }

    public Indicator(
        string code,
        string description,
        IndicatorSeverity severity,
        string? evidence = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Indicator code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Indicator description is required.", nameof(description));
        }

        Id = Guid.NewGuid();
        Code = code.Trim();
        Description = description.Trim();
        Severity = severity;
        Evidence = string.IsNullOrWhiteSpace(evidence) ? null : evidence.Trim();
        DetectedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public IndicatorSeverity Severity { get; private set; }

    public string? Evidence { get; private set; }

    public DateTimeOffset DetectedAt { get; private set; }
}
