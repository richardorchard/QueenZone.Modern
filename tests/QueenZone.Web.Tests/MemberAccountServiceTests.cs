using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.Data.Entities;
using QueenZone.Storage;
using QueenZone.Web;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace QueenZone.Web.Tests;

public sealed class MemberAccountServiceTests
{
    private static MemberAccountService CreateService(
        IMemberAccountRepository? memberAccountRepository = null,
        ILegacyMemberLookupRepository? legacyMemberLookupRepository = null,
        IBlobUploadService? blobUploadService = null,
        InMemoryBlobStorageBackend? blobBackend = null,
        MemberUploadQuotaService? uploadQuota = null)
    {
        var backend = blobBackend ?? new InMemoryBlobStorageBackend();
        var blobs = blobUploadService
            ?? new AzureBlobUploadService(backend, Options.Create(new BlobUploadOptions()));
        return new(
            memberAccountRepository ?? new InMemoryMemberAccountRepository(),
            legacyMemberLookupRepository ?? new InMemoryLegacyMemberLookupRepository(
                new Dictionary<string, LegacyMemberMatch>()),
            blobs,
            uploadQuota ?? CreateDisabledUploadQuota());
    }

    private static MemberUploadQuotaService CreateDisabledUploadQuota() =>
        new(
            new Microsoft.Extensions.Caching.Memory.MemoryCache(
                new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
            TimeProvider.System,
            Options.Create(new UploadQuotaOptions { Enabled = false }));

    private static async Task<MemoryStream> CreatePngAsync(int width = 32, int height = 32)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(40, 120, 200));
        var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public async Task RegisterAsync_CreatesAccount_WithoutSilentLegacyLink()
    {
        var legacyLookup = new InMemoryLegacyMemberLookupRepository(new Dictionary<string, LegacyMemberMatch>
        {
            ["fan@queenzone.org"] = new LegacyMemberMatch(123, "OldFan"),
        });
        var service = CreateService(legacyMemberLookupRepository: legacyLookup);

        var result = await service.RegisterAsync("fan@queenzone.org", "S3curePass!", "New Fan");

        Assert.True(result.Succeeded);
        Assert.Null(result.Account!.LinkedLegacyUserId);
        Assert.Equal("fan@queenzone.org", result.Account.Email);

        var state = await service.GetLegacyLinkStateAsync(result.Account);
        Assert.Equal(LegacyAccountLinkKind.Claimable, state.Kind);
        Assert.Equal(123, state.Match!.UserId);
        Assert.Equal("OldFan", state.Match.Username);
    }

    [Fact]
    public async Task RegisterAsync_LeavesLinkNull_WhenNoLegacyMatch()
    {
        var service = CreateService();

        var result = await service.RegisterAsync("nobody@example.com", "S3curePass!", "Nobody");

        Assert.True(result.Succeeded);
        Assert.Null(result.Account!.LinkedLegacyUserId);

        var state = await service.GetLegacyLinkStateAsync(result.Account);
        Assert.Equal(LegacyAccountLinkKind.None, state.Kind);
    }

