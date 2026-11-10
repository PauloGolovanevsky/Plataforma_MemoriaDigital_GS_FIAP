using MemoriaDigital.Domain.Enums;

namespace MemoriaDigital.Application.DTOs;

public sealed record AnalysisResponse(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    AnalysisStatus Status,
    RiskLevel? RiskLevel,
    AnalysisInputResponse Input,
    IReadOnlyCollection<IndicatorResponse> Indicators,
    IReadOnlyCollection<RecommendationResponse> Recommendations);

public sealed record AnalysisInputResponse(
    Guid Id,
    InputType Type,
    string? Content,
    string? Reference,
    string? FileName,
    string? MediaType);

public sealed record IndicatorResponse(
    Guid Id,
    string Code,
    string Description,
    IndicatorSeverity Severity,
    string? Evidence,
    DateTimeOffset DetectedAt);

public sealed record RecommendationResponse(
    Guid Id,
    string Code,
    string Description,
    RecommendationPriority Priority,
    DateTimeOffset CreatedAt);
