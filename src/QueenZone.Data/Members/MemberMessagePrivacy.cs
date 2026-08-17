namespace QueenZone.Data;

/// <summary>
/// Controls who may start a new private conversation with a member.
/// Existing conversations can still receive replies unless a block is in place.
/// </summary>
public enum MemberMessagePrivacy : byte
{
    /// <summary>Any signed-in member may start a conversation (default).</summary>
    Members = 0,

    /// <summary>Only members this person follows may start a conversation.</summary>
    Followed = 1,

    /// <summary>Nobody may start a new conversation.</summary>
    Nobody = 2,
}
