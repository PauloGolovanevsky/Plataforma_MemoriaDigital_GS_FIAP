using MemoriaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoriaDigital.Infrastructure.Persistence.Configurations;

public sealed class AnalysisConfiguration : IEntityTypeConfiguration<Analysis>
{
    public void Configure(EntityTypeBuilder<Analysis> builder)
    {
        builder.ToTable("Analyses");
        builder.HasKey(analysis => analysis.Id);

        builder.Property(analysis => analysis.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(analysis => analysis.RiskLevel)
            .HasConversion<string>();

        builder.Property(analysis => analysis.FailureReason)
            .HasMaxLength(2000);

        builder.Property(analysis => analysis.CreatedAt)
            .IsRequired();

        // As relações dependem da análise; ao removê-la, seus dados associados não ficam órfãos no banco.
        builder.HasOne(analysis => analysis.Input)
            .WithOne()
            .HasForeignKey<AnalysisInput>("AnalysisId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(analysis => analysis.Indicators)
            .WithOne()
            .HasForeignKey("AnalysisId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(analysis => analysis.Recommendations)
            .WithOne()
            .HasForeignKey("AnalysisId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
