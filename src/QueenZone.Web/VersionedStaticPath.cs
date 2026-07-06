using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace QueenZone.Web;

public static class VersionedStaticPath
{
    public static string Apply(IFileVersionProvider fileVersionProvider, PathString requestPathBase, string path) =>
        fileVersionProvider.AddFileVersionToPath(requestPathBase, path);
}
