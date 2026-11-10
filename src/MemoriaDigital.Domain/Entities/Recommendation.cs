using MemoriaDigital.Domain.Enums;

namespace MemoriaDigital.Domain.Entities;

public sealed class Recommendation
{
    private Recommendation()
    {
    }

    public Recommendation(
        string code,
        string description,
        RecommendationPriority priority = RecommendationPriority.Normal)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Recommendation code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Recommendation description is required.", nameof(description));
        }

        Id = Guid.NewGuid();
        Code = code.Trim();
        Description = description.Trim();
        Priority = priority;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public RecommendationPriority Priority { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
