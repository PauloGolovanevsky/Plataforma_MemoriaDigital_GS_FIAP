using MemoriaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MemoriaDigital.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<Analysis> Analyses => Set<Analysis>();

    public DbSet<AnalysisInput> AnalysisInputs => Set<AnalysisInput>();

    public DbSet<Indicator> Indicators => Set<Indicator>();

    public DbSet<Recommendation> Recommendations => Set<Recommendation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
