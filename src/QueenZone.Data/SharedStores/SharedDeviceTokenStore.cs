using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class SharedDeviceTokenStore
{
    public Lock Gate { get; } = new();

    public List<DeviceTokenEntity> Tokens { get; } = [];
}
