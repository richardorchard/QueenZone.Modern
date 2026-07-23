using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace QueenZone.Web;

/// <summary>
/// Process-local daily upload quotas keyed by member id or email.
/// Suitable for single-instance hosting; counters do not share across workers.
/// </summary>
public sealed class MemberUploadQuotaService(
    IMemoryCache cache,
    TimeProvider timeProvider,
    IOptions<UploadQuotaOptions> options)
{
    private sealed class DailyUsage
    {
        public int Count;
        public long Bytes;
    }

    public static string PrincipalKeyFromMemberId(Guid memberId) =>
        memberId == Guid.Empty ? "anonymous" : $"member:{memberId:N}";

    public static string PrincipalKeyFromUser(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(id, out var memberId) && memberId != Guid.Empty)
        {
            return PrincipalKeyFromMemberId(memberId);
        }

        var email = user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue("preferred_username")
            ?? user.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(email))
        {
            return "email:" + email.Trim().ToLowerInvariant();
        }

        return "anonymous";
    }

    /// <summary>
    /// Atomically checks and consumes quota for one or more upload operations.
    /// Prefer calling after basic validation and before blob writes.
    /// </summary>
    public bool TryConsume(
        string principalKey,
        long byteCount,
        out string? errorMessage,
        int uploadCount = 1)
    {
        errorMessage = null;
        var opts = options.Value;
        if (!opts.Enabled)
        {
            return true;
        }

        if (uploadCount < 1)
        {
            uploadCount = 1;
        }

        if (byteCount < 0)
        {
            byteCount = 0;
        }

        if (string.IsNullOrWhiteSpace(principalKey))
        {
            principalKey = "anonymous";
        }

        if (opts.MaxUploadsPerDay < 1)
        {
            errorMessage = "Uploads are temporarily disabled.";
            return false;
        }

        if (opts.MaxBytesPerDay < 1)
        {
            errorMessage = "Uploads are temporarily disabled.";
            return false;
        }

        // Single-file over the daily byte cap can never succeed.
        if (byteCount > opts.MaxBytesPerDay)
        {
            errorMessage =
                $"Upload exceeds the daily size limit ({FormatBytes(opts.MaxBytesPerDay)} per day).";
            return false;
        }

        var now = timeProvider.GetUtcNow();
        var dayKey = now.UtcDateTime.ToString("yyyyMMdd");
        var cacheKey = $"upload-quota:{principalKey}:{dayKey}";

        var endOfUtcDay = new DateTimeOffset(now.UtcDateTime.Date.AddDays(1), TimeSpan.Zero);
        var ttl = endOfUtcDay - now + TimeSpan.FromMinutes(5);
        if (ttl < TimeSpan.FromMinutes(1))
        {
            ttl = TimeSpan.FromHours(1);
        }

        var usage = cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ttl;
            return new DailyUsage();
        })!;

        lock (usage)
        {
            if (usage.Count + uploadCount > opts.MaxUploadsPerDay)
            {
                errorMessage =
                    $"Daily upload limit reached ({opts.MaxUploadsPerDay} uploads per day). Try again tomorrow.";
                return false;
            }

            if (usage.Bytes + byteCount > opts.MaxBytesPerDay)
            {
                errorMessage =
                    $"Daily upload size limit reached ({FormatBytes(opts.MaxBytesPerDay)} per day). Try again tomorrow.";
                return false;
            }

            usage.Count += uploadCount;
            usage.Bytes += byteCount;
            return true;
        }
    }

    internal static string FormatBytes(long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024)
        {
            return $"{bytes / (1024.0 * 1024 * 1024):0.#} GB";
        }

        if (bytes >= 1024L * 1024)
        {
            return $"{bytes / (1024.0 * 1024):0.#} MB";
        }

        if (bytes >= 1024)
        {
            return $"{bytes / 1024.0:0.#} KB";
        }

        return $"{bytes} bytes";
    }
}
