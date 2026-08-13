namespace QueenZone.Data;

/// <summary>
/// Injects <see cref="PhotoListFilter"/> predicates into <see cref="PhotoSqlQueries"/> SQL shapes.
/// Placeholders: <c>{PHOTO_FILTER_P}</c>, <c>{PHOTO_FILTER_T}</c>, <c>{PHOTO_FILTER}</c> (unqualified).
/// </summary>
public static class PhotoSqlFilter
{
    public const string PlaceholderP = "{PHOTO_FILTER_P}";

    public const string PlaceholderT = "{PHOTO_FILTER_T}";

    public const string PlaceholderUnqualified = "{PHOTO_FILTER}";

    public static string ApplyProduction(string sql, PhotoListFilter? filter)
    {
        var active = filter ?? PhotoListFilter.None;
        return sql
            .Replace(PlaceholderP, active.ToSqlServerAndClause("p"), StringComparison.Ordinal)
            .Replace(PlaceholderT, active.ToSqlServerAndClause("t"), StringComparison.Ordinal)
            .Replace(PlaceholderUnqualified, active.ToSqlServerAndClause(string.Empty), StringComparison.Ordinal);
    }

    public static string ApplySqlite(string sql, PhotoListFilter? filter)
    {
        var active = filter ?? PhotoListFilter.None;
        return sql
            .Replace(PlaceholderP, active.ToSqliteAndClause("p"), StringComparison.Ordinal)
            .Replace(PlaceholderT, active.ToSqliteAndClause("t"), StringComparison.Ordinal)
            .Replace(PlaceholderUnqualified, active.ToSqliteAndClause(string.Empty), StringComparison.Ordinal);
    }
}
