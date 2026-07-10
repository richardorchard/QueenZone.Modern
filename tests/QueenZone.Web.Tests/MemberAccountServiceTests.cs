using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class MemberAccountServiceTests
{
    private static MemberAccountService CreateService(
        IMemberAccountRepository? memberAccountRepository = null,
        ILegacyMemberLookupRepository? legacyMemberLookupRepository = null) =>
        new(
            memberAccountRepository ?? new InMemoryMemberAccountRepository(),
            legacyMemberLookupRepository ?? new InMemoryLegacyMemberLookupRepository(
                new Dictionary<string, LegacyMemberMatch>()));

    [Fact]
    public async Task RegisterAsync_CreatesAccount_AndAutoLinksMatchingLegacyEmail()
    {
        var legacyLookup = new InMemoryLegacyMemberLookupRepository(new Dictionary<string, LegacyMemberMatch>
        {
            ["fan@queenzone.org"] = new LegacyMemberMatch(123, "OldFan"),
        });
        var service = CreateService(legacyMemberLookupRepository: legacyLookup);

        var result = await service.RegisterAsync("fan@queenzone.org", "S3curePass!", "New Fan");

        Assert.True(result.Succeeded);
        Assert.Equal(123, result.Account!.LinkedLegacyUserId);
        Assert.Equal("fan@queenzone.org", result.Account.Email);
    }

    [Fact]
    public async Task RegisterAsync_LeavesLinkNull_WhenNoLegacyMatch()
    {
        var service = CreateService();

        var result = await service.RegisterAsync("nobody@example.com", "S3curePass!", "Nobody");

        Assert.True(result.Succeeded);
        Assert.Null(result.Account!.LinkedLegacyUserId);
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
    public async Task FindOrCreateFromExternalLoginAsync_CreatesNewAccount_AndAutoLinksLegacyEmail()
    {
        var legacyLookup = new InMemoryLegacyMemberLookupRepository(new Dictionary<string, LegacyMemberMatch>
        {
            ["fan@queenzone.org"] = new LegacyMemberMatch(123, "OldFan"),
        });
        var service = CreateService(legacyMemberLookupRepository: legacyLookup);

        var account = await service.FindOrCreateFromExternalLoginAsync("Google", "google-subject-1", "fan@queenzone.org", "Fan");

        Assert.Equal(123, account.LinkedLegacyUserId);
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
}
