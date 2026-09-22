using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DFDS.SmartGate.IntegrationTests.Hosting;

/// <summary>
/// Issues RSA-signed JWTs shaped like those of the identity provider: <c>sub</c> (or <c>client_id</c> for machine
/// clients) plus a multi-valued <c>terminal</c> claim listing the UN/LOCODEs the caller may access.
/// </summary>
public sealed class TestTokens
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private readonly string _issuer;
    private readonly string _audience;
    private readonly SigningCredentials _credentials;
    private readonly SigningCredentials _strangerCredentials;
    private readonly JsonWebTokenHandler _handler = new();

    public TestTokens(string issuer, string audience)
    {
        _issuer = issuer;
        _audience = audience;
        SigningKey = NewKey("tests");
        _credentials = new SigningCredentials(SigningKey, SecurityAlgorithms.RsaSha256);
        _strangerCredentials = new SigningCredentials(NewKey("stranger"), SecurityAlgorithms.RsaSha256);
    }

    /// <summary>The key the host trusts.</summary>
    public RsaSecurityKey SigningKey { get; }

    /// <summary>A valid token for an end user entitled to <paramref name="terminals"/>.</summary>
    public string For(string subject, params string[] terminals) =>
        Issue(new Dictionary<string, object> { ["sub"] = subject, ["terminal"] = terminals }, _credentials, expiresIn: Lifetime);

    /// <summary>A valid token for a machine client (<c>client_id</c>, no <c>sub</c>).</summary>
    public string ForClient(string clientId, params string[] terminals) =>
        Issue(new Dictionary<string, object> { ["client_id"] = clientId, ["terminal"] = terminals }, _credentials, expiresIn: Lifetime);

    /// <summary>A valid token that identifies nobody (neither <c>sub</c> nor <c>client_id</c>).</summary>
    public string WithoutSubject(params string[] terminals) =>
        Issue(new Dictionary<string, object> { ["terminal"] = terminals }, _credentials, expiresIn: Lifetime);

    /// <summary>A token whose <c>exp</c> is already in the past (beyond the configured clock skew).</summary>
    public string Expired(string subject, params string[] terminals) =>
        Issue(new Dictionary<string, object> { ["sub"] = subject, ["terminal"] = terminals }, _credentials, expiresIn: -Lifetime);

    /// <summary>A well-formed token signed with a key the host does not trust.</summary>
    public string SignedByStranger(string subject, params string[] terminals) =>
        Issue(new Dictionary<string, object> { ["sub"] = subject, ["terminal"] = terminals }, _strangerCredentials, expiresIn: Lifetime);

    private string Issue(Dictionary<string, object> claims, SigningCredentials credentials, TimeSpan expiresIn)
    {
        var now = DateTime.UtcNow;
        var expires = now + expiresIn;

        return _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _issuer,
            Audience = _audience,
            Claims = claims,
            IssuedAt = expires < now ? expires - Lifetime : now,
            NotBefore = expires < now ? expires - Lifetime : now,
            Expires = expires,
            SigningCredentials = credentials,
        });
    }

    private static RsaSecurityKey NewKey(string keyId) => new(RSA.Create(2048)) { KeyId = keyId };
}
