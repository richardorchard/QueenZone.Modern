using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using QueenZone.Data;
using QueenZone.Storage;
using QueenZone.Web.Sitemap;

namespace QueenZone.Web.Tests;

public sealed class StartupOptionsValidatorTests
{
    [Fact]
    public void UploadQuotaOptionsValidator_accepts_defaults()
    {
        var result = new UploadQuotaOptionsValidator().Validate(null, new UploadQuotaOptions());
        Assert.False(result.Failed);
    }

    [Theory]
    [InlineData(0, 1024)]
    [InlineData(-1, 1024)]
    [InlineData(1, 0)]
    [InlineData(UploadQuotaOptionsValidator.MaxUploadsPerDayCeiling + 1, 1024)]
    [InlineData(1, UploadQuotaOptionsValidator.MaxBytesPerDayCeiling + 1)]
    public void UploadQuotaOptionsValidator_rejects_non_positive_or_oversized_limits(
        int maxUploads,
        long maxBytes)
    {
        var result = new UploadQuotaOptionsValidator().Validate(
            null,
            new UploadQuotaOptions { MaxUploadsPerDay = maxUploads, MaxBytesPerDay = maxBytes });
        Assert.True(result.Failed);
    }

    [Fact]
    public void ForumOptionsValidator_accepts_default_zero_and_unlimited()
    {
        var validator = new ForumOptionsValidator();
        Assert.False(validator.Validate(null, new ForumOptions()).Failed);
        Assert.False(validator.Validate(null, new ForumOptions { PostEditWindowMinutes = 0 }).Failed);
        Assert.False(validator.Validate(null, new ForumOptions { PostEditWindowMinutes = -1 }).Failed);
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(ForumOptionsValidator.MaxPostEditWindowMinutes + 1)]
    public void ForumOptionsValidator_rejects_invalid_edit_windows(int minutes)
    {
        var result = new ForumOptionsValidator().Validate(
            null,
            new ForumOptions { PostEditWindowMinutes = minutes });
        Assert.True(result.Failed);
        Assert.Contains("PostEditWindowMinutes", result.FailureMessage);
    }

    [Fact]
    public void ForumDataOptionsValidator_allows_modern_and_legacy_reads()
    {
        var validator = new ForumDataOptionsValidator();
        Assert.False(validator.Validate(null, new ForumDataOptions { UseModernForumReads = true }).Failed);
        Assert.False(validator.Validate(null, new ForumDataOptions { UseModernForumReads = false }).Failed);
    }

    [Fact]
    public void ForumAttachmentOptionsValidator_accepts_defaults()
    {
        var result = new ForumAttachmentOptionsValidator().Validate(null, new ForumAttachmentOptions());
        Assert.False(result.Failed);
    }

    [Fact]
    public void ForumAttachmentOptionsValidator_rejects_total_smaller_than_file_limit()
    {
        var result = new ForumAttachmentOptionsValidator().Validate(
            null,
            new ForumAttachmentOptions
            {
                MaxFilesPerPost = 1,
                MaxBytesPerFile = 10,
                MaxTotalBytesPerPost = 5,
            });
        Assert.True(result.Failed);
        Assert.Contains("MaxTotalBytesPerPost", result.FailureMessage);
    }

    [Fact]
    public void ForumAttachmentOptionsValidator_rejects_blank_content_types()
    {
        var result = new ForumAttachmentOptionsValidator().Validate(
            null,
            new ForumAttachmentOptions { AllowedContentTypes = ["image/jpeg", "  "] });
        Assert.True(result.Failed);
        Assert.Contains("AllowedContentTypes", result.FailureMessage);
    }

    [Fact]
    public void ForumAttachmentOptionsValidator_rejects_empty_content_types()
    {
        var result = new ForumAttachmentOptionsValidator().Validate(
            null,
            new ForumAttachmentOptions { AllowedContentTypes = [] });
        Assert.True(result.Failed);
    }

