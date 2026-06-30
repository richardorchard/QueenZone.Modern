using Microsoft.AspNetCore.Identity;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web;

/// <summary>
/// Shared find-or-link-or-create logic for both the native Queenzone.org signup and every
/// external provider's first sign-in. Linking to a legacy USERS_T account is automatic and
/// silent, by email match only — the legacy PASSWORD column is never read or verified.
/// </summary>
public sealed class MemberAccountService(
    IMemberAccountRepository memberAccountRepository,
    ILegacyMemberLookupRepository legacyMemberLookupRepository)
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

        return MemberAccountResult.Success(account);
    }

    public async Task<MemberAccount> FindOrCreateFromExternalLoginAsync(
        string provider,
        string providerKey,
        string email,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var existingByLogin = await memberAccountRepository.FindByExternalLoginAsync(provider, providerKey, cancellationToken);
        if (existingByLogin is not null)
        {
            return existingByLogin;
        }

        var existingByEmail = await memberAccountRepository.FindByEmailAsync(email, cancellationToken);
        if (existingByEmail is not null)
        {
            await memberAccountRepository.AddExternalLoginAsync(existingByEmail.Id, provider, providerKey, email, cancellationToken);
            return existingByEmail;
        }

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
        return created;
    }
}

public sealed record MemberAccountResult(bool Succeeded, MemberAccount? Account, string? Error)
{
    public static MemberAccountResult Success(MemberAccount account) => new(true, account, null);

    public static MemberAccountResult Failure(string error) => new(false, null, error);
}
