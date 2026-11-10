using MemoriaDigital.Application.DTOs;
using MemoriaDigital.Application.Interfaces;
using MemoriaDigital.Domain.Entities;

namespace MemoriaDigital.Application.Services;

public sealed class AnalysisService(IAnalysisRepository analysisRepository) : IAnalysisService
{
    public async Task<AnalysisResponse> CreateAsync(
        CreateAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var input = new AnalysisInput(
            request.InputType,
            request.Content,
            request.Reference,
            request.FileName,
            request.MediaType);

        var analysis = new Analysis(input);
        await analysisRepository.AddAsync(analysis, cancellationToken);

        return ToResponse(analysis);
    }

    public async Task<AnalysisResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Analysis id is required.", nameof(id));
        }

        var analysis = await analysisRepository.GetByIdAsync(id, cancellationToken);
        return analysis is null ? null : ToResponse(analysis);
    }

    public async Task<PagedResult<AnalysisResponse>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(pageNumber, pageSize);

        var result = await analysisRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            cancellationToken);

        var items = result.Items.Select(ToResponse).ToArray();
        return new PagedResult<AnalysisResponse>(items, pageNumber, pageSize, result.TotalCount);
    }

    private static void ValidatePagination(int pageNumber, int pageSize)
    {
        if (pageNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than zero.");
        }

        if (pageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 100.");
        }
    }

    private static AnalysisResponse ToResponse(Analysis analysis) =>
        new(
            analysis.Id,
            analysis.CreatedAt,
            analysis.CompletedAt,
            analysis.Status,
            analysis.RiskLevel,
            new AnalysisInputResponse(
                analysis.Input.Id,
                analysis.Input.Type,
                analysis.Input.Content,
                analysis.Input.Reference,
                analysis.Input.FileName,
                analysis.Input.MediaType),
            analysis.Indicators
                .Select(indicator => new IndicatorResponse(
                    indicator.Id,
                    indicator.Code,
                    indicator.Description,
                    indicator.Severity,
                    indicator.Evidence,
                    indicator.DetectedAt))
                .ToArray(),
            analysis.Recommendations
                .Select(recommendation => new RecommendationResponse(
                    recommendation.Id,
                    recommendation.Code,
                    recommendation.Description,
                    recommendation.Priority,
                    recommendation.CreatedAt))
                .ToArray());
}
