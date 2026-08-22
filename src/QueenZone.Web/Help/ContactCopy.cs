namespace QueenZone.Web;

/// <summary>
/// Visitor-facing copy for the public contact form (website <c>/contact</c> and
/// <c>/api/v1/contact</c>). Admin review still uses the Help-request inbox from PR #711.
/// </summary>
public static class ContactCopy
{
    public const string ConfirmationTitle = "Thank you";

    public const string ConfirmationStandfirst = "Your message has been sent to the site admin.";

    public const string ConfirmationMessage =
        "Thanks — we have your message. The site admin will read it and reply by email if a response is needed.";

    public const string Intro =
        "This form reaches the site admin. It is not a public forum post. " +
        "You will get a reply by email if we need more information or have an update.";
}
