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
        var query = dbContext.Analyses
            .FromSqlRaw("SELECT * FROM \"Analyses\" ORDER BY \"CreatedAt\" DESC, \"Id\" DESC")
            .AsNoTracking()
            .AsSplitQuery()
            .Include(analysis => analysis.Input)
            .Include(analysis => analysis.Indicators)
            .Include(analysis => analysis.Recommendations);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return (items, totalCount);
    }
}
