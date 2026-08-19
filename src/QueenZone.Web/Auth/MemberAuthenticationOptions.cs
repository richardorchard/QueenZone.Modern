namespace QueenZone.Web;

public sealed class MemberAuthenticationOptions
{
    public const string SectionName = "Authentication";

    public ProviderCredentials? Google { get; init; }

    public ProviderCredentials? Microsoft { get; init; }

    public ProviderCredentials? Discord { get; init; }

    public ProviderCredentials? GitHub { get; init; }

    public AppleCredentials? Apple { get; init; }

    public sealed class ProviderCredentials
    {
        public string? ClientId { get; init; }

        public string? ClientSecret { get; init; }
    }

    public sealed class AppleCredentials
    {
        public string? ClientId { get; init; }

        public string? TeamId { get; init; }

        public string? KeyId { get; init; }

        public string? PrivateKey { get; init; }

        public bool IsConfigured =>
            OptionsValidation.LooksConfigured(ClientId)
            && OptionsValidation.LooksConfigured(TeamId)
            && OptionsValidation.LooksConfigured(KeyId)
            && OptionsValidation.LooksConfigured(PrivateKey);
    }
}
