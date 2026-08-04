namespace QueenZone.Data;

public interface ILegacyMemberLookupRepository
{
    /// <summary>
    /// Returns every legacy USERS_T row whose EMAIL matches (case rules follow the database collation).
    /// Ordered by username then user id. Empty when none match.
    /// </summary>
    Task<IReadOnlyList<LegacyMemberMatch>> FindAllByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// First match for <paramref name="email"/>, or null. Prefer <see cref="FindAllByEmailAsync"/> when
    /// the caller must handle duplicate legacy emails.
    /// </summary>
    Task<LegacyMemberMatch?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<LegacyMemberMatch?> FindByUserIdAsync(int userId, CancellationToken cancellationToken = default);
}
