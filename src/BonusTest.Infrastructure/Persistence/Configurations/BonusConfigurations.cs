using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BonusTest.Domain.Entities;

namespace BonusTest.Infrastructure.Persistence.Configurations;

public class BonusConfiguration : IEntityTypeConfiguration<Bonus>
{
    public void Configure(EntityTypeBuilder<Bonus> builder)
    {
        builder.ToTable("Bonus").HasKey(x => x.BonusId);

        // Columnas CHAR/VARCHAR: se declaran no-Unicode para que EF envíe parámetros varchar
        // y SQL Server pueda usar los índices sin conversiones implícitas.
        builder.Property(x => x.DepartmentCode).HasMaxLength(5).IsUnicode(false).IsFixedLength();
        builder.Property(x => x.Period).HasMaxLength(7).IsUnicode(false).IsFixedLength();
        builder.Property(x => x.ReasonCode).HasMaxLength(6).IsUnicode(false).IsFixedLength();
        builder.Property(x => x.BonusStateCode).HasMaxLength(4).IsUnicode(false).IsFixedLength();
        builder.Property(x => x.CreatedBy).HasMaxLength(50).IsUnicode(false);
        builder.Property(x => x.ModifiedBy).HasMaxLength(50).IsUnicode(false);

        builder.Property(x => x.BonusValue).HasPrecision(16, 2);
        builder.Property(x => x.MonthlySalary).HasPrecision(16, 2);
        builder.Property(x => x.TaxRate).HasPrecision(5, 2);
        builder.Property(x => x.GrossAmount).HasPrecision(16, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(16, 2);
        builder.Property(x => x.NetAmount).HasPrecision(16, 2);

        builder.HasOne<Employee>().WithMany()
            .HasForeignKey(x => new { x.DepartmentCode, x.EmployeeNumber });
        builder.HasOne<BonusType>().WithMany().HasForeignKey(x => x.BonusTypeId);
        builder.HasOne<BonusReason>().WithMany().HasForeignKey(x => x.ReasonCode);
        builder.HasOne<State>().WithMany().HasForeignKey(x => x.BonusStateCode);

        builder.HasMany(x => x.Distributions).WithOne().HasForeignKey(x => x.BonusId);
    }
}

public class BonusProjectDistributionConfiguration : IEntityTypeConfiguration<BonusProjectDistribution>
{
    public void Configure(EntityTypeBuilder<BonusProjectDistribution> builder)
    {
        builder.ToTable("BonusProjectDistribution").HasKey(x => x.BonusProjectDistributionId);
        builder.Property(x => x.ProjectName).HasMaxLength(100).IsUnicode(false);
        builder.Property(x => x.Hours).HasPrecision(8, 2);
        builder.Property(x => x.Amount).HasPrecision(16, 2);
        builder.Property(x => x.CreatedBy).HasMaxLength(50).IsUnicode(false);

        builder.HasOne<EmployeeProjectHours>().WithMany().HasForeignKey(x => x.EmployeeProjectHoursId);
    }
}
