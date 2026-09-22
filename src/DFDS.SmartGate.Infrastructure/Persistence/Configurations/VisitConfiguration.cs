using DFDS.SmartGate.Application.Validation;
using DFDS.SmartGate.Domain.Visits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DFDS.SmartGate.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Visit"/> to the <c>visits</c> table. Truck and driver are complex properties (columns on the same
/// row, no separate identity); movements and history are child tables. Optimistic concurrency uses PostgreSQL's
/// system column <c>xmin</c>, so no version column has to be maintained.
/// Every search filter of <c>GET /api/visits</c> is backed by a composite index that starts with <c>terminal_id</c>
/// (always present because authorization restricts by terminal) and ends with the sort key.
/// </summary>
internal sealed class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
    /// <summary>Table name; referenced by the child configurations' foreign keys.</summary>
    private const string TableName = "visits";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Visit> builder)
    {
        builder.ToTable(TableName, table => table.HasComment(
            "Truck visits. Rows are retained for 7 years (regulatory); archive/partition by created_at, never delete in-app."));

        builder.HasKey(v => v.Id).HasName("pk_visits");
        builder.Property(v => v.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(v => v.TerminalId).HasColumnName("terminal_id").IsRequired();
        builder.Property(v => v.CurrentStatus).HasColumnName("current_status").HasConversion<short>().IsRequired();
        builder.Property(v => v.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(v => v.CreatedBy).HasColumnName("created_by").HasMaxLength(FieldLimits.CreatedByMaxLength).IsRequired();

        builder.ComplexProperty(v => v.Truck, truck =>
        {
            truck.Property(t => t.UnitNumber).HasColumnName("truck_unit_number").IsRequired();
            truck.Property(t => t.LicensePlate).HasColumnName("truck_license_plate").IsRequired();
            truck.Property(t => t.Carrier).HasColumnName("truck_carrier").HasMaxLength(FieldLimits.CarrierMaxLength);
        });

        builder.ComplexProperty(v => v.Driver, driver =>
        {
            driver.Property(d => d.Name).HasColumnName("driver_name").HasMaxLength(FieldLimits.DriverNameMaxLength).IsRequired();
            driver.Property(d => d.LicenseNumber).HasColumnName("driver_license_number").IsRequired();
            driver.Property(d => d.Phone).HasColumnName("driver_phone").HasMaxLength(16);
        });

        builder.HasMany(v => v.Movements)
            .WithOne()
            .HasForeignKey(MovementConfiguration.VisitIdProperty)
            .HasConstraintName("fk_visit_movements_visit")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(v => v.Movements)
            .HasField("_movements")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(v => v.StatusHistory)
            .WithOne()
            .HasForeignKey(StatusHistoryEntryConfiguration.VisitIdProperty)
            .HasConstraintName("fk_visit_status_history_visit")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(v => v.StatusHistory)
            .HasField("_statusHistory")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // PostgreSQL's system column: Npgsql maps a uint "xmin" row version onto it, so no version column is stored.
        builder.Property<uint>("xmin").IsRowVersion();

        // Base search: terminal + time window, already in result order (created_at desc, id desc).
        builder.HasIndex(v => new { v.TerminalId, v.CreatedAt, v.Id })
            .HasDatabaseName("ix_visits_terminal_created")
            .IsDescending(false, true, true);

        // currentStatus filter, e.g. "everything currently On Site at DKCPH".
        builder.HasIndex(v => new { v.TerminalId, v.CurrentStatus, v.CreatedAt })
            .HasDatabaseName("ix_visits_terminal_status_created")
            .IsDescending(false, false, true);

        // createdBy filter (exact match on the token subject).
        builder.HasIndex(v => new { v.TerminalId, v.CreatedBy, v.CreatedAt })
            .HasDatabaseName("ix_visits_terminal_created_by_created")
            .IsDescending(false, false, true);
    }
}
