using QueenZone.Data;
using QueenZone.Storage;

namespace QueenZone.Web.Tests;

public sealed class InMemoryAdminFanPerformanceRepositoryTests
{
    [Fact]
    public async Task Create_Update_And_Hide_ShareStoreWithPublicReads_WithoutDeletingAudioBlob()
    {
        var store = new SharedFanPerformanceStore();
        var admin = new InMemoryAdminFanPerformanceRepository(store);
        var publicRepo = new InMemoryFanPerformanceRepository(store);
        var blobs = new RecordingBlobUploadService();
        blobs.Seed(SongFileUrl.ContainerName, "keep-me.mp3", "audio"u8.ToArray());

        var createdId = await admin.CreateAsync(
            new AdminFanPerformanceCreateRequest(
                Title: "Published cover",
                PerformedBy: "Test Band",
                Description: "A new archive row.",
                AudioFileName: "keep-me.mp3",
                FileSizeBytes: 2048,
                DateAdded: new DateTime(2026, 9, 4, 12, 0, 0, DateTimeKind.Utc),
                IsVisible: true),
            "admin@test.local");

        var created = await admin.GetByIdAsync(createdId);
        Assert.NotNull(created);
        Assert.Equal("Published cover", created.Title);
        Assert.True(created.IsVisible);
        Assert.Equal("keep-me.mp3", created.AudioFileName);

        var publicItems = await publicRepo.GetPageAsync(1, 20);
        Assert.Contains(publicItems, item => item.Id == createdId);

        await admin.UpdateAsync(
            createdId,
            new AdminFanPerformanceUpdateRequest(
                "Renamed cover",
                "Other Band",
                "Updated notes.",
                new DateTime(2026, 9, 5, 8, 0, 0, DateTimeKind.Utc)),
            "admin@test.local");

        var updated = await admin.GetByIdAsync(createdId);
        Assert.NotNull(updated);
        Assert.Equal("Renamed cover", updated.Title);
        Assert.Equal("Other Band", updated.PerformedBy);

        await admin.SetVisibilityAsync(createdId, false, "admin@test.local");

        var hidden = await admin.GetByIdAsync(createdId);
        Assert.NotNull(hidden);
        Assert.False(hidden.IsVisible);
        Assert.Equal("keep-me.mp3", hidden.AudioFileName);

        Assert.DoesNotContain(await publicRepo.GetPageAsync(1, 20), item => item.Id == createdId);
        Assert.Null(await publicRepo.GetByIdAsync(createdId));

        var hiddenPage = await admin.GetPageAsync(new AdminFanPerformanceListFilter(IsVisible: false), 1, 50);
        Assert.Contains(hiddenPage.Items, item => item.Id == createdId);

        Assert.Empty(blobs.Deleted);
        Assert.True(blobs.Exists(SongFileUrl.ContainerName, "keep-me.mp3"));
    }

    [Fact]
    public async Task Update_and_visibility_reject_stale_concurrency_token()
    {
        var store = new SharedFanPerformanceStore();
        var admin = new InMemoryAdminFanPerformanceRepository(store);
        var createdId = await admin.CreateAsync(
            new AdminFanPerformanceCreateRequest(
                Title: "Token performance",
                PerformedBy: "Original",
                Description: "Original notes",
                AudioFileName: "token.mp3",
                FileSizeBytes: 1024,
                DateAdded: new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                IsVisible: true),
            "admin@test.local");
        var created = await admin.GetByIdAsync(createdId);
        var stale = created!.ToConcurrencyToken();

        await admin.UpdateAsync(
            createdId,
            new AdminFanPerformanceUpdateRequest(
                "Changed",
                "Original",
                "Original notes",
                created.DateAdded),
            "admin@test.local");

        await Assert.ThrowsAsync<OptimisticConcurrencyException>(() =>
            admin.UpdateAsync(
                createdId,
                new AdminFanPerformanceUpdateRequest(
                    "Stale write",
                    "Original",
                    "Original notes",
                    created.DateAdded),
                "admin@test.local",
                stale));

        await admin.SetVisibilityAsync(createdId, false, "admin@test.local", expectedIsVisible: true);
        await Assert.ThrowsAsync<OptimisticConcurrencyException>(() =>
            admin.SetVisibilityAsync(createdId, true, "admin@test.local", expectedIsVisible: true));

        var after = await admin.GetByIdAsync(createdId);
        Assert.Equal("Changed", after!.Title);
        Assert.False(after.IsVisible);
    }

    [Fact]
    public async Task SearchFilter_MatchesTitlePerformedByAndDescription()
    {
        var store = new SharedFanPerformanceStore();
        var admin = new InMemoryAdminFanPerformanceRepository(store);
        await admin.CreateAsync(
            new AdminFanPerformanceCreateRequest(
                "Hammer to Fall",
                "Sonic Snafu",
                "A fan tribute cover.",
                "hammer.mp3",
                100,
                new DateTime(2013, 5, 1),
                true),
            "admin@test.local");

        var byTitle = await admin.GetPageAsync(new AdminFanPerformanceListFilter(Search: "Hammer"), 1, 20);
        Assert.Contains(byTitle.Items, item => item.Title.Contains("Hammer", StringComparison.OrdinalIgnoreCase));

        var byPerformer = await admin.GetPageAsync(new AdminFanPerformanceListFilter(Search: "Snafu"), 1, 20);
        Assert.Contains(byPerformer.Items, item => item.PerformedBy.Contains("Snafu", StringComparison.OrdinalIgnoreCase));

        var byDescription = await admin.GetPageAsync(new AdminFanPerformanceListFilter(Search: "tribute"), 1, 20);
        Assert.Contains(byDescription.Items, item => item.Description.Contains("tribute", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MissingRows_ThrowNotFound_WithoutConcurrencyException()
    {
        var admin = new InMemoryAdminFanPerformanceRepository(new SharedFanPerformanceStore());

        var missingUpdate = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            admin.UpdateAsync(
                999,
                new AdminFanPerformanceUpdateRequest("x", "y", "z", DateTime.UtcNow),
                "admin@test.local"));
        Assert.Contains("was not found", missingUpdate.Message);

        var missingHide = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            admin.SetVisibilityAsync(999, false, "admin@test.local"));
        Assert.Contains("was not found", missingHide.Message);
    }

    private sealed class RecordingBlobUploadService : IBlobUploadService
    {
        private readonly Dictionary<string, byte[]> blobs = new(StringComparer.OrdinalIgnoreCase);

        public List<(string Container, string BlobName)> Deleted { get; } = [];

        public void Seed(string containerName, string blobName, byte[] content) =>
            blobs[Key(containerName, blobName)] = content;

        public bool Exists(string containerName, string blobName) =>
            blobs.ContainsKey(Key(containerName, blobName));

        public Task<BlobUploadResult> UploadAsync(
            Stream content,
            string originalFileName,
            string containerName,
            BlobUploadContext? context = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
        {
            Deleted.Add((containerName, blobName));
            blobs.Remove(Key(containerName, blobName));
            return Task.CompletedTask;
        }

        public Task<BlobContent?> OpenReadAsync(
            string containerName,
            string blobName,
            CancellationToken cancellationToken = default)
        {
            if (!blobs.TryGetValue(Key(containerName, blobName), out var bytes))
            {
                return Task.FromResult<BlobContent?>(null);
            }

            return Task.FromResult<BlobContent?>(new BlobContent
            {
                Stream = new MemoryStream(bytes, writable: false),
                ContentType = "audio/mpeg",
            });
        }

        private static string Key(string containerName, string blobName) => $"{containerName}/{blobName}";
    }
}
