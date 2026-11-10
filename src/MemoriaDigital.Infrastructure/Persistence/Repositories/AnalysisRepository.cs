using MemoriaDigital.Application.Interfaces;
using MemoriaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MemoriaDigital.Infrastructure.Persistence.Repositories;

public sealed class AnalysisRepository(ApplicationDbContext dbContext) : IAnalysisRepository
{
    public async Task AddAsync(Analysis analysis, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        await dbContext.Analyses.AddAsync(analysis, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Analysis?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Analyses
            .AsNoTracking()
            .AsSplitQuery()
            .Include(analysis => analysis.Input)
            .Include(analysis => analysis.Indicators)
            .Include(analysis => analysis.Recommendations)
            .SingleOrDefaultAsync(analysis => analysis.Id == id, cancellationToken);

    public async Task<(IReadOnlyCollection<Analysis> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await dbContext.Analyses.CountAsync(cancellationToken);
        var offset = (pageNumber - 1) * pageSize;
        var orderedIds = await dbContext.Analyses
            .FromSqlInterpolated($"SELECT * FROM \"Analyses\" ORDER BY julianday(\"CreatedAt\") DESC, \"Id\" DESC LIMIT {pageSize} OFFSET {offset}")
            .AsNoTracking()
            .Select(analysis => analysis.Id)
            .ToArrayAsync(cancellationToken);

        var loadedItems = await dbContext.Analyses
            .AsNoTracking()
            .AsSplitQuery()
            .Include(analysis => analysis.Input)
            .Include(analysis => analysis.Indicators)
            .Include(analysis => analysis.Recommendations)
            .Where(analysis => orderedIds.Contains(analysis.Id))
            .ToArrayAsync(cancellationToken);

        var itemsById = loadedItems.ToDictionary(analysis => analysis.Id);
        var items = orderedIds.Select(id => itemsById[id]).ToArray();

        return (items, totalCount);
    }
}
