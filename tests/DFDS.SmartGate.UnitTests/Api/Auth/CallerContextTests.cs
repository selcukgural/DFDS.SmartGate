using System.Security.Claims;
using DFDS.SmartGate.Api.Auth;
using DFDS.SmartGate.Domain.Locations;

namespace DFDS.SmartGate.UnitTests.Api.Auth;

public sealed class CallerContextTests
{
    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "Test"));

    [Fact]
    public void FromPrincipal_UsesSubjectClaim()
    {
        var caller = CallerContext.FromPrincipal(Principal(new Claim("sub", "gate-1"), new Claim("client_id", "app")));

        Assert.Equal("gate-1", caller.Subject);
    }

    [Fact]
    public void FromPrincipal_FallsBackToClientId()
    {
        var caller = CallerContext.FromPrincipal(Principal(new Claim("client_id", "gate-app")));

        Assert.Equal("gate-app", caller.Subject);
    }

    [Theory]
    [InlineData("sub", " ")]
    [InlineData("name", "someone")]
    public void FromPrincipal_ThrowsWithoutUsableSubject(string type, string value)
    {
        Assert.Throws<InvalidOperationException>(() => CallerContext.FromPrincipal(Principal(new Claim(type, value))));
    }

    [Fact]
    public void Terminals_ComeFromRepeatedClaims()
    {
        var caller = CallerContext.FromPrincipal(Principal(
            new Claim("sub", "s"), new Claim("terminal", "DKCPH"), new Claim("terminal", "segot")));

        Assert.Equal(["DKCPH", "SEGOT"], caller.Terminals.Select(t => t.Value));
    }

    [Fact]
    public void Terminals_ComeFromDelimitedSingleClaim()
    {
        var caller = CallerContext.FromPrincipal(Principal(new Claim("sub", "s"), new Claim("terminal", "DKCPH SEGOT,NLRTM;  dkcph")));

        Assert.Equal(["DKCPH", "SEGOT", "NLRTM"], caller.Terminals.Select(t => t.Value));
    }

    [Fact]
    public void Terminals_IgnoreMalformedValues()
    {
        var caller = CallerContext.FromPrincipal(Principal(new Claim("sub", "s"), new Claim("terminal", "DK"), new Claim("terminal", "SEGOT1")));

        Assert.Empty(caller.Terminals);
    }

    [Fact]
    public void CanAccess_MatchesEntitledTerminalsOnly()
    {
        var caller = CallerContext.FromPrincipal(Principal(new Claim("sub", "s"), new Claim("terminal", "DKCPH")));

        Assert.True(caller.CanAccess(LocationCode.Create("DKCPH").Value));
        Assert.False(caller.CanAccess(LocationCode.Create("SEGOT").Value));
    }
}
