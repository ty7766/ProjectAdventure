using System;
using System.Net.Mail;

public enum ConsentState
{
    Unknown,
    Granted,
    Declined
}

public enum PlatformNotice
{
    GoogleProgressRestored,
    GoogleLinked
}

public enum SupportError
{
    None,
    TooShort,
    InvalidEmail,
    RateLimited,
    Failed
}

public struct ConflictInfo
{
    public int DeviceClearedStages;
    public int DeviceStars;
    public int GoogleClearedStages;
    public int GoogleStars;
}

public struct SupportTicketResult
{
    public bool Ok;
    public string Reference;
    public string ThreadUrl;
    public SupportError Error;
}

public static class SupportTicketValidation
{
    public static SupportError Validate(string body, string email)
    {
        body = (body ?? "").Trim();
        email = (email ?? "").Trim();
        if (body.Length < 5 || body.Length > 2000)
            return SupportError.TooShort;
        if (email.Length == 0)
            return SupportError.None;
        try
        {
            return new MailAddress(email).Address == email ? SupportError.None : SupportError.InvalidEmail;
        }
        catch (FormatException)
        {
            return SupportError.InvalidEmail;
        }
    }
}
