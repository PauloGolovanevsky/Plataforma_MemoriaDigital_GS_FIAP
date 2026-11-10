using MemoriaDigital.Domain.Enums;

namespace MemoriaDigital.Domain.Entities;

public sealed class Analysis
{
    private readonly List<Indicator> _indicators = [];
    private readonly List<Recommendation> _recommendations = [];

    private Analysis()
    {
    }

    public Analysis(AnalysisInput input)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
        Status = AnalysisStatus.Processing;
    }

    public Guid Id { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public AnalysisStatus Status { get; private set; }

    public RiskLevel? RiskLevel { get; private set; }

    public string? FailureReason { get; private set; }

    public AnalysisInput Input { get; private set; } = null!;

    public IReadOnlyCollection<Indicator> Indicators => _indicators.AsReadOnly();

    public IReadOnlyCollection<Recommendation> Recommendations => _recommendations.AsReadOnly();

    public void AddIndicator(Indicator indicator)
    {
        EnsureProcessing();
        ArgumentNullException.ThrowIfNull(indicator);
        _indicators.Add(indicator);
    }

    public void AddRecommendation(Recommendation recommendation)
    {
        EnsureProcessing();
        ArgumentNullException.ThrowIfNull(recommendation);
        _recommendations.Add(recommendation);
    }

    public void Complete()
    {
        EnsureProcessing();
        Status = AnalysisStatus.Completed;
        RiskLevel = CalculateRiskLevel();
        Finish();
    }

    public void MarkInconclusive()
    {
        EnsureProcessing();
        Status = AnalysisStatus.Inconclusive;
        Finish();
    }

    public void MarkUnsupported()
    {
        EnsureProcessing();
        Status = AnalysisStatus.Unsupported;
        Finish();
    }

    public void MarkFailed(string failureReason)
    {
        EnsureProcessing();

        if (string.IsNullOrWhiteSpace(failureReason))
        {
            throw new ArgumentException("Failure reason is required.", nameof(failureReason));
        }

        Status = AnalysisStatus.Failed;
        FailureReason = failureReason.Trim();
        Finish();
    }

    private RiskLevel CalculateRiskLevel()
    {
        if (_indicators.Any(indicator => indicator.Severity == IndicatorSeverity.High))
        {
            return Enums.RiskLevel.High;
        }

        var score = _indicators.Sum(indicator => indicator.Severity switch
        {
            IndicatorSeverity.Low => 1,
            IndicatorSeverity.Medium => 3,
            IndicatorSeverity.High => 5,
            _ => throw new InvalidOperationException("Unknown indicator severity.")
        });

        return score switch
        {
            <= 2 => Enums.RiskLevel.Low,
            <= 5 => Enums.RiskLevel.Medium,
            _ => Enums.RiskLevel.High
        };
    }

    private void EnsureProcessing()
    {
        if (Status != AnalysisStatus.Processing)
        {
            throw new InvalidOperationException("A finalized analysis cannot be changed.");
        }
    }

    private void Finish()
    {
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
