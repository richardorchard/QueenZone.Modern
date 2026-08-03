namespace QueenZone.Data;

public interface IMemberPublicActivityRepository
{
    Task<MemberPublicActivityPage> GetPageAsync(
        Guid memberId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
