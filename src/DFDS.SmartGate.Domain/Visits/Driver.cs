using DFDS.SmartGate.Domain.Identifiers;

namespace DFDS.SmartGate.Domain.Visits;

/// <summary>
/// The person driving the truck. Contains personal data: never write it to logs or audit rows.
/// </summary>
/// <param name="Name">Full name as captured at the gate.</param>
/// <param name="LicenseNumber">Driving license number, normalised.</param>
/// <param name="Phone">Optional contact number in E.164 format.</param>
public sealed record Driver(string Name, DriverLicenseNumber LicenseNumber, string? Phone);
