namespace DFDS.SmartGate.Application.Validation;

/// <summary>
/// Length limits for free-text request fields that the domain stores verbatim. Kept in one place so validators,
/// persistence column sizes and documentation agree.
/// </summary>
public static class FieldLimits
{
    /// <summary>Maximum length of a driver's full name.</summary>
    public const int DriverNameMaxLength = 200;

    /// <summary>Maximum length of a carrier / haulier name.</summary>
    public const int CarrierMaxLength = 200;

    /// <summary>Maximum length of a movement's booking reference.</summary>
    public const int ReferenceMaxLength = 64;

    /// <summary>Maximum length of the free-text reason attached to a status change.</summary>
    public const int ReasonMaxLength = 500;

    /// <summary>Maximum length of a <c>createdBy</c> search value (token subjects are short opaque strings).</summary>
    public const int CreatedByMaxLength = 256;

    /// <summary>E.164 phone number: a plus sign followed by 2–15 digits, no leading zero.</summary>
    public const string PhonePattern = @"^\+[1-9]\d{1,14}$";
}
