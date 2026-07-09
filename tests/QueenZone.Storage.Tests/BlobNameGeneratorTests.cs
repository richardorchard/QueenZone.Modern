using QueenZone.Storage;

namespace QueenZone.Storage.Tests;

public sealed class BlobNameGeneratorTests
{
    [Fact]
    public void Uses_member_prefix_when_member_id_present()
    {
        var name = BlobNameGenerator.Create(
            "Avatar.PNG",
            new BlobUploadContext { MemberId = 42 });

        Assert.StartsWith("members/42/", name);
        Assert.EndsWith(".png", name);
    }

    [Fact]
    public void Uses_editor_prefix_when_actor_email_present()
    {
        var name = BlobNameGenerator.Create(
            "hero.jpg",
            new BlobUploadContext { ActorEmail = "Editor+News@Example.com" });

        Assert.StartsWith("editors/", name);
        Assert.Contains("editor-news-example.com", name);
        Assert.EndsWith(".jpg", name);
    }

    [Fact]
    public void Falls_back_to_anonymous_prefix()
    {
        var name = BlobNameGenerator.Create("file.webp", context: null);
        Assert.StartsWith("anonymous/", name);
        Assert.EndsWith(".webp", name);
    }
}
