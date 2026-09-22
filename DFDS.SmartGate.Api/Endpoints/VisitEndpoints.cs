using DFDS.SmartGate.Api.Auth;
using DFDS.SmartGate.Api.Http;
using DFDS.SmartGate.Application.Abstractions;
using DFDS.SmartGate.Application.Visits.Create;
using DFDS.SmartGate.Application.Visits.GetById;
using DFDS.SmartGate.Application.Visits.Models;
using DFDS.SmartGate.Application.Visits.Search;
using DFDS.SmartGate.Application.Visits.UpdateStatus;

namespace DFDS.SmartGate.Api.Endpoints;

/// <summary>
/// The <c>/api/visits</c> surface. Each endpoint binds the request, runs its validator (<see cref="ValidationFilter"/>)
/// and delegates to the Application handler; authorization beyond "authenticated with a subject" is the handler's
/// decision. Adding an endpoint is one <c>Map*</c> line plus its command/validator/handler in Application.
/// </summary>
public static class VisitEndpoints
{
    /// <summary>Route name of <c>GET /api/visits/{visitId}</c>, used for the <c>Location</c> header of a create.</summary>
    private const string GetByIdRoute = "GetVisitById";

    /// <summary>Maps the visit endpoints under <c>/api/visits</c>, all requiring <see cref="AuthorizationPolicies.VisitAccess"/>.</summary>
    /// <param name="endpoints">The route builder.</param>
    /// <returns><paramref name="endpoints"/>, for chaining.</returns>
    public static IEndpointRouteBuilder MapVisitEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var visits = endpoints.MapGroup("/api/visits")
            .WithTags("Visits")
            .RequireAuthorization(AuthorizationPolicies.VisitAccess)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        visits.MapPost("/", CreateAsync)
            .WithName("CreateVisit")
            .WithSummary("Pre-registers a truck visit at a terminal the caller is entitled to.")
            .WithValidation<CreateVisitCommand>()
            .Produces<VisitResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        visits.MapGet("/{visitId:guid}", GetByIdAsync)
            .WithName(GetByIdRoute)
            .WithSummary("Returns a visit with its full status history.")
            .WithValidation<GetVisitQuery>()
            .Produces<VisitResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        visits.MapGet("/", SearchAsync)
            .WithName("SearchVisits")
            .WithSummary("Searches visits of the caller's terminals, newest first, with paging metadata.")
            .WithValidation<SearchVisitsQuery>()
            .Produces<SearchVisitsResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        visits.MapPost("/{visitId:guid}/status", UpdateStatusAsync)
            .WithName("UpdateVisitStatus")
            .WithSummary("Moves a visit to the next status and appends the change to its audit history.")
            .WithRouteVisitId()
            .WithValidation<UpdateVisitStatusCommand>()
            .Produces<VisitResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateVisitCommand command,
        ICommandHandler<CreateVisitCommand, VisitResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        return result.Match(static visit => TypedResults.CreatedAtRoute(visit, GetByIdRoute, new { visitId = visit.Id }));
    }

    private static async Task<IResult> GetByIdAsync(
        [AsParameters] GetVisitQuery query,
        IQueryHandler<GetVisitQuery, VisitResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(query, cancellationToken).ConfigureAwait(false);

        return result.Match(static visit => TypedResults.Ok(visit));
    }

    private static async Task<IResult> SearchAsync(
        [AsParameters] SearchVisitsQuery query,
        IQueryHandler<SearchVisitsQuery, SearchVisitsResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(query, cancellationToken).ConfigureAwait(false);

        return result.Match(static page => TypedResults.Ok(page));
    }

    private static async Task<IResult> UpdateStatusAsync(
        Guid visitId,
        UpdateVisitStatusCommand command,
        ICommandHandler<UpdateVisitStatusCommand, VisitResponse> handler,
        CancellationToken cancellationToken)
    {
        _ = visitId; // Already merged into the command by WithRouteVisitId; kept so the route value binds.

        var result = await handler.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        return result.Match(static visit => TypedResults.Ok(visit));
    }

    /// <summary>
    /// Copies the <c>visitId</c> route value into the body-bound <see cref="UpdateVisitStatusCommand"/> before
    /// validation, so the validator and handler see one complete command and the body cannot carry a different id.
    /// </summary>
    private static RouteHandlerBuilder WithRouteVisitId(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilterFactory(static (factoryContext, next) =>
        {
            var idIndex = ValidationFilter.IndexOfParameter<Guid>(factoryContext.MethodInfo);
            var commandIndex = ValidationFilter.IndexOfParameter<UpdateVisitStatusCommand>(factoryContext.MethodInfo);

            return invocationContext =>
            {
                var command = invocationContext.GetArgument<UpdateVisitStatusCommand>(commandIndex);
                invocationContext.Arguments[commandIndex] = command with { VisitId = invocationContext.GetArgument<Guid>(idIndex) };

                return next(invocationContext);
            };
        });
}
