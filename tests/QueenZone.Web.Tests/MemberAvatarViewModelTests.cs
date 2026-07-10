using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class MemberAvatarViewModelTests
{
    [Fact]
    public void Initials_UsesFirstLetterOfDisplayName()
    {
        var model = new MemberAvatarViewModel { DisplayName = "freddie" };

        Assert.Equal("F", model.Initials);
        Assert.Equal("freddie's avatar", model.AltText);
        Assert.Null(model.ImageUrl);
    }

    [Fact]
    public void ImageUrl_UsesProxyPath_WhenMemberHasAvatar()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var model = new MemberAvatarViewModel
        {
            DisplayName = "Fan",
            MemberId = id,
            HasAvatar = true,
            UseThumbnail = true,
        };

        Assert.Equal($"/account/avatar/{id:D}?size=thumb", model.ImageUrl);
    }

    [Fact]
    public void ImageUrl_IsNull_WhenNoAvatarEvenWithMemberId()
    {
        var model = new MemberAvatarViewModel
        {
            DisplayName = "Fan",
            MemberId = Guid.NewGuid(),
            HasAvatar = false,
        };

        Assert.Null(model.ImageUrl);
        Assert.Equal("F", model.Initials);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Initials_FallsBackWhenDisplayNameBlank(string name)
    {
        var model = new MemberAvatarViewModel { DisplayName = name };
        Assert.Equal("?", model.Initials);
    }
}
