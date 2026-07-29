namespace QueenZone.Web;

/// <summary>
/// Process-local per-principal daily upload caps (fits single-instance B1; not distributed).
/// </summary>
public sealed class UploadQuotaOptions
{
    public const string SectionName = "UploadQuotas";

    /// <summary>When false, quota checks always succeed (useful in tests).</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Maximum successful upload operations per principal per UTC day.</summary>
    public int MaxUploadsPerDay { get; init; } = 50;

    /// <summary>Maximum total uploaded bytes per principal per UTC day (default 100 MiB).</summary>
    public long MaxBytesPerDay { get; init; } = 100L * 1024 * 1024;
}
