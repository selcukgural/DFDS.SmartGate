namespace DFDS.SmartGate.Api.Http;

/// <summary>Converts validator property paths (<c>Truck.UnitNumber</c>, <c>Movements[0].Location</c>) to the camelCase JSON names clients sent.</summary>
public static class JsonPropertyPath
{
    /// <summary>Lower-cases the first character of every dot-separated segment; indexers are left untouched.</summary>
    /// <param name="propertyPath">A PascalCase path as produced by FluentValidation.</param>
    /// <returns>The camelCase path, or the input when it is already camelCase.</returns>
    public static string ToCamelCase(string propertyPath)
    {
        ArgumentNullException.ThrowIfNull(propertyPath);

        if (!NeedsConversion(propertyPath))
        {
            return propertyPath;
        }

        return string.Create(propertyPath.Length, propertyPath, static (destination, source) =>
        {
            var segmentStart = true;

            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];
                destination[i] = segmentStart ? char.ToLowerInvariant(c) : c;
                segmentStart = c == '.';
            }
        });
    }

    private static bool NeedsConversion(string path)
    {
        var segmentStart = true;

        foreach (var c in path)
        {
            if (segmentStart && char.IsUpper(c))
            {
                return true;
            }

            segmentStart = c == '.';
        }

        return false;
    }
}
