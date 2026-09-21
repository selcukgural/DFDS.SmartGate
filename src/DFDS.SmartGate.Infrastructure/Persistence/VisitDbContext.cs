using DFDS.SmartGate.Domain.Identifiers;
using DFDS.SmartGate.Domain.Locations;
using DFDS.SmartGate.Domain.Visits;
using DFDS.SmartGate.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;

namespace DFDS.SmartGate.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the visit store (PostgreSQL). Exposes only the aggregate root: movements and status history
/// are reached through <see cref="Visit"/> and never queried or modified on their own.
/// Mapping lives in <c>Persistence/Configurations</c>; a new aggregate needs a <see cref="DbSet{TEntity}"/> here
/// and one <see cref="IEntityTypeConfiguration{TEntity}"/> per table.
/// </summary>
/// <param name="options">Provider and behaviour options supplied by <see cref="DependencyInjection.AddInfrastructure"/>.</param>
public sealed class VisitDbContext(DbContextOptions<VisitDbContext> options) : DbContext(options)
{
    /// <summary>The visit aggregates. Reads that do not need tracking must call <c>AsNoTracking()</c>.</summary>
    public DbSet<Visit> Visits => Set<Visit>();

    /// <summary>
    /// Registers a value converter and column length for every property of each domain value-object type, so the
    /// individual configurations only name columns.
    /// </summary>
    /// <param name="configurationBuilder">The pre-convention model configuration.</param>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        configurationBuilder.Properties<UnitNumber>()
            .HaveConversion<UnitNumberConverter>()
            .HaveMaxLength(UnitNumber.MaxLength);

        configurationBuilder.Properties<LicensePlate>()
            .HaveConversion<LicensePlateConverter>()
            .HaveMaxLength(LicensePlate.MaxLength);

        configurationBuilder.Properties<DriverLicenseNumber>()
            .HaveConversion<DriverLicenseNumberConverter>()
            .HaveMaxLength(DriverLicenseNumber.MaxLength);

        configurationBuilder.Properties<LocationCode>()
            .HaveConversion<LocationCodeConverter>()
            .HaveMaxLength(LocationCode.Length)
            .AreFixedLength();
    }

    /// <summary>Applies every <see cref="IEntityTypeConfiguration{TEntity}"/> in this assembly.</summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VisitDbContext).Assembly);
    }
}
