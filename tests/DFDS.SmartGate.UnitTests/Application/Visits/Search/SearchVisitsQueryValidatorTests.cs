using DFDS.SmartGate.Application.Visits.Search;
using DFDS.SmartGate.Domain.Visits;
using FluentValidation.TestHelper;
using Xunit;
using static DFDS.SmartGate.UnitTests.Application.ApplicationTestData;

namespace DFDS.SmartGate.UnitTests.Application.Visits.Search;

public sealed class SearchVisitsQueryValidatorTests
{
    private readonly SearchVisitsQueryValidator _validator = new();

    [Fact]
    public void EmptyQuery_UsesDefaults_AndIsValid()
    {
        var result = _validator.TestValidate(new SearchVisitsQuery());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void FullyPopulatedValidQuery_IsValid()
    {
        var query = new SearchVisitsQuery
        {
            TerminalId = "dkcph",
            CurrentStatus = VisitStatus.OnSite,
            MovementFrom = "TR",
            MovementTo = "SEGOT",
            CreatedTimeFrom = Now.AddDays(-1),
            CreatedTimeTo = Now,
            CreatedBy = "operator-1",
            Page = 3,
            PageSize = 100,
        };

        _validator.TestValidate(query).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("D")]
    [InlineData("DKC")]
    [InlineData("DKCP1")]
    [InlineData("")]
    public void MovementFilters_MustBeCountryOrLocode(string value)
    {
        var result = _validator.TestValidate(new SearchVisitsQuery { MovementFrom = value, MovementTo = value });

        result.ShouldHaveValidationErrorFor(x => x.MovementFrom)
            .WithErrorMessage("MovementFrom must be either a 2-letter ISO country code (e.g. 'DK') or a 5-character UN/LOCODE (e.g. 'DKCPH').");
        result.ShouldHaveValidationErrorFor(x => x.MovementTo);
    }

    [Fact]
    public void InvalidTerminal_IsReported()
    {
        _validator.TestValidate(new SearchVisitsQuery { TerminalId = "DK" }).ShouldHaveValidationErrorFor(x => x.TerminalId);
    }

    [Fact]
    public void UnknownStatus_IsReported()
    {
        _validator.TestValidate(new SearchVisitsQuery { CurrentStatus = (VisitStatus)9 }).ShouldHaveValidationErrorFor(x => x.CurrentStatus);
    }

    [Fact]
    public void FromAfterTo_IsReported()
    {
        var query = new SearchVisitsQuery { CreatedTimeFrom = Now, CreatedTimeTo = Now.AddSeconds(-1) };

        _validator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.CreatedTimeFrom)
            .WithErrorMessage("'createdTimeFrom' must not be later than 'createdTimeTo'.");
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void PagingOutOfRange_IsReported(int page, int pageSize)
    {
        var result = _validator.TestValidate(new SearchVisitsQuery { Page = page, PageSize = pageSize });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void BlankCreatedBy_IsReported()
    {
        _validator.TestValidate(new SearchVisitsQuery { CreatedBy = " " }).ShouldHaveValidationErrorFor(x => x.CreatedBy);
    }
}
