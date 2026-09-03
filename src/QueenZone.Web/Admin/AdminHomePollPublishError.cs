using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web;

internal static class AdminHomePollPublishError
{
    internal const string Message =
        "Could not publish this poll. The previous Home poll is unchanged. Retry publish.";

    internal static bool IsPersistenceFailure(Exception exception)
    {
        if (exception is DbUpdateException)
        {
            return true;
        }

        if (SiteSearchSqlTimeout.IsCommandTimeout(exception))
        {
            return true;
        }

        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sql && sql.Number is 2601 or 2627)
            {
                return true;
            }
        }

        return false;
    }
}
