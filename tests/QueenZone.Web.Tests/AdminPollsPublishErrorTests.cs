using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class AdminPollsPublishErrorTests
{
    [Fact]
    public void IsPersistenceFailure_detects_db_update_unique_and_timeout()
    {
        var duplicate = new DbUpdateException(
            "fail",
            SiteSearchSqlTimeoutTests.CreateSqlException(
                2601,
                "Cannot insert duplicate key row in object 'dbo.HomePolls' with unique index 'UX_HomePolls_IsCurrent'."));
        Assert.True(AdminHomePollPublishError.IsPersistenceFailure(duplicate));

        var uniqueKey = SiteSearchSqlTimeoutTests.CreateSqlException(2627, "Violation of UNIQUE KEY constraint");
        Assert.True(AdminHomePollPublishError.IsPersistenceFailure(uniqueKey));

        var timeout = SiteSearchSqlTimeoutTests.CreateSqlException(
            SiteSearchSqlTimeout.SqlErrorNumber,
            "Execution Timeout Expired. The timeout period elapsed prior to completion of the operation or the server is not responding.");
        Assert.True(AdminHomePollPublishError.IsPersistenceFailure(timeout));
        Assert.True(AdminHomePollPublishError.IsPersistenceFailure(new InvalidOperationException("wrap", timeout)));

        Assert.False(AdminHomePollPublishError.IsPersistenceFailure(new InvalidOperationException("nope")));
        Assert.False(AdminHomePollPublishError.IsPersistenceFailure(
            SiteSearchSqlTimeoutTests.CreateSqlException(208, "Invalid object name.")));
    }
}
