namespace QueenZone.Web;

/// <summary>
/// Visitor-facing copy for account deletion (website <c>/account/delete</c> and
/// <c>/api/v1/me/deletion-request</c>).
/// </summary>
public static class AccountDeletionCopy
{
    public const string ConfirmationPhrase = "DELETE";

    public const string RequestedTitle = "Account deletion scheduled";

    public const string RequestedMessage =
        "You have been signed out. Your public name is now Deleted member, your profile and avatar are hidden, and retained content has anonymised attribution. " +
        "You can sign back in and cancel deletion at any time during the 30-day cooling-off period. " +
        "Cancelling within 30 days restores your identity and attribution. After 30 days, your sign-in data and stored avatar are permanently removed.";

    public const string ConfirmationHint = "Type DELETE to schedule deletion of the account.";

    public const string ConfirmationRequired = "Type DELETE to confirm account deletion.";

    public static readonly string[] WhatHappens =
    [
        "You are signed out after requesting deletion.",
        "Your public name changes immediately to Deleted member, your profile is hidden, and your avatar is hidden.",
        "Your forum posts and private messages remain with anonymised attribution.",
        "A 30-day cooling-off period starts.",
        "You can sign back in and cancel deletion before the scheduled date.",
        "Cancelling restores your display name, avatar, profile, and content attribution.",
        "After 30 days, your sign-in data and stored avatar are permanently removed.",
        "Your existing legacy account link is retained.",
    ];
}
