using MemoriaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MemoriaDigital.Infrastructure.Persistence.Configurations;

public sealed class AnalysisInputConfiguration : IEntityTypeConfiguration<AnalysisInput>
{
    public void Configure(EntityTypeBuilder<AnalysisInput> builder)
    {
        builder.ToTable("AnalysisInputs");
        builder.HasKey(input => input.Id);

        builder.Property(input => input.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(input => input.Content)
            .HasMaxLength(4000);

        builder.Property(input => input.Reference)
            .HasMaxLength(2048);

        builder.Property(input => input.FileName)
            .HasMaxLength(255);

        builder.Property(input => input.MediaType)
            .HasMaxLength(255);
    }
}