    [Fact]
    public void ForumAttachmentOptionsValidator_rejects_non_positive_file_count()
    {
        var result = new ForumAttachmentOptionsValidator().Validate(
            null,
            new ForumAttachmentOptions { MaxFilesPerPost = 0 });
        Assert.True(result.Failed);
    }

    [Fact]
    public void ForumAttachmentOptionsValidator_rejects_non_positive_file_bytes()
    {
        var result = new ForumAttachmentOptionsValidator().Validate(
            null,
            new ForumAttachmentOptions { MaxBytesPerFile = 0 });
        Assert.True(result.Failed);
        Assert.Contains("MaxBytesPerFile", result.FailureMessage);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("E2E")]
    public void AnalyticsOptionsValidator_allows_empty_measurement_id_outside_production(string environmentName)
    {
        var result = new AnalyticsOptionsValidator(new FakeHostEnvironment(environmentName))
            .Validate(null, new AnalyticsOptions { MeasurementId = "", TrafficCacheMinutes = 60 });
        Assert.False(result.Failed);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Preview")]
    public void AnalyticsOptionsValidator_requires_measurement_id_in_production_like_environments(
        string environmentName)
    {
        var result = new AnalyticsOptionsValidator(new FakeHostEnvironment(environmentName))
            .Validate(null, new AnalyticsOptions { MeasurementId = "", TrafficCacheMinutes = 60 });
        Assert.True(result.Failed);
        Assert.Contains("MeasurementId", result.FailureMessage);
    }

    [Fact]
    public void AnalyticsOptionsValidator_accepts_ga4_measurement_id()
    {
        var result = new AnalyticsOptionsValidator(new FakeHostEnvironment("Production"))
            .Validate(null, new AnalyticsOptions { MeasurementId = "G-V2W56BZ3KZ", TrafficCacheMinutes = 60 });
        Assert.False(result.Failed);
    }

    [Fact]
    public void AnalyticsOptionsValidator_rejects_malformed_measurement_id()
    {
        var result = new AnalyticsOptionsValidator(new FakeHostEnvironment("Development"))
            .Validate(null, new AnalyticsOptions { MeasurementId = "UA-123", TrafficCacheMinutes = 60 });
        Assert.True(result.Failed);
        Assert.Contains("MeasurementId", result.FailureMessage);
    }

    [Fact]
    public void AnalyticsOptionsValidator_rejects_partial_data_api_config()
    {
        var result = new AnalyticsOptionsValidator(new FakeHostEnvironment("Development"))
            .Validate(null, new AnalyticsOptions
            {
                MeasurementId = "G-V2W56BZ3KZ",
                GoogleAnalyticsPropertyId = "123456",
                TrafficCacheMinutes = 60,
            });
        Assert.True(result.Failed);
        Assert.Contains("GoogleAnalyticsServiceAccountJson", result.FailureMessage);
    }

    [Fact]
    public void AnalyticsOptionsValidator_accepts_complete_data_api_config()
    {
        var result = new AnalyticsOptionsValidator(new FakeHostEnvironment("Development"))
            .Validate(null, new AnalyticsOptions
            {
                MeasurementId = "G-V2W56BZ3KZ",
                GoogleAnalyticsPropertyId = "123456",
                GoogleAnalyticsServiceAccountJson = "{}",
                TrafficCacheMinutes = 60,
            });
        Assert.False(result.Failed);
    }

    [Fact]
    public void AnalyticsOptionsValidator_rejects_non_positive_cache_minutes()
    {
        var result = new AnalyticsOptionsValidator(new FakeHostEnvironment("Development"))
            .Validate(null, new AnalyticsOptions { TrafficCacheMinutes = 0 });
        Assert.True(result.Failed);
        Assert.Contains("TrafficCacheMinutes", result.FailureMessage);
    }

    [Fact]
    public void AnalyticsOptionsValidator_rejects_oversized_cache_minutes()
    {
        var result = new AnalyticsOptionsValidator(new FakeHostEnvironment("Development"))
            .Validate(null, new AnalyticsOptions { TrafficCacheMinutes = AnalyticsOptionsValidator.MaxTrafficCacheMinutes + 1 });
        Assert.True(result.Failed);
        Assert.Contains("TrafficCacheMinutes", result.FailureMessage);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("E2E")]
    public void MemberAuthenticationOptionsValidator_allows_empty_providers_outside_production(
        string environmentName)
    {
        var result = new MemberAuthenticationOptionsValidator(new FakeHostEnvironment(environmentName))
            .Validate(null, new MemberAuthenticationOptions());
        Assert.False(result.Failed);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Preview")]
    public void MemberAuthenticationOptionsValidator_requires_a_provider_in_production_like_environments(
        string environmentName)
    {
        var result = new MemberAuthenticationOptionsValidator(new FakeHostEnvironment(environmentName))
            .Validate(null, new MemberAuthenticationOptions());
        Assert.True(result.Failed);
        Assert.Contains("OAuth provider", result.FailureMessage);
    }

    [Fact]
    public void MemberAuthenticationOptionsValidator_rejects_client_id_without_secret()
    {
        var result = new MemberAuthenticationOptionsValidator(new FakeHostEnvironment("Development"))
            .Validate(null, new MemberAuthenticationOptions
            {
                Google = new MemberAuthenticationOptions.ProviderCredentials
                {
                    ClientId = "google-client",
                    ClientSecret = "",
                },
            });
        Assert.True(result.Failed);
        Assert.Contains("Google", result.FailureMessage);
    }

    [Fact]
    public void MemberAuthenticationOptionsValidator_rejects_secret_without_client_id()
    {
        var result = new MemberAuthenticationOptionsValidator(new FakeHostEnvironment("Development"))
            .Validate(null, new MemberAuthenticationOptions
            {
                Microsoft = new MemberAuthenticationOptions.ProviderCredentials
                {
                    ClientId = " ",
                    ClientSecret = "microsoft-secret",
                },
            });
        Assert.True(result.Failed);
        Assert.Contains("Microsoft", result.FailureMessage);
    }

    [Fact]
    public void MemberAuthenticationOptionsValidator_accepts_complete_provider_in_production()
    {
        var result = new MemberAuthenticationOptionsValidator(new FakeHostEnvironment("Production"))
            .Validate(null, new MemberAuthenticationOptions
            {
                Google = new MemberAuthenticationOptions.ProviderCredentials
                {
                    ClientId = "google-client",
                    ClientSecret = "google-secret",
                },
            });
        Assert.False(result.Failed);
    }

    [Fact]
    public void MemberAuthenticationOptionsValidator_accepts_complete_apple_provider()
    {
        var result = new MemberAuthenticationOptionsValidator(new FakeHostEnvironment("Production"))
            .Validate(null, new MemberAuthenticationOptions
            {
                Apple = new MemberAuthenticationOptions.AppleCredentials
                {
                    ClientId = "org.queenzone.web",
                    TeamId = "TEAM123456",
                    KeyId = "KEY1234567",
                    PrivateKey = "private-key",
                },
            });

        Assert.False(result.Failed);
    }

    [Fact]
    public void MemberAuthenticationOptionsValidator_rejects_partial_apple_provider()
    {
        var result = new MemberAuthenticationOptionsValidator(new FakeHostEnvironment("Development"))
            .Validate(null, new MemberAuthenticationOptions
            {
                Apple = new MemberAuthenticationOptions.AppleCredentials
                {
                    ClientId = "org.queenzone.web",
                    TeamId = "TEAM123456",
                },
            });

        Assert.True(result.Failed);
        Assert.Contains("ClientId, TeamId, KeyId and PrivateKey", result.FailureMessage);
    }

    [Fact]
    public void MemberAuthenticationOptionsValidator_rejects_placeholder_credentials()
    {
        var result = new MemberAuthenticationOptionsValidator(new FakeHostEnvironment("Production"))
            .Validate(null, new MemberAuthenticationOptions
            {
                Google = new MemberAuthenticationOptions.ProviderCredentials
                {
                    ClientId = "YOUR_CLIENT_ID",
                    ClientSecret = "YOUR_CLIENT_SECRET",
                },
            });
        Assert.True(result.Failed);
        Assert.Contains("OAuth provider", result.FailureMessage);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    [InlineData("E2E")]
    public void BlobUploadOptionsValidator_allows_empty_connection_outside_production(string environmentName)
    {
        var result = CreateBlobValidator(environmentName)
            .Validate(null, new BlobUploadOptions());
        Assert.False(result.Failed);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Preview")]
    public void BlobUploadOptionsValidator_requires_connection_in_production_like_environments(
        string environmentName)
    {
        var result = CreateBlobValidator(environmentName)
            .Validate(null, new BlobUploadOptions());
        Assert.True(result.Failed);
        Assert.Contains("ConnectionStrings:BlobStorage", result.FailureMessage);
    }

    [Fact]
    public void BlobUploadOptionsValidator_rejects_placeholder_connection_in_production()
    {
        var result = CreateBlobValidator("Production", "YOUR_STORAGE_CONNECTION")
            .Validate(null, new BlobUploadOptions());
        Assert.True(result.Failed);
        Assert.Contains("ConnectionStrings:BlobStorage", result.FailureMessage);
    }

    [Fact]
    public void BlobUploadOptionsValidator_accepts_connection_in_production()
    {
        var result = CreateBlobValidator(
                "Production",
                "DefaultEndpointsProtocol=https;AccountName=qz;AccountKey=test;EndpointSuffix=core.windows.net")
            .Validate(null, new BlobUploadOptions());
        Assert.False(result.Failed);
    }

    [Fact]
    public void BlobUploadOptionsValidator_rejects_non_positive_size_limits()
    {
        var result = CreateBlobValidator("Development")
            .Validate(null, new BlobUploadOptions { DefaultMaxBytes = 0 });
        Assert.True(result.Failed);
        Assert.Contains("DefaultMaxBytes", result.FailureMessage);
    }

    [Fact]
    public void BlobUploadOptionsValidator_rejects_non_positive_editor_size_limit()
    {
        var result = CreateBlobValidator("Development")
            .Validate(null, new BlobUploadOptions { EditorMaxBytes = 0 });
        Assert.True(result.Failed);
        Assert.Contains("EditorMaxBytes", result.FailureMessage);
    }

    [Fact]
    public void BlobUploadOptionsValidator_rejects_empty_default_content_types()
    {
        var result = CreateBlobValidator("Development")
            .Validate(null, new BlobUploadOptions { DefaultAllowedContentTypes = [] });
        Assert.True(result.Failed);
        Assert.Contains("DefaultAllowedContentTypes", result.FailureMessage);
    }

    [Fact]
    public void BlobUploadOptionsValidator_rejects_blank_container_names()
    {
        var result = CreateBlobValidator("Development")
            .Validate(null, new BlobUploadOptions
            {
                Containers = { [""] = new BlobContainerPolicy { MaxBytes = 1024 } },
            });
        Assert.True(result.Failed);
        Assert.Contains("Containers keys", result.FailureMessage);
    }

    [Fact]
    public void BlobUploadOptionsValidator_rejects_invalid_public_base_url()
    {
        var result = CreateBlobValidator("Development")
            .Validate(null, new BlobUploadOptions { PublicBaseUrl = "not-a-url" });
        Assert.True(result.Failed);
        Assert.Contains("PublicBaseUrl", result.FailureMessage);
    }

    [Fact]
    public void BlobUploadOptionsValidator_accepts_https_public_base_url()
    {
        var result = CreateBlobValidator("Development")
            .Validate(null, new BlobUploadOptions { PublicBaseUrl = "https://cdn.queenzone.org" });
        Assert.False(result.Failed);
    }

    [Fact]
    public void BlobUploadOptionsValidator_rejects_blank_container_content_types()
    {
        var result = CreateBlobValidator("Development")
            .Validate(null, new BlobUploadOptions
            {
                Containers =
                {
                    ["ugc-avatars"] = new BlobContainerPolicy
                    {
                        MaxBytes = 1024,
                        AllowedContentTypes = [" "],
                    },
                },
            });
        Assert.True(result.Failed);
        Assert.Contains("AllowedContentTypes", result.FailureMessage);
    }

    [Fact]
    public void BlobUploadOptionsValidator_rejects_non_positive_container_max_bytes()
    {
        var result = CreateBlobValidator("Development")
            .Validate(null, new BlobUploadOptions
            {
                Containers =
                {
                    ["ugc-avatars"] = new BlobContainerPolicy { MaxBytes = 0 },
                },
            });
        Assert.True(result.Failed);
        Assert.Contains("MaxBytes", result.FailureMessage);
    }

    [Fact]
    public void FanPerformanceRateLimitingOptionsValidator_accepts_defaults()
    {
        var result = new FanPerformanceRateLimitingOptionsValidator()
            .Validate(null, new FanPerformanceRateLimitingOptions());
        Assert.False(result.Failed);
    }

    [Theory]
    [InlineData(0, 300, 60, 60)]
    [InlineData(10, 0, 60, 60)]
    [InlineData(10, 300, 0, 60)]
    [InlineData(10, 300, 60, 0)]
    [InlineData(FanPerformanceRateLimitingOptionsValidator.MaxPermitLimit + 1, 300, 60, 60)]
    [InlineData(10, FanPerformanceRateLimitingOptionsValidator.MaxWindowSeconds + 1, 60, 60)]
    public void FanPerformanceRateLimitingOptionsValidator_rejects_non_positive_or_oversized_limits(
        int audioPermit,
        int audioWindow,
        int browsePermit,
        int browseWindow)
    {
        var result = new FanPerformanceRateLimitingOptionsValidator().Validate(
            null,
            new FanPerformanceRateLimitingOptions
            {
                AudioPermitLimit = audioPermit,
                AudioSlidingWindowSeconds = audioWindow,
                BrowsePermitLimit = browsePermit,
                BrowseWindowSeconds = browseWindow,
            });
        Assert.True(result.Failed);
    }

    [Fact]
    public void PublicQueryCacheOptionsValidator_rejects_non_positive_durations()
    {
        var result = new PublicQueryCacheOptionsValidator().Validate(
            null,
            new PublicQueryCacheOptions { NewsCacheDuration = TimeSpan.Zero });
        Assert.True(result.Failed);
    }

    [Fact]
    public void PublicQueryCacheOptionsValidator_accepts_defaults()
    {
        var result = new PublicQueryCacheOptionsValidator().Validate(null, new PublicQueryCacheOptions());
        Assert.False(result.Failed);
    }

    [Fact]
    public void SitemapOptionsValidator_accepts_positive_cache_hours()
    {
        var result = new SitemapOptionsValidator().Validate(null, new SitemapOptions { CacheHours = 24 });
        Assert.False(result.Failed);
    }

    private static BlobUploadOptionsValidator CreateBlobValidator(
        string environmentName,
        string? blobConnection = null)
    {
        var values = new Dictionary<string, string?>();
        if (blobConnection is not null)
        {
            values["ConnectionStrings:BlobStorage"] = blobConnection;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        return new BlobUploadOptionsValidator(new FakeHostEnvironment(environmentName), configuration);
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "QueenZone.Web.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
