using DFDS.SmartGate.Api.Http;
using DFDS.SmartGate.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DFDS.SmartGate.UnitTests.Api.Http;

public sealed class ProblemResultsTests
{
    [Theory]
    [InlineData(ErrorKind.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorKind.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorKind.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorKind.Forbidden, StatusCodes.Status403Forbidden)]
    public void StatusCodeFor_MapsEveryKind(ErrorKind kind, int expected)
    {
        Assert.Equal(expected, ProblemResults.StatusCodeFor(kind));
    }

    [Fact]
    public void StatusCodeFor_RejectsUnknownKind()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProblemResults.StatusCodeFor((ErrorKind)99));
    }

    [Fact]
    public void ToProblem_CarriesMessageAndCode()
    {
        var problem = DomainError.Conflict("Visit.InvalidTransition", "Cannot go there.").ToProblem();

        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("Cannot go there.", problem.ProblemDetails.Detail);
        Assert.Equal("Visit.InvalidTransition", problem.ProblemDetails.Extensions[ProblemResults.CodeExtension]);
        Assert.False(string.IsNullOrWhiteSpace(problem.ProblemDetails.Title));
    }

    [Fact]
    public void Match_UsesSuccessFactoryForValues()
    {
        Result<int> result = 42;

        var http = result.Match(static value => TypedResults.Ok(value));

        Assert.Equal(42, Assert.IsType<Ok<int>>(http).Value);
    }

    [Fact]
    public void Match_ReturnsProblemForErrors()
    {
        Result<int> result = DomainError.NotFound("X.Missing", "gone");

        var http = result.Match(static value => TypedResults.Ok(value));

        Assert.Equal(StatusCodes.Status404NotFound, Assert.IsType<ProblemHttpResult>(http).StatusCode);
    }
}
