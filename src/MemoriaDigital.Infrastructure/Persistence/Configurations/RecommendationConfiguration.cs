using MemoriaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoriaDigital.Infrastructure.Persistence.Configurations;

public sealed class RecommendationConfiguration : IEntityTypeConfiguration<Recommendation>
{
    public void Configure(EntityTypeBuilder<Recommendation> builder)
    {
        builder.ToTable("Recommendations");
        builder.HasKey(recommendation => recommendation.Id);

        builder.Property(recommendation => recommendation.Code)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(recommendation => recommendation.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(recommendation => recommendation.Priority)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(recommendation => recommendation.CreatedAt)
            .IsRequired();
    }
}
