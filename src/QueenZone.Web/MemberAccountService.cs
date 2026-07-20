using Microsoft.AspNetCore.Identity;
using QueenZone.Data;
using QueenZone.Data.Entities;
using QueenZone.Storage;

namespace QueenZone.Web;

/// <summary>
/// Shared find-or-link-or-create logic for both the native Queenzone.org signup and every
/// external provider's first sign-in. Linking to a legacy USERS_T account is automatic and
/// silent, by email match only — the legacy PASSWORD column is never read or verified.
/// </summary>
public sealed class MemberAccountService(
    IMemberAccountRepository memberAccountRepository,
    ILegacyMemberLookupRepository legacyMemberLookupRepository,
    IBlobUploadService blobUploadService)
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
            LinkedLegacyUserId = (await legacyMemberLookupRepository.FindByEmailAsync(email, cancellationToken))?.UserId,
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
                    LinkedLegacyUserId = (await legacyMemberLookupRepository.FindByEmailAsync(email, cancellationToken))?.UserId,
                };

                var created = await memberAccountRepository.CreateAsync(account, cancellationToken);
                await memberAccountRepository.AddExternalLoginAsync(created.Id, provider, providerKey, email, cancellationToken);
                result = created;
            }
        }

        await memberAccountRepository.RecordLoginAsync(result.Id, DateTime.UtcNow, cancellationToken);
        return result;
    }

    public async Task<MemberAccount?> FindByIdAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        await memberAccountRepository.FindByIdAsync(memberId, cancellationToken);

    public async Task<IReadOnlyList<string>> ListExternalProvidersAsync(Guid memberId, CancellationToken cancellationToken = default) =>
        await memberAccountRepository.ListExternalProvidersAsync(memberId, cancellationToken);

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