    [Fact]
    public async Task RegisterAsync_Fails_WhenEmailAlreadyRegistered()
    {
        var service = CreateService();
        await service.RegisterAsync("fan@queenzone.org", "S3curePass!", "Fan");

        var result = await service.RegisterAsync("fan@queenzone.org", "DifferentPass!", "Fan Again");

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task SignInAsync_Succeeds_WithCorrectPassword()
    {
        var service = CreateService();
        await service.RegisterAsync("fan@queenzone.org", "S3curePass!", "Fan");

        var result = await service.SignInAsync("fan@queenzone.org", "S3curePass!");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task SignInAsync_Fails_WithWrongPassword()
    {
        var service = CreateService();
        await service.RegisterAsync("fan@queenzone.org", "S3curePass!", "Fan");

        var result = await service.SignInAsync("fan@queenzone.org", "WrongPassword!");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task SignInAsync_Fails_WhenAccountDoesNotExist()
    {
        var service = CreateService();

        var result = await service.SignInAsync("ghost@example.com", "whatever");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task SignInAsync_Fails_WhenAccountIsSuspended()
    {
        var repository = new InMemoryMemberAccountRepository();
        var service = CreateService(memberAccountRepository: repository);
        var registered = await service.RegisterAsync("fan@queenzone.org", "S3curePass!", "Fan");
        await repository.SuspendAsync(registered.Account!.Id, "Spamming the board", "admin@queenzone.org", DateTime.UtcNow);

        var result = await service.SignInAsync("fan@queenzone.org", "S3curePass!");

        Assert.False(result.Succeeded);
        Assert.Equal(MemberAccountService.SuspendedSignInError, result.Error);
    }

    [Fact]
    public async Task FindOrCreateFromExternalLoginAsync_CreatesNewAccount_WithoutSilentLegacyLink()
    {
        var legacyLookup = new InMemoryLegacyMemberLookupRepository(new Dictionary<string, LegacyMemberMatch>
        {
            ["fan@queenzone.org"] = new LegacyMemberMatch(123, "OldFan"),
        });
        var service = CreateService(legacyMemberLookupRepository: legacyLookup);

        var account = await service.FindOrCreateFromExternalLoginAsync("Google", "google-subject-1", "fan@queenzone.org", "Fan");

        Assert.Null(account.LinkedLegacyUserId);
        var state = await service.GetLegacyLinkStateAsync(account);
        Assert.Equal(LegacyAccountLinkKind.Claimable, state.Kind);
    }

    [Fact]
    public async Task SignInAsync_BackfillsLegacyLink_ForExistingUnlinkedAccount()
    {
        var legacyLookup = new InMemoryLegacyMemberLookupRepository(new Dictionary<string, LegacyMemberMatch>
        {
            ["fan@queenzone.org"] = new LegacyMemberMatch(123, "OldFan"),
        });
        var service = CreateService(legacyMemberLookupRepository: legacyLookup);
        await service.RegisterAsync("fan@queenzone.org", "S3curePass!", "Fan");

        var signedIn = await service.SignInAsync("fan@queenzone.org", "S3curePass!");

        Assert.True(signedIn.Succeeded);
        Assert.Equal(123, signedIn.Account!.LinkedLegacyUserId);
    }

    [Fact]
    public async Task ClaimLegacyAccountAsync_LinksAndOptionallyAdoptsDisplayName()
    {
        var legacyLookup = new InMemoryLegacyMemberLookupRepository(new Dictionary<string, LegacyMemberMatch>
        {
            ["fan@queenzone.org"] = new LegacyMemberMatch(123, "OldFan"),
        });
        var service = CreateService(legacyMemberLookupRepository: legacyLookup);
        var registered = await service.RegisterAsync("fan@queenzone.org", "S3curePass!", "New Fan");

        var claimed = await service.ClaimLegacyAccountAsync(
            registered.Account!.Id,
            legacyUserId: 123,
            adoptLegacyDisplayName: true);

        Assert.True(claimed.Succeeded);
        Assert.Equal(123, claimed.Account!.LinkedLegacyUserId);
        Assert.Equal("OldFan", claimed.Account.DisplayName);

        var state = await service.GetLegacyLinkStateAsync(claimed.Account);
        Assert.Equal(LegacyAccountLinkKind.Linked, state.Kind);
        Assert.Equal("OldFan", state.Match!.Username);
    }

    [Fact]
    public async Task ClaimLegacyAccountAsync_Fails_WhenLegacyIdAlreadyTaken()
    {
        var legacyLookup = new InMemoryLegacyMemberLookupRepository(new Dictionary<string, LegacyMemberMatch>
        {
            ["first@queenzone.org"] = new LegacyMemberMatch(123, "OldFan"),
            ["second@queenzone.org"] = new LegacyMemberMatch(123, "OldFan"),
        });
        var service = CreateService(legacyMemberLookupRepository: legacyLookup);
        var first = await service.RegisterAsync("first@queenzone.org", "S3curePass!", "First");
        Assert.True((await service.ClaimLegacyAccountAsync(
            first.Account!.Id,
            legacyUserId: 123,
            adoptLegacyDisplayName: false)).Succeeded);

        var second = await service.RegisterAsync("second@queenzone.org", "S3curePass!", "Second");
        var conflict = await service.ClaimLegacyAccountAsync(
            second.Account!.Id,
            legacyUserId: 123,
            adoptLegacyDisplayName: false);

        Assert.False(conflict.Succeeded);
        Assert.Contains("already linked", conflict.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnlinkLegacyAccountAsync_ClearsLink_AndAllowsReclaim()
    {
        var legacyLookup = new InMemoryLegacyMemberLookupRepository(
            new Dictionary<string, IReadOnlyList<LegacyMemberMatch>>(StringComparer.OrdinalIgnoreCase)
            {
                ["fan@queenzone.org"] =
                [
                    new LegacyMemberMatch(100, "WrongFan"),
                    new LegacyMemberMatch(200, "RightFan"),
                ],
            });
        var service = CreateService(legacyMemberLookupRepository: legacyLookup);
        var registered = await service.RegisterAsync("fan@queenzone.org", "S3curePass!", "New Fan");
        Assert.True((await service.ClaimLegacyAccountAsync(
            registered.Account!.Id,
            legacyUserId: 100,
            adoptLegacyDisplayName: false)).Succeeded);

        var unlinked = await service.UnlinkLegacyAccountAsync(registered.Account.Id);

        Assert.True(unlinked.Succeeded);
        Assert.Null(unlinked.Account!.LinkedLegacyUserId);

        var state = await service.GetLegacyLinkStateAsync(unlinked.Account);
        Assert.Equal(LegacyAccountLinkKind.Claimable, state.Kind);
        Assert.True(state.HasMultipleClaimable);

        var reclaimed = await service.ClaimLegacyAccountAsync(
            registered.Account.Id,
            legacyUserId: 200,
            adoptLegacyDisplayName: true);

        Assert.True(reclaimed.Succeeded);
        Assert.Equal(200, reclaimed.Account!.LinkedLegacyUserId);
        Assert.Equal("RightFan", reclaimed.Account.DisplayName);
    }

    [Fact]
    public async Task UnlinkLegacyAccountAsync_IsNoOp_WhenAlreadyUnlinked()
    {
        var service = CreateService();
        var registered = await service.RegisterAsync("fan@queenzone.org", "S3curePass!", "Fan");

        var result = await service.UnlinkLegacyAccountAsync(registered.Account!.Id);

        Assert.True(result.Succeeded);
        Assert.Null(result.Account!.LinkedLegacyUserId);
    }

    [Fact]
    public async Task UnlinkLegacyAccountAsync_Fails_WhenAccountDoesNotExist()
    {
        var service = CreateService();

        var result = await service.UnlinkLegacyAccountAsync(Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal("Account not found.", result.Error);
    }

    [Fact]
    public async Task GetLegacyLinkStateAsync_ReturnsMultipleClaimable_WhenEmailMatchesSeveralFreeAccounts()
    {
        var legacyLookup = new InMemoryLegacyMemberLookupRepository(
            new Dictionary<string, IReadOnlyList<LegacyMemberMatch>>(StringComparer.OrdinalIgnoreCase)
            {
                ["fan@queenzone.org"] =
                [
                    new LegacyMemberMatch(200, "BetaFan"),
                    new LegacyMemberMatch(100, "AlphaFan"),
                ],
            });
        var service = CreateService(legacyMemberLookupRepository: legacyLookup);
        var registered = await service.RegisterAsync("fan@queenzone.org", "S3curePass!", "New Fan");

        var state = await service.GetLegacyLinkStateAsync(registered.Account!);

        Assert.Equal(LegacyAccountLinkKind.Claimable, state.Kind);
        Assert.True(state.HasMultipleClaimable);
        Assert.Null(state.Match);
        Assert.Equal([100, 200], state.ClaimableMatches.Select(m => m.UserId).ToArray());
        Assert.Equal(["AlphaFan", "BetaFan"], state.ClaimableMatches.Select(m => m.Username).ToArray());
    }

    [Fact]
    public async Task ClaimLegacyAccountAsync_ClaimsChosenMatch_WhenMultipleShareEmail()
    {
        var legacyLookup = new InMemoryLegacyMemberLookupRepository(
            new Dictionary<string, IReadOnlyList<LegacyMemberMatch>>(StringComparer.OrdinalIgnoreCase)
            {
                ["fan@queenzone.org"] =
                [
                    new LegacyMemberMatch(100, "AlphaFan"),
                    new LegacyMemberMatch(200, "BetaFan"),
                ],
            });
        var service = CreateService(legacyMemberLookupRepository: legacyLookup);
        var registered = await service.RegisterAsync("fan@queenzone.org", "S3curePass!", "New Fan");

        var claimed = await service.ClaimLegacyAccountAsync(
            registered.Account!.Id,
            legacyUserId: 200,
            adoptLegacyDisplayName: true);

        Assert.True(claimed.Succeeded);
        Assert.Equal(200, claimed.Account!.LinkedLegacyUserId);
        Assert.Equal("BetaFan", claimed.Account.DisplayName);
    }

    [Fact]
    public async Task ClaimLegacyAccountAsync_Fails_WhenChosenIdDoesNotMatchEmail()
    {
        var legacyLookup = new InMemoryLegacyMemberLookupRepository(
            new Dictionary<string, IReadOnlyList<LegacyMemberMatch>>(StringComparer.OrdinalIgnoreCase)
            {
                ["fan@queenzone.org"] = [new LegacyMemberMatch(100, "AlphaFan")],
                ["other@queenzone.org"] = [new LegacyMemberMatch(999, "OtherFan")],
            });
        var service = CreateService(legacyMemberLookupRepository: legacyLookup);
        var registered = await service.RegisterAsync("fan@queenzone.org", "S3curePass!", "New Fan");

        var result = await service.ClaimLegacyAccountAsync(
            registered.Account!.Id,
            legacyUserId: 999,
            adoptLegacyDisplayName: false);

        Assert.False(result.Succeeded);
        Assert.Contains("does not match", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Null((await service.FindByIdAsync(registered.Account.Id))!.LinkedLegacyUserId);
    }

    [Fact]
    public async Task SignInAsync_DoesNotBackfill_WhenMultipleFreeLegacyMatches()
    {
        var legacyLookup = new InMemoryLegacyMemberLookupRepository(
            new Dictionary<string, IReadOnlyList<LegacyMemberMatch>>(StringComparer.OrdinalIgnoreCase)
            {
                ["fan@queenzone.org"] =
                [
                    new LegacyMemberMatch(100, "AlphaFan"),
                    new LegacyMemberMatch(200, "BetaFan"),
                ],
            });
        var service = CreateService(legacyMemberLookupRepository: legacyLookup);
        await service.RegisterAsync("fan@queenzone.org", "S3curePass!", "Fan");

        var signedIn = await service.SignInAsync("fan@queenzone.org", "S3curePass!");

        Assert.True(signedIn.Succeeded);
        Assert.Null(signedIn.Account!.LinkedLegacyUserId);

        var state = await service.GetLegacyLinkStateAsync(signedIn.Account);
        Assert.Equal(LegacyAccountLinkKind.Claimable, state.Kind);
        Assert.True(state.HasMultipleClaimable);
    }

    [Fact]
    public async Task GetLegacyLinkStateAsync_SplitsFreeAndTakenMatches()
    {
        var legacyLookup = new InMemoryLegacyMemberLookupRepository(
            new Dictionary<string, IReadOnlyList<LegacyMemberMatch>>(StringComparer.OrdinalIgnoreCase)
            {
                ["shared@queenzone.org"] =
                [
                    new LegacyMemberMatch(100, "FreeFan"),
                    new LegacyMemberMatch(200, "TakenFan"),
                ],
                ["other@queenzone.org"] = [new LegacyMemberMatch(200, "TakenFan")],
            });
        var service = CreateService(legacyMemberLookupRepository: legacyLookup);
        var other = await service.RegisterAsync("other@queenzone.org", "S3curePass!", "Other");
        Assert.True((await service.ClaimLegacyAccountAsync(
            other.Account!.Id,
            legacyUserId: 200,
            adoptLegacyDisplayName: false)).Succeeded);

        var shared = await service.RegisterAsync("shared@queenzone.org", "S3curePass!", "Shared");
        var state = await service.GetLegacyLinkStateAsync(shared.Account!);

        Assert.Equal(LegacyAccountLinkKind.Claimable, state.Kind);
        Assert.False(state.HasMultipleClaimable);
        Assert.Single(state.ClaimableMatches);
        Assert.Equal(100, state.ClaimableMatches[0].UserId);
        Assert.Single(state.UnavailableMatches);
        Assert.Equal(200, state.UnavailableMatches[0].UserId);
    }

    [Fact]
    public async Task FindOrCreateFromExternalLoginAsync_ReturnsSameAccount_OnSecondLoginWithSameProviderKey()
    {
        var service = CreateService();

        var first = await service.FindOrCreateFromExternalLoginAsync("Google", "google-subject-1", "fan@queenzone.org", "Fan");
        var second = await service.FindOrCreateFromExternalLoginAsync("Google", "google-subject-1", "fan@queenzone.org", "Fan");

        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task FindOrCreateFromExternalLoginAsync_LinksToExistingNativeAccount_WhenEmailMatches()
    {
        var service = CreateService();
        var registered = await service.RegisterAsync("fan@queenzone.org", "S3curePass!", "Fan");

        var externalAccount = await service.FindOrCreateFromExternalLoginAsync("Google", "google-subject-1", "fan@queenzone.org", "Fan");

        Assert.Equal(registered.Account!.Id, externalAccount.Id);
    }

    [Fact]
    public async Task UpdateDisplayNameAsync_PersistsNewName()
    {
        var service = CreateService();
        var registered = await service.RegisterAsync("fan@queenzone.org", "S3curePass!", "Original Name");

        var result = await service.UpdateDisplayNameAsync(registered.Account!.Id, "  New Name  ");

        Assert.True(result.Succeeded);
        Assert.Equal("New Name", result.Account!.DisplayName);

        var reloaded = await service.FindByIdAsync(registered.Account.Id);
        Assert.Equal("New Name", reloaded!.DisplayName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateDisplayNameAsync_RejectsEmptyOrWhitespace(string? displayName)
    {
        var service = CreateService();
        var registered = await service.RegisterAsync("fan@queenzone.org", "S3curePass!", "Original Name");

        var result = await service.UpdateDisplayNameAsync(registered.Account!.Id, displayName!);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
        Assert.Null(result.Account);

        var reloaded = await service.FindByIdAsync(registered.Account.Id);
        Assert.Equal("Original Name", reloaded!.DisplayName);
    }

    [Fact]
    public async Task UpdateDisplayNameAsync_RejectsTooShortName()
    {
        var service = CreateService();
        var registered = await service.RegisterAsync("fan@queenzone.org", "S3curePass!", "Original Name");

        var result = await service.UpdateDisplayNameAsync(registered.Account!.Id, "A");

        Assert.False(result.Succeeded);
        Assert.Contains("at least", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateDisplayNameAsync_RejectsTooLongName()
    {
        var service = CreateService();
        var registered = await service.RegisterAsync("fan@queenzone.org", "S3curePass!", "Original Name");
        var tooLong = new string('x', MemberAccountService.MaxDisplayNameLength + 1);

        var result = await service.UpdateDisplayNameAsync(registered.Account!.Id, tooLong);

        Assert.False(result.Succeeded);
        Assert.Contains("at most", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateDisplayNameAsync_AllowsDuplicateDisplayNames()
    {
        var service = CreateService();
        await service.RegisterAsync("fan1@queenzone.org", "S3curePass!", "Shared Name");
        var second = await service.RegisterAsync("fan2@queenzone.org", "S3curePass!", "Other Name");

        var result = await service.UpdateDisplayNameAsync(second.Account!.Id, "Shared Name");

        Assert.True(result.Succeeded);
        Assert.Equal("Shared Name", result.Account!.DisplayName);
    }

    [Fact]
    public async Task UpdateDisplayNameAsync_Fails_WhenAccountDoesNotExist()
    {
        var service = CreateService();

        var result = await service.UpdateDisplayNameAsync(Guid.NewGuid(), "Nobody");

        Assert.False(result.Succeeded);
        Assert.Equal("Account not found.", result.Error);
        Assert.Null(result.Account);
    }

    [Fact]
    public async Task ListExternalProvidersAsync_ReturnsLinkedProviders()
    {
        var service = CreateService();
        var account = await service.FindOrCreateFromExternalLoginAsync(
            "Google", "google-providers-1", "providers@example.com", "Provider Fan");
        await service.FindOrCreateFromExternalLoginAsync(
            "GitHub", "github-providers-1", "providers@example.com", "Provider Fan");

        var providers = await service.ListExternalProvidersAsync(account.Id);

        Assert.Equal(["GitHub", "Google"], providers);
    }

    [Fact]
    public async Task ListExternalProvidersAsync_ReturnsEmpty_WhenNoneLinked()
    {
        var service = CreateService();
        var registered = await service.RegisterAsync("native@example.com", "S3curePass!", "Native Fan");

        var providers = await service.ListExternalProvidersAsync(registered.Account!.Id);

        Assert.Empty(providers);
    }

    [Fact]
    public async Task UpdateAvatarAsync_PersistsBlobPath_AndStoresFullAndThumb()
    {
        var backend = new InMemoryBlobStorageBackend();
        var service = CreateService(blobBackend: backend);
        var registered = await service.RegisterAsync("avatar@example.com", "S3curePass!", "Avatar Fan");
        await using var png = await CreatePngAsync(120, 80);

        var result = await service.UpdateAvatarAsync(registered.Account!.Id, png, "photo.png");

        Assert.True(result.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(result.Account!.AvatarUrl));
        Assert.StartsWith($"members/{registered.Account.Id:N}/avatar-", result.Account.AvatarUrl);
        Assert.EndsWith(".webp", result.Account.AvatarUrl);
        Assert.True(backend.Exists(MemberAvatarPaths.Container, result.Account.AvatarUrl!));
        Assert.True(backend.Exists(
            MemberAvatarPaths.Container,
            MemberAvatarPaths.ToThumbBlobName(result.Account.AvatarUrl!)));
    }

    [Fact]
    public async Task RequestDeletionAsync_PreservesAccountDuringCoolingOff_AndCanCancel()
    {
        var backend = new InMemoryBlobStorageBackend();
        var repository = new InMemoryMemberAccountRepository();
        var service = CreateService(memberAccountRepository: repository, blobBackend: backend);
        var registered = await service.RegisterAsync(
            "delete-service@example.com",
            "S3curePass!",
            "Delete Service");
        await using var png = await CreatePngAsync();
        var uploaded = await service.UpdateAvatarAsync(registered.Account!.Id, png, "avatar.png");
        var avatarPath = uploaded.Account!.AvatarUrl!;
        var thumbPath = MemberAvatarPaths.ToThumbBlobName(avatarPath);

        var result = await service.RequestDeletionAsync(registered.Account.Id);
        var signIn = await service.SignInAsync("delete-service@example.com", "S3curePass!");

        Assert.True(result.Succeeded);
        Assert.Equal("Delete Service", result.Account!.DisplayName);
        Assert.NotNull(result.Account.DeletionRequestedAt);
        Assert.False(result.Account.IsSuspended);
        Assert.True(backend.Exists(MemberAvatarPaths.Container, avatarPath));
        Assert.True(backend.Exists(MemberAvatarPaths.Container, thumbPath));
        Assert.True(signIn.Succeeded);

        var cancelled = await service.CancelDeletionAsync(registered.Account.Id);

        Assert.True(cancelled.Succeeded);
        Assert.Null(cancelled.Account!.DeletionRequestedAt);
    }

    [Fact]
    public async Task PendingDeletion_ExternalSignInRemainsAvailable()
    {
        var repository = new InMemoryMemberAccountRepository();
        var service = CreateService(memberAccountRepository: repository);
        var account = await service.FindOrCreateFromExternalLoginAsync(
            "Google",
            "deleted-google-subject",
            "deleted-external@example.com",
            "External Delete");
        await service.RequestDeletionAsync(account.Id);

        var returned = await service.FindOrCreateFromExternalLoginAsync(
            "GitHub",
            "deleted-github-subject",
            "deleted-external@example.com",
            "External Delete");

        Assert.Equal(account.Id, returned.Id);
        Assert.False(returned.IsSuspended);
        Assert.NotNull(returned.LastLoginAt);
        Assert.Equal(["GitHub", "Google"], await service.ListExternalProvidersAsync(account.Id));
    }

    [Fact]
    public async Task UpdateAvatarAsync_RejectsOversizedFile_BeforeUpload()
    {
        var backend = new InMemoryBlobStorageBackend();
        var service = CreateService(blobBackend: backend);
        var registered = await service.RegisterAsync("big@example.com", "S3curePass!", "Big Fan");
        // Oversized payload that is not a valid image — rejected on size before blob upload.
        await using var oversized = new MemoryStream(new byte[MemberAvatarPaths.MaxUploadBytes + 1]);

        var result = await service.UpdateAvatarAsync(registered.Account!.Id, oversized, "huge.png");

        Assert.False(result.Succeeded);
        Assert.Contains("MB", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Null(result.Account);
        var reloaded = await service.FindByIdAsync(registered.Account.Id);
        Assert.Null(reloaded!.AvatarUrl);
        Assert.Null(backend.TryGet(MemberAvatarPaths.Container, "members/any"));
    }

    [Fact]
    public async Task UpdateAvatarAsync_RejectsNonImageFile_BeforeUpload()
    {
        var backend = new InMemoryBlobStorageBackend();
        var service = CreateService(blobBackend: backend);
        var registered = await service.RegisterAsync("txt@example.com", "S3curePass!", "Text Fan");
        await using var notImage = new MemoryStream("not-an-image"u8.ToArray());

        var result = await service.UpdateAvatarAsync(registered.Account!.Id, notImage, "notes.txt");

        Assert.False(result.Succeeded);
        Assert.Contains("JPEG", result.Error, StringComparison.OrdinalIgnoreCase);
        var reloaded = await service.FindByIdAsync(registered.Account.Id);
        Assert.Null(reloaded!.AvatarUrl);
    }

    [Fact]
    public async Task UpdateAvatarAsync_CleansUpNewBlobs_WhenDbSaveFails_AndKeepsOldAvatar()
    {
        var backend = new InMemoryBlobStorageBackend();
        var realRepo = new InMemoryMemberAccountRepository();
        var service = CreateService(memberAccountRepository: realRepo, blobBackend: backend);
        var registered = await service.RegisterAsync("keep@example.com", "S3curePass!", "Keep Fan");
        await using var first = await CreatePngAsync();
        var firstResult = await service.UpdateAvatarAsync(registered.Account!.Id, first, "one.png");
        Assert.True(firstResult.Succeeded);
        var oldPath = firstResult.Account!.AvatarUrl!;
        var oldThumb = MemberAvatarPaths.ToThumbBlobName(oldPath);
        Assert.True(backend.Exists(MemberAvatarPaths.Container, oldPath));

        var failingRepo = new FailingUpdateAvatarRepository(realRepo);
        var failingService = CreateService(memberAccountRepository: failingRepo, blobBackend: backend);
        await using var second = await CreatePngAsync(64, 64);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            failingService.UpdateAvatarAsync(registered.Account.Id, second, "two.png"));

        Assert.True(backend.Exists(MemberAvatarPaths.Container, oldPath));
        Assert.True(backend.Exists(MemberAvatarPaths.Container, oldThumb));
        var reloaded = await realRepo.FindByIdAsync(registered.Account.Id);
        Assert.Equal(oldPath, reloaded!.AvatarUrl);
    }

    /// <summary>
    /// Delegates reads to the inner repo but throws on avatar URL writes to simulate DB failure.
    /// </summary>
    private sealed class FailingUpdateAvatarRepository(IMemberAccountRepository inner) : IMemberAccountRepository
    {
        public Task<MemberAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            inner.FindByEmailAsync(email, cancellationToken);

        public Task<MemberAccount?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            inner.FindByIdAsync(id, cancellationToken);

        public Task<MemberAccount?> FindByExternalLoginAsync(string provider, string providerKey, CancellationToken cancellationToken = default) =>
            inner.FindByExternalLoginAsync(provider, providerKey, cancellationToken);

        public Task<IReadOnlyList<string>> ListExternalProvidersAsync(Guid memberAccountId, CancellationToken cancellationToken = default) =>
            inner.ListExternalProvidersAsync(memberAccountId, cancellationToken);

        public Task<MemberAccount> CreateAsync(MemberAccount account, CancellationToken cancellationToken = default) =>
            inner.CreateAsync(account, cancellationToken);

        public Task AddExternalLoginAsync(Guid memberAccountId, string provider, string providerKey, string email, CancellationToken cancellationToken = default) =>
            inner.AddExternalLoginAsync(memberAccountId, provider, providerKey, email, cancellationToken);

        public Task<MemberAccount?> UpdateDisplayNameAsync(Guid memberId, string displayName, CancellationToken cancellationToken = default) =>
            inner.UpdateDisplayNameAsync(memberId, displayName, cancellationToken);

        public Task<MemberAccount?> UpdateAvatarUrlAsync(Guid memberId, string? avatarBlobPath, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated database failure.");

        public Task<MemberAccount?> FindByLinkedLegacyUserIdAsync(int legacyUserId, CancellationToken cancellationToken = default) =>
            inner.FindByLinkedLegacyUserIdAsync(legacyUserId, cancellationToken);

        public Task<MemberAccount?> LinkLegacyUserIdAsync(Guid memberId, int legacyUserId, CancellationToken cancellationToken = default) =>
            inner.LinkLegacyUserIdAsync(memberId, legacyUserId, cancellationToken);

        public Task<MemberAccount?> UnlinkLegacyUserIdAsync(Guid memberId, CancellationToken cancellationToken = default) =>
            inner.UnlinkLegacyUserIdAsync(memberId, cancellationToken);

        public Task RecordLoginAsync(Guid memberId, DateTime loginAt, CancellationToken cancellationToken = default) =>
            inner.RecordLoginAsync(memberId, loginAt, cancellationToken);

        public Task<MemberStats> GetStatsAsync(DateTime utcNow, CancellationToken cancellationToken = default) =>
            inner.GetStatsAsync(utcNow, cancellationToken);

        public Task<IReadOnlyList<RecentLogin>> GetRecentLoginsAsync(int count, CancellationToken cancellationToken = default) =>
            inner.GetRecentLoginsAsync(count, cancellationToken);

        public Task<IReadOnlyList<DailyRegistration>> GetDailyRegistrationsAsync(DateOnly fromDate, CancellationToken cancellationToken = default) =>
            inner.GetDailyRegistrationsAsync(fromDate, cancellationToken);

        public Task<IReadOnlyList<MemberRecipientMatch>> SearchByDisplayNameAsync(
            string query,
            Guid? excludeMemberId = null,
            int maxResults = PrivateMessageLimits.MaxRecipientSearchResults,
            CancellationToken cancellationToken = default) =>
            inner.SearchByDisplayNameAsync(query, excludeMemberId, maxResults, cancellationToken);

        public Task<MemberSearchResult> SearchMembersAsync(
            string? query,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            inner.SearchMembersAsync(query, pageNumber, pageSize, cancellationToken);

        public Task<MemberAccount?> SuspendAsync(
            Guid memberId,
            string reason,
            string suspendedByAdminEmail,
            DateTime suspendedAt,
            CancellationToken cancellationToken = default) =>
            inner.SuspendAsync(memberId, reason, suspendedByAdminEmail, suspendedAt, cancellationToken);

        public Task<MemberAccount?> ReinstateAsync(Guid memberId, CancellationToken cancellationToken = default) =>
            inner.ReinstateAsync(memberId, cancellationToken);

        public Task<MemberAccountDeletionRequestResult?> RequestDeletionAsync(
            Guid memberId,
            DateTime requestedAt,
            CancellationToken cancellationToken = default) =>
            inner.RequestDeletionAsync(memberId, requestedAt, cancellationToken);

        public Task<MemberAccount?> CancelDeletionAsync(
            Guid memberId,
            DateTime cancelledAt,
            CancellationToken cancellationToken = default) =>
            inner.CancelDeletionAsync(memberId, cancelledAt, cancellationToken);

        public Task<MemberAccountDeletionPurgeResult> PurgeDeletedAccountsAsync(
            DateTime purgeBefore,
            DateTime purgedAt,
            CancellationToken cancellationToken = default) =>
            inner.PurgeDeletedAccountsAsync(purgeBefore, purgedAt, cancellationToken);
    }
}
