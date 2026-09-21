using DFDS.SmartGate.Domain.Identifiers;
using DFDS.SmartGate.Domain.Locations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DFDS.SmartGate.Infrastructure.Persistence.Conversions;

/// <summary>
/// Maps the domain's single-value identifiers to their canonical string column and back. Reading goes through
/// the value-object factory so a corrupt column surfaces as an <see cref="InvalidOperationException"/>
/// at materialisation instead of a half-valid aggregate. Registered once for every property of the type in
/// <see cref="VisitDbContext.ConfigureConventions"/>; a new value object needs one converter class here.
/// </summary>
internal sealed class UnitNumberConverter() : ValueConverter<UnitNumber, string>(
    static unitNumber => unitNumber.Value,
    static stored => UnitNumber.Create(stored).Value);

/// <inheritdoc cref="UnitNumberConverter"/>
internal sealed class LicensePlateConverter() : ValueConverter<LicensePlate, string>(
    static plate => plate.Value,
    static stored => LicensePlate.Create(stored).Value);

/// <inheritdoc cref="UnitNumberConverter"/>
internal sealed class DriverLicenseNumberConverter() : ValueConverter<DriverLicenseNumber, string>(
    static license => license.Value,
    static stored => DriverLicenseNumber.Create(stored).Value);

/// <summary>
/// Stores a <see cref="LocationCode"/> as its five-character UN/LOCODE; the <see cref="LocationCode.Country"/> part
/// is re-derived on read and, for searchable columns, also kept as a generated column in the database.
/// </summary>
internal sealed class LocationCodeConverter() : ValueConverter<LocationCode, string>(
    static location => location.Value,
    static stored => LocationCode.Create(stored).Value);
