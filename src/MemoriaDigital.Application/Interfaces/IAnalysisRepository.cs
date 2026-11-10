using MemoriaDigital.Domain.Entities;

namespace MemoriaDigital.Application.Interfaces;

public interface IAnalysisRepository
{
    Task AddAsync(Analysis analysis, CancellationToken cancellationToken = default);

    Task<Analysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyCollection<Analysis> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
