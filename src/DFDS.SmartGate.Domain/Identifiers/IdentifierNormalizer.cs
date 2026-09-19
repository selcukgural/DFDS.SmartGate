using System.Text;

namespace DFDS.SmartGate.Domain.Identifiers;

/// <summary>
/// Canonical form for unit numbers, license plates and driver license numbers:
/// all whitespace removed, Unicode compatibility-normalised (NFKC) and upper-cased with the invariant culture.
/// Unicode letters are allowed so non-Latin plates (e.g. Chinese, Japanese) survive intact.
/// </summary>
public static class IdentifierNormalizer
{
    private const int StackAllocThreshold = 128;

    /// <summary>
    /// Returns the canonical form of <paramref name="raw"/>, or an empty string when the input is null,
    /// empty or whitespace only. Never throws for user input.
    /// </summary>
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        // NFKC folds full-width forms (e.g. "ＡＢＣ" -> "ABC") and other compatibility variants. Skip the
        // allocation when the input is already in normal form, which is the common case.
        var source = raw.IsNormalized(NormalizationForm.FormKC) ? raw : raw.Normalize(NormalizationForm.FormKC);

        if (IsCanonical(source))
        {
            return source;
        }

        var buffer = source.Length <= StackAllocThreshold ? stackalloc char[source.Length] : new char[source.Length];
        var length = 0;

        foreach (var c in source)
        {
            if (!char.IsWhiteSpace(c))
            {
                buffer[length++] = c;
            }
        }

        return length == 0
            ? string.Empty
            : string.Create(length, buffer[..length], static (destination, compact) => compact.ToUpperInvariant(destination));
    }

    /// <summary>
    /// True when every character is a Unicode letter, a digit or a hyphen. Callers must pass an already
    /// normalised, non-empty value.
    /// </summary>
    public static bool IsValid(ReadOnlySpan<char> normalized)
    {
        if (normalized.IsEmpty)
        {
            return false;
        }

        foreach (var c in normalized)
        {
            if (!char.IsLetterOrDigit(c) && c != '-')
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether the supplied value is already in canonical form for this identifier normalizer.
    /// Canonical values contain no whitespace and no lowercase letters, since normalization upper-cases the
    /// result and strips whitespace before returning it to callers.
    /// </summary>
    private static bool IsCanonical(ReadOnlySpan<char> value)
    {
        foreach (var c in value)
        {
            if (char.IsWhiteSpace(c) || char.IsLower(c))
            {
                return false;
            }
        }

        return true;
    }
}
