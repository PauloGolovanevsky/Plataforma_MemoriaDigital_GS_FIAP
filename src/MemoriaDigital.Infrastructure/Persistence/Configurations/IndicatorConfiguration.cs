using MemoriaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoriaDigital.Infrastructure.Persistence.Configurations;

public sealed class IndicatorConfiguration : IEntityTypeConfiguration<Indicator>
{
    public void Configure(EntityTypeBuilder<Indicator> builder)
    {
        builder.ToTable("Indicators");
        builder.HasKey(indicator => indicator.Id);

        builder.Property(indicator => indicator.Code)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(indicator => indicator.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(indicator => indicator.Severity)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(indicator => indicator.Evidence)
            .HasMaxLength(2000);

        builder.Property(indicator => indicator.DetectedAt)
            .IsRequired();
    }
}
