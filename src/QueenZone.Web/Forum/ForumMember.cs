using System.Security.Claims;

namespace QueenZone.Web;

public static class ForumMember
{
    public static Guid? GetMemberId(ClaimsPrincipal? user)
    {
        if (user is null)
        {
            return null;
        }

        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
