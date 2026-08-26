using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class DeviceTokenRepositoryTests
{
    [Fact]
    public async Task ListByMemberIdsAsync_ReturnsEachDevice_AndClonesRows()
    {
        var store = new SharedDeviceTokenStore();
        var repository = new InMemoryDeviceTokenRepository(store);
        var alice = Guid.NewGuid();
        var bob = Guid.NewGuid();
        var carol = Guid.NewGuid();

        await repository.UpsertAsync(DeviceTokenTestData.Token(alice, DevicePushPlatform.Apns, "alice-phone"));
        await repository.UpsertAsync(DeviceTokenTestData.Token(alice, DevicePushPlatform.Fcm, "alice-android"));
        await repository.UpsertAsync(DeviceTokenTestData.Token(bob, DevicePushPlatform.Apns, "bob-phone"));
        await repository.UpsertAsync(DeviceTokenTestData.Token(carol, DevicePushPlatform.Fcm, "carol-android"));

        var listed = await repository.ListByMemberIdsAsync([alice, bob, alice]);

        Assert.Equal(3, listed.Count);
        Assert.Equal(2, listed.Count(row => row.MemberAccountId == alice));
        Assert.Contains(listed, row => row.MemberAccountId == bob && row.Token == "bob-phone");
        Assert.DoesNotContain(listed, row => row.MemberAccountId == carol);

        listed[0].Token = "mutated";
        lock (store.Gate)
        {
            Assert.DoesNotContain(store.Tokens, row => row.Token == "mutated");
        }
    }

    [Fact]
    public async Task ListByMemberIdsAsync_EmptyInput_ReturnsEmpty()
    {
        var repository = new InMemoryDeviceTokenRepository(new SharedDeviceTokenStore());

        var listed = await repository.ListByMemberIdsAsync([]);

        Assert.Empty(listed);
    }
}
