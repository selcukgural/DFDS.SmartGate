using System.Security.Cryptography;

namespace DFDS.SmartGate.IntegrationTests.Hosting;

/// <summary>Create request body; property names serialise to the API's camelCase contract.</summary>
internal sealed class CreateVisitRequest
{
    public string? TerminalId { get; set; }

    public TruckRequest? Truck { get; set; } = new();

    public DriverRequest? Driver { get; set; } = new();

    public List<MovementRequest>? Movements { get; set; } =
    [
        new() { Type = "Delivery", UnitNumber = "cont 1", Location = "SEGOT", Reference = "BK-1" },
        new() { Type = "Collection", UnitNumber = "cont 2", Location = "NLRTM" },
    ];
}

internal sealed class TruckRequest
{
    public string? UnitNumber { get; set; } = "trk 001";

    public string? LicensePlate { get; set; } = "ab 12 345";

    public string? Carrier { get; set; } = "Acme Haulage";
}

internal sealed class DriverRequest
{
    public string? Name { get; set; } = "Ada Lovelace";

    public string? LicenseNumber { get; set; } = "dl 1234";

    public string? Phone { get; set; } = "+4512345678";
}

internal sealed class MovementRequest
{
    public string? Type { get; set; }

    public string? UnitNumber { get; set; }

    public string? Location { get; set; }

    public string? Reference { get; set; }
}

/// <summary>Status update request body.</summary>
internal sealed class UpdateStatusRequest
{
    public string? Status { get; set; }

    public string? Reason { get; set; }
}

/// <summary>
/// Terminal codes for test isolation: every test works in its own, freshly generated terminal so that searches never
/// see another test's visits and no clean-up is needed between tests. Codes are format-valid UN/LOCODEs
/// (<c>ZZ</c> + three characters from <c>A-Z2-9</c>) that never collide with the real locations used in requests.
/// </summary>
internal static class Terminals
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ23456789";

    public static string Next()
    {
        Span<char> code = stackalloc char[5];
        code[0] = 'Z';
        code[1] = 'Z';

        for (var i = 2; i < code.Length; i++)
        {
            code[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(code);
    }
}
