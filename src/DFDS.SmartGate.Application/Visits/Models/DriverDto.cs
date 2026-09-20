using System.ComponentModel;

namespace DFDS.SmartGate.Application.Visits.Models;

/// <summary>The driver as returned to clients. Personal data: only ever returned to callers entitled to the visit's terminal.</summary>
/// <param name="Name">Full name.</param>
/// <param name="LicenseNumber">Driving license number, normalised.</param>
/// <param name="Phone">Contact number in E.164 format, when supplied.</param>
[ImmutableObject(true)]
public sealed record DriverDto(string Name, string LicenseNumber, string? Phone);
