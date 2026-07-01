using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace QueenZone.Web.Pages.Admin.News;

internal static class AdminNewsDeleteError
{
    private static Func<Exception?, bool> foreignKeyViolationClassifier =
        inner => inner is SqlException { Number: 547 };

    internal static bool IsForeignKeyViolation(DbUpdateException exception) =>
        foreignKeyViolationClassifier(exception.InnerException);

    internal static IDisposable UseForeignKeyViolationClassifier(Func<Exception?, bool> classifier)
    {
        var previous = foreignKeyViolationClassifier;
        foreignKeyViolationClassifier = classifier;
        return new ResetScope(previous);
    }

    private sealed class ResetScope(Func<Exception?, bool> previous) : IDisposable
    {
        public void Dispose() => foreignKeyViolationClassifier = previous;
    }
}
