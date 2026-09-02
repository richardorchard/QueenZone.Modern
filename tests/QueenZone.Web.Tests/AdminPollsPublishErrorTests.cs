using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Web.Pages.Admin.Polls;

namespace QueenZone.Web.Tests;

public sealed class AdminPollsPublishErrorTests
{
    [Fact]
    public void IsPublishPersistenceFailure_detects_db_update_unique_and_timeout()
    {
        var duplicate = new DbUpdateException(
            "fail",
            SiteSearchSqlTimeoutTests.CreateSqlException(
                2601,
                "Cannot insert duplicate key row in object 'dbo.HomePolls' with unique index 'UX_HomePolls_IsCurrent'."));
        Assert.True(IndexModel.IsPublishPersistenceFailure(duplicate));

        var uniqueKey = SiteSearchSqlTimeoutTests.CreateSqlException(2627, "Violation of UNIQUE KEY constraint");
        Assert.True(IndexModel.IsPublishPersistenceFailure(uniqueKey));

        var timeout = SiteSearchSqlTimeoutTests.CreateSqlException(
            SiteSearchSqlTimeout.SqlErrorNumber,
            "Execution Timeout Expired. The timeout period elapsed prior to completion of the operation or the server is not responding.");
        Assert.True(IndexModel.IsPublishPersistenceFailure(timeout));
        Assert.True(IndexModel.IsPublishPersistenceFailure(new InvalidOperationException("wrap", timeout)));

        Assert.False(IndexModel.IsPublishPersistenceFailure(new InvalidOperationException("nope")));
        Assert.False(IndexModel.IsPublishPersistenceFailure(
            SiteSearchSqlTimeoutTests.CreateSqlException(208, "Invalid object name.")));
    }
}
