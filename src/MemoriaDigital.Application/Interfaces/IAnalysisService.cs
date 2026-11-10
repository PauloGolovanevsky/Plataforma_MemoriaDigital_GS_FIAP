using MemoriaDigital.Application.DTOs;

namespace MemoriaDigital.Application.Interfaces;

public interface IAnalysisService
{
    Task<AnalysisResponse> CreateAsync(
        CreateAnalysisRequest request,
        CancellationToken cancellationToken = default);

    Task<AnalysisResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AnalysisResponse>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
