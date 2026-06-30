namespace QueenZone.Data;

public interface ILegacyMemberLookupRepository
{
    Task<LegacyMemberMatch?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
}
