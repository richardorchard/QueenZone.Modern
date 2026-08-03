namespace QueenZone.Data;

public interface ILegacyMemberLookupRepository
{
    Task<LegacyMemberMatch?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<LegacyMemberMatch?> FindByUserIdAsync(int userId, CancellationToken cancellationToken = default);
}
