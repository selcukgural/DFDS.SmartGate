using DFDS.SmartGate.Application.Validation;
using DFDS.SmartGate.Domain.Visits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DFDS.SmartGate.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="StatusHistoryEntry"/> to <c>visit_status_history</c>. The table is append-only: EF never issues
/// updates (all properties are read-only) and the initial migration adds a trigger that rejects <c>UPDATE</c> and
/// <c>DELETE</c> at the database, so the audit trail cannot be rewritten by any client.
/// </summary>
internal sealed class StatusHistoryEntryConfiguration : IEntityTypeConfiguration<StatusHistoryEntry>
{
    /// <summary>Table name; also used by the immutability trigger in the migration.</summary>
    private const string TableName = "visit_status_history";

    /// <summary>Shadow foreign key to the owning visit.</summary>
    public const string VisitIdProperty = "VisitId";

    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<StatusHistoryEntry> builder)
    {
        builder.ToTable(TableName, table => table.HasComment(
            "Append-only audit trail of status changes; UPDATE/DELETE are rejected by trigger."));

        builder.HasKey(h => h.Id).HasName("pk_visit_status_history");
        builder.Property(h => h.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property<Guid>(VisitIdProperty).HasColumnName("visit_id").IsRequired();
        builder.Property(h => h.Status).HasColumnName("status").HasConversion<short>().IsRequired();
        builder.Property(h => h.ChangedAt).HasColumnName("changed_at").IsRequired();
        builder.Property(h => h.ChangedBy).HasColumnName("changed_by").HasMaxLength(FieldLimits.CreatedByMaxLength).IsRequired();
        builder.Property(h => h.Reason).HasColumnName("reason").HasMaxLength(FieldLimits.ReasonMaxLength);

        // History is always read per visit in chronological order.
        builder.HasIndex(VisitIdProperty, nameof(StatusHistoryEntry.ChangedAt))
            .HasDatabaseName("ix_visit_status_history_visit_changed_at");
    }
}
