using Microsoft.AspNetCore.Identity;
using QueenZone.Data;
using QueenZone.Data.Entities;
using QueenZone.Storage;

namespace QueenZone.Web;

/// <summary>
/// Shared find-or-link-or-create logic for both the native Queenzone.org signup and every
/// external provider's first sign-in.
/// </summary>
/// <remarks>
/// Legacy USERS_T matching is by email only — the legacy PASSWORD column is never read.
/// New accounts do not auto-claim; members confirm the link from Account settings.
/// Existing unlinked accounts get a silent backfill on later sign-in when the legacy id
/// is still free, repairing accounts that missed create-time linking under the old model.
/// </remarks>
public sealed class MemberAccountService(
    IMemberAccountRepository memberAccountRepository,
    ILegacyMemberLookupRepository legacyMemberLookupRepository,
    IBlobUploadService blobUploadService,
    MemberUploadQuotaService uploadQuota)
{
    private readonly PasswordHasher<MemberAccount> passwordHasher = new();

    public async Task<MemberAccountResult> RegisterAsync(string email, string password, string displayName, CancellationToken cancellationToken = default)
    {
        var existing = await memberAccountRepository.FindByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            return MemberAccountResult.Failure("An account with that email already exists.");
        }

        var account = new MemberAccount
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow,
        };
        account.PasswordHash = passwordHasher.HashPassword(account, password);

        var created = await memberAccountRepository.CreateAsync(account, cancellationToken);
        return MemberAccountResult.Success(created);
    }

    public async Task<MemberAccountResult> SignInAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var account = await memberAccountRepository.FindByEmailAsync(email, cancellationToken);
        if (account is null || account.PasswordHash is null)
        {
            return MemberAccountResult.Failure("Incorrect email or password.");
        }

        var verification = passwordHasher.VerifyHashedPassword(account, account.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return MemberAccountResult.Failure("Incorrect email or password.");
        }

        account = await TryBackfillLegacyLinkAsync(account, cancellationToken);
        await memberAccountRepository.RecordLoginAsync(account.Id, DateTime.UtcNow, cancellationToken);
        return MemberAccountResult.Success(account);
    }

    public async Task<MemberAccount> FindOrCreateFromExternalLoginAsync(
        string provider,
        string providerKey,
        string email,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        MemberAccount result;
        var isNewAccount = false;

        var existingByLogin = await memberAccountRepository.FindByExternalLoginAsync(provider, providerKey, cancellationToken);
        if (existingByLogin is not null)
        {
            result = existingByLogin;
        }
        else
        {
            var existingByEmail = await memberAccountRepository.FindByEmailAsync(email, cancellationToken);
            if (existingByEmail is not null)
            {
                await memberAccountRepository.AddExternalLoginAsync(existingByEmail.Id, provider, providerKey, email, cancellationToken);
                result = existingByEmail;
            }
            else
            {
                var account = new MemberAccount
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    DisplayName = displayName,
                    CreatedAt = DateTime.UtcNow,
                };

                var created = await memberAccountRepository.CreateAsync(account, cancellationToken);
                await memberAccountRepository.AddExternalLoginAsync(created.Id, provider, providerKey, email, cancellationToken);
                result = created;
                isNewAccount = true;
            }
        }

        // Existing accounts only: silent backfill for people who never claimed yet.
        // New accounts keep LinkedLegacyUserId null so Settings can offer an explicit claim.
        if (!isNewAccount)
        {
            result = await TryBackfillLegacyLinkAsync(result, cancellationToken);
        }

        await memberAccountRepository.RecordLoginAsync(result.Id, DateTime.UtcNow, cancellationToken);
        return result;
    }

    public async Task<MemberAccount?> FindByIdAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        await memberAccountRepository.FindByIdAsync(memberId, cancellationToken);

    public async Task<IReadOnlyList<string>> ListExternalProvidersAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        await memberAccountRepository.ListExternalProvidersAsync(memberId, cancellationToken);

    /// <summary>
    /// Resolves whether the member is already linked to a legacy account, or can claim one
    /// because their modern email matches a free USERS_T row.
    /// </summary>
    public async Task<LegacyAccountLinkState> GetLegacyLinkStateAsync(
        MemberAccount account,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (account.LinkedLegacyUserId is int linkedId)
        {
            var linked = await legacyMemberLookupRepository.FindByUserIdAsync(linkedId, cancellationToken)
                ?? new LegacyMemberMatch(linkedId, $"User #{linkedId}");
            return LegacyAccountLinkState.Linked(linked);
        }

        var match = await legacyMemberLookupRepository.FindByEmailAsync(account.Email, cancellationToken);
        if (match is null)
        {
            return LegacyAccountLinkState.None();
        }

        var takenBy = await memberAccountRepository.FindByLinkedLegacyUserIdAsync(match.UserId, cancellationToken);
        if (takenBy is not null && takenBy.Id != account.Id)
        {
            return LegacyAccountLinkState.Unavailable(match);
        }

        return LegacyAccountLinkState.Claimable(match);
    }

    /// <summary>
    /// Explicitly claims a free legacy USERS_T account that matches the member's email.
    /// Optionally adopts the legacy username as the modern display name.
    /// </summary>
    public async Task<MemberAccountResult> ClaimLegacyAccountAsync(
        Guid memberId,
        bool adoptLegacyDisplayName,
        CancellationToken cancellationToken = default)
    {
        var account = await memberAccountRepository.FindByIdAsync(memberId, cancellationToken);
        if (account is null)
        {
            return MemberAccountResult.Failure("Account not found.");
        }

        if (account.LinkedLegacyUserId is not null)
        {
            return MemberAccountResult.Success(account);
        }

        var match = await legacyMemberLookupRepository.FindByEmailAsync(account.Email, cancellationToken);
        if (match is null)
        {
            return MemberAccountResult.Failure("No matching legacy account was found for your email.");
        }

        var takenBy = await memberAccountRepository.FindByLinkedLegacyUserIdAsync(match.UserId, cancellationToken);
        if (takenBy is not null && takenBy.Id != memberId)
        {
            return MemberAccountResult.Failure(
                "That legacy account is already linked to another modern member.");
        }

        var linked = await memberAccountRepository.LinkLegacyUserIdAsync(memberId, match.UserId, cancellationToken);
        if (linked is null)
        {
            return MemberAccountResult.Failure("Account not found.");
        }

        if (adoptLegacyDisplayName && !string.IsNullOrWhiteSpace(match.Username))
        {
            var renamed = await UpdateDisplayNameAsync(memberId, match.Username, cancellationToken);
            if (renamed.Succeeded && renamed.Account is not null)
            {
                return renamed;
            }
        }

        return MemberAccountResult.Success(linked);
    }

    /// <summary>
    /// Silently links a free legacy account when the member is still unlinked and their email
    /// matches. Used to backfill accounts created before the claim UI existed.
    /// </summary>
    private async Task<MemberAccount> TryBackfillLegacyLinkAsync(
        MemberAccount account,
        CancellationToken cancellationToken)
    {
        if (account.LinkedLegacyUserId is not null)
        {
            return account;
        }

        var match = await legacyMemberLookupRepository.FindByEmailAsync(account.Email, cancellationToken);
        if (match is null)
        {
            return account;
        }

        var takenBy = await memberAccountRepository.FindByLinkedLegacyUserIdAsync(match.UserId, cancellationToken);
        if (takenBy is not null && takenBy.Id != account.Id)
        {
            return account;
        }

        return await memberAccountRepository.LinkLegacyUserIdAsync(account.Id, match.UserId, cancellationToken)
            ?? account;
    }

    /// <summary>
    /// Updates the member's display name. Names are not unique — multiple members may share
    /// the same display name (common on forums). Leading/trailing whitespace is stripped.
    /// </summary>
    public async Task<MemberAccountResult> UpdateDisplayNameAsync(Guid memberId, string displayName, CancellationToken cancellationToken = default)
    {
        var trimmed = displayName?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return MemberAccountResult.Failure("Display name is required.");
        }

        if (trimmed.Length < MinDisplayNameLength)
        {
            return MemberAccountResult.Failure($"Display name must be at least {MinDisplayNameLength} characters.");
        }

        if (trimmed.Length > MaxDisplayNameLength)
        {
            return MemberAccountResult.Failure($"Display name must be at most {MaxDisplayNameLength} characters.");
        }

        var updated = await memberAccountRepository.UpdateDisplayNameAsync(memberId, trimmed, cancellationToken);
        if (updated is null)
        {
            return MemberAccountResult.Failure("Account not found.");
        }

        return MemberAccountResult.Success(updated);
    }

    /// <summary>
    /// Validates and processes an avatar upload, stores full + thumbnail WebP blobs, and
    /// updates <see cref="MemberAccount.AvatarUrl"/>. On DB failure the new blobs are deleted
    /// and the previous avatar is left in place.
    /// </summary>
    public async Task<MemberAccountResult> UpdateAvatarAsync(
        Guid memberId,
        Stream image,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);

        var account = await memberAccountRepository.FindByIdAsync(memberId, cancellationToken);
        if (account is null)
        {
            return MemberAccountResult.Failure("Account not found.");
        }

        MemberAvatarImageProcessor.ProcessedAvatar processed;
        try
        {
            processed = await MemberAvatarImageProcessor.ProcessAsync(image, originalFileName, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return MemberAccountResult.Failure(ex.Message);
        }

        var principalKey = MemberUploadQuotaService.PrincipalKeyFromMemberId(memberId);
        var quotaBytes = Math.Max(processed.FullImage.Length, 1);
        if (!uploadQuota.TryConsume(principalKey, quotaBytes, out var quotaError))
        {
            return MemberAccountResult.Failure(quotaError ?? "Daily upload limit reached.");
        }

        await using (processed.FullImage)
        await using (processed.Thumbnail)
        {
            var avatarBlobName = MemberAvatarPaths.CreateAvatarBlobName(memberId);
            var thumbBlobName = MemberAvatarPaths.ToThumbBlobName(avatarBlobName);
            var uploadContext = new BlobUploadContext
            {
                MemberAccountId = memberId,
                PreferredBlobName = avatarBlobName,
            };
            var thumbContext = new BlobUploadContext
            {
                MemberAccountId = memberId,
                PreferredBlobName = thumbBlobName,
            };

            string? uploadedAvatar = null;
            string? uploadedThumb = null;
            try
            {
                processed.FullImage.Position = 0;
                var fullResult = await blobUploadService.UploadAsync(
                    processed.FullImage,
                    "avatar.webp",
                    MemberAvatarPaths.Container,
                    uploadContext,
                    cancellationToken);
                uploadedAvatar = fullResult.BlobName;

                processed.Thumbnail.Position = 0;
                var thumbResult = await blobUploadService.UploadAsync(
                    processed.Thumbnail,
                    "avatar-thumb.webp",
                    MemberAvatarPaths.Container,
                    thumbContext,
                    cancellationToken);
                uploadedThumb = thumbResult.BlobName;

                var previousAvatar = account.AvatarUrl;
                var updated = await memberAccountRepository.UpdateAvatarUrlAsync(
                    memberId,
                    uploadedAvatar,
                    cancellationToken);
                if (updated is null)
                {
                    await SafeDeleteAsync(uploadedAvatar, uploadedThumb, cancellationToken);
                    return MemberAccountResult.Failure("Account not found.");
                }

                // Delete previous blobs only after the DB write succeeds.
                if (!string.IsNullOrWhiteSpace(previousAvatar)
                    && !string.Equals(previousAvatar, uploadedAvatar, StringComparison.Ordinal))
                {
                    await SafeDeleteAsync(
                        previousAvatar,
                        MemberAvatarPaths.ToThumbBlobName(previousAvatar),
                        cancellationToken);
                }

                return MemberAccountResult.Success(updated);
            }
            catch (NotSupportedException ex)
            {
                await SafeDeleteAsync(uploadedAvatar, uploadedThumb, cancellationToken);
                return MemberAccountResult.Failure(ex.Message);
            }
            catch (BlobUploadException ex)
            {
                await SafeDeleteAsync(uploadedAvatar, uploadedThumb, cancellationToken);
                return MemberAccountResult.Failure(ex.Message);
            }
            catch (Exception)
            {
                await SafeDeleteAsync(uploadedAvatar, uploadedThumb, cancellationToken);
                throw;
            }
        }
    }

    public async Task<MemberAccountResult> RemoveAvatarAsync(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        var account = await memberAccountRepository.FindByIdAsync(memberId, cancellationToken);
        if (account is null)
        {
            return MemberAccountResult.Failure("Account not found.");
        }

        if (string.IsNullOrWhiteSpace(account.AvatarUrl))
        {
            return MemberAccountResult.Success(account);
        }

        var previousAvatar = account.AvatarUrl;
        var updated = await memberAccountRepository.UpdateAvatarUrlAsync(memberId, null, cancellationToken);
        if (updated is null)
        {
            return MemberAccountResult.Failure("Account not found.");
        }

        await SafeDeleteAsync(
            previousAvatar,
            MemberAvatarPaths.ToThumbBlobName(previousAvatar),
            cancellationToken);

        return MemberAccountResult.Success(updated);
    }

    private async Task SafeDeleteAsync(
        string? avatarBlobName,
        string? thumbBlobName,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(avatarBlobName))
        {
            try
            {
                await blobUploadService.DeleteAsync(MemberAvatarPaths.Container, avatarBlobName, cancellationToken);
            }
            catch
            {
                // Best-effort cleanup; do not mask the original failure.
            }
        }

        if (!string.IsNullOrWhiteSpace(thumbBlobName))
        {
            try
            {
                await blobUploadService.DeleteAsync(MemberAvatarPaths.Container, thumbBlobName, cancellationToken);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }

    public const int MinDisplayNameLength = 2;

    public const int MaxDisplayNameLength = 100;
}

public sealed record MemberAccountResult(bool Succeeded, MemberAccount? Account, string? Error)
{
    public static MemberAccountResult Success(MemberAccount account) => new(true, account, null);

    public static MemberAccountResult Failure(string error) => new(false, null, error);
}

public enum LegacyAccountLinkKind
{
    None = 0,
    Linked = 1,
    Claimable = 2,
    Unavailable = 3,
}

public sealed record LegacyAccountLinkState(
    LegacyAccountLinkKind Kind,
    LegacyMemberMatch? Match)
{
    public static LegacyAccountLinkState None() => new(LegacyAccountLinkKind.None, null);

    public static LegacyAccountLinkState Linked(LegacyMemberMatch match) =>
        new(LegacyAccountLinkKind.Linked, match);

    public static LegacyAccountLinkState Claimable(LegacyMemberMatch match) =>
        new(LegacyAccountLinkKind.Claimable, match);

    public static LegacyAccountLinkState Unavailable(LegacyMemberMatch match) =>
        new(LegacyAccountLinkKind.Unavailable, match);
}
