using DFDS.SmartGate.Domain.Identifiers;
using DFDS.SmartGate.Domain.Locations;

namespace DFDS.SmartGate.Domain.Visits;

/// <summary>
/// One leg of a visit: a unit delivered to, or collected from, the terminal. Immutable after creation;
/// only reachable through its owning <see cref="Visit"/>.
/// </summary>
public sealed class Movement
{
    /// <summary>
    /// Initializes a new movement with the supplied identifiers and route information.
    /// </summary>
    /// <param name="id">The unique identifier for the movement.</param>
    /// <param name="type">The movement type, indicating whether it is a delivery or collection.</param>
    /// <param name="unitNumber">The unit associated with the movement.</param>
    /// <param name="from">The source location of the leg.</param>
    /// <param name="to">The destination location of the leg.</param>
    /// <param name="reference">Optional operational or business reference associated with the movement.</param>
    private Movement(Guid id, MovementType type, UnitNumber unitNumber, LocationCode from, LocationCode to, string? reference)
    {
        Id = id;
        Type = type;
        UnitNumber = unitNumber;
        From = from;
        To = to;
        Reference = reference;
    }
    
    /// <summary>Materialisation constructor for the persistence layer; state is written to the backing fields.</summary>
    private Movement()
    {
    }

    /// <summary>
    /// Gets the unique identifier for this movement.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the type of movement, such as a delivery or collection.
    /// </summary>
    public MovementType Type { get; }

    /// <summary>
    /// Gets the unit number for the goods or asset involved in the movement.
    /// </summary>
    public UnitNumber UnitNumber { get; }

    /// <summary>Origin of the leg: the external location for a delivery, the terminal for a collection.</summary>
    public LocationCode From { get; }

    /// <summary>Destination of the leg: the terminal for a delivery, the external location for a collection.</summary>
    public LocationCode To { get; }

    /// <summary>
    /// Gets an optional reference that can be used to trace or describe the movement.
    /// </summary>
    public string? Reference { get; }

    /// <summary>
    /// Creates a movement based on the draft and the terminal location at the current time.
    /// </summary>
    /// <param name="draft">The movement draft containing the unit, type, location, and optional reference.</param>
    /// <param name="terminal">The terminal location used as the opposite endpoint of the movement.</param>
    /// <param name="now">The current time used to generate the movement identifier.</param>
    /// <returns>A new movement instance representing the leg between the supplied locations.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the movement type is not recognized.</exception>
    internal static Movement Create(MovementDraft draft, LocationCode terminal, DateTimeOffset now)
    {
        var (from, to) = draft.Type switch
        {
            MovementType.Delivery => (draft.Location, terminal),
            MovementType.Collection => (terminal, draft.Location),
            _ => throw new ArgumentOutOfRangeException(nameof(draft), draft.Type, "Unknown movement type."),
        };

        return new Movement(Guid.CreateVersion7(now), draft.Type, draft.UnitNumber, from, to, draft.Reference);
    }
}
