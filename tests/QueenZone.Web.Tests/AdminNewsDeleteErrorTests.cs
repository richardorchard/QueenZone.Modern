using Microsoft.EntityFrameworkCore;
using QueenZone.Web.Pages.Admin.News;

namespace QueenZone.Web.Tests;

[Collection(AdminNewsDeleteErrorCollection.Name)]
public sealed class AdminNewsDeleteErrorTests
{
    [Fact]
    public void IsDeleteForeignKeyViolation_returns_true_for_wrapped_db_update_exception()
    {
        using var _ = AdminNewsDeleteError.UseForeignKeyViolationClassifier(_ => true);
        var exception = new DbUpdateException("delete failed", new InvalidOperationException("fk"));

        Assert.True(AdminNewsDeleteError.IsDeleteForeignKeyViolation(exception));
    }

    [Fact]
    public void IsDeleteForeignKeyViolation_returns_false_for_unrelated_exception()
    {
        Assert.False(AdminNewsDeleteError.IsDeleteForeignKeyViolation(new InvalidOperationException("nope")));
    }

    [Fact]
    public void IsForeignKeyViolation_uses_configured_classifier()
    {
        using var _ = AdminNewsDeleteError.UseForeignKeyViolationClassifier(_ => true);
        var dbException = new DbUpdateException("delete failed", new InvalidOperationException("fk"));

        Assert.True(AdminNewsDeleteError.IsForeignKeyViolation(dbException));
    }

    [Fact]
    public void IsForeignKeyViolation_returns_false_when_classifier_returns_false()
    {
        using var _ = AdminNewsDeleteError.UseForeignKeyViolationClassifier(_ => false);
        var dbException = new DbUpdateException("delete failed", new InvalidOperationException("fk"));

        Assert.False(AdminNewsDeleteError.IsForeignKeyViolation(dbException));
    }

    [Fact]
    public void IsForeignKeyViolation_returns_false_without_inner_exception_by_default()
    {
        var dbException = new DbUpdateException("delete failed", new InvalidOperationException("nope"));

        Assert.False(AdminNewsDeleteError.IsForeignKeyViolation(dbException));
    }
}
