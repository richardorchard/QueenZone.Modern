namespace QueenZone.Web;

public sealed class MemberAuthenticationOptions
{
    public const string SectionName = "Authentication";

    public ProviderCredentials? Google { get; init; }

    public ProviderCredentials? Microsoft { get; init; }

    public ProviderCredentials? Facebook { get; init; }

    public sealed class ProviderCredentials
    {
        public string? ClientId { get; init; }

        public string? ClientSecret { get; init; }
    }
}
