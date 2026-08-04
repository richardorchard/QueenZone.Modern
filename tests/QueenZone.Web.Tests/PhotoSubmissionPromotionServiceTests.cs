using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Storage;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class PhotoSubmissionPromotionServiceTests
{
    private readonly Guid memberId = Guid.NewGuid();

    [Fact]
    public async Task PromoteAsync_CopiesBlobsCreatesPublicRowAndMarksSubmissionPromoted()
    {
        var submissionRepository = new InMemoryPhotoSubmissionRepository();
        var store = new SharedPhotoStore(SamplePhotoData.CreateSeedCategories());
        var adminPhotoRepository = new InMemoryAdminPhotoRepository(store);
        var publicPhotoRepository = new InMemoryPhotoRepository(store);
        var blobUploadService = new FakeBlobUploadService();
        var galleryPhotoBlobService = new NullGalleryPhotoBlobService();
        var service = CreateService(
            submissionRepository, adminPhotoRepository, blobUploadService, galleryPhotoBlobService);

        var submission = await CreateApprovableSubmissionAsync(submissionRepository, blobUploadService, "Queen");

        var picId = await service.PromoteAsync(submission, "Queen", "admin@test.local", "Looks great");

        var updated = await submissionRepository.GetByIdAsync(submission.Id);
        Assert.Equal(PhotoSubmissionStatus.Approved, updated!.Status);
        Assert.Equal(picId, updated.PromotedPicId);
        Assert.Equal("Queen", updated.ApprovedCategory);

        var publicItem = await adminPhotoRepository.GetByIdAsync(picId);
        Assert.NotNull(publicItem);
        Assert.Equal(12, publicItem!.CatId);
        Assert.True(publicItem.IsVisible);
        Assert.Equal(submission.Title, publicItem.Title);

        var allInCategory = await publicPhotoRepository.GetCategoryAllAsync(12);
        Assert.Contains(allInCategory, item => item.PicId == picId);
    }

    [Fact]
    public async Task PromoteAsync_Throws_WhenCategoryDoesNotExist()
    {
        var submissionRepository = new InMemoryPhotoSubmissionRepository();
        var store = new SharedPhotoStore(SamplePhotoData.CreateSeedCategories());
        var adminPhotoRepository = new InMemoryAdminPhotoRepository(store);
        var blobUploadService = new FakeBlobUploadService();
        var service = CreateService(
            submissionRepository, adminPhotoRepository, blobUploadService, new NullGalleryPhotoBlobService());

        var submission = await CreateApprovableSubmissionAsync(submissionRepository, blobUploadService, "Nonexistent");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.PromoteAsync(submission, "Nonexistent", "admin@test.local", null));
        Assert.Contains("does not exist", ex.Message);

        var unchanged = await submissionRepository.GetByIdAsync(submission.Id);
        Assert.Equal(PhotoSubmissionStatus.Pending, unchanged!.Status);
        Assert.Null(unchanged.PromotedPicId);
    }

    [Fact]
    public async Task PromoteAsync_Throws_WhenBlobMissing()
    {
        var submissionRepository = new InMemoryPhotoSubmissionRepository();
        var store = new SharedPhotoStore(SamplePhotoData.CreateSeedCategories());
        var adminPhotoRepository = new InMemoryAdminPhotoRepository(store);
        var blobUploadService = new FakeBlobUploadService();
        var service = CreateService(
            submissionRepository, adminPhotoRepository, blobUploadService, new NullGalleryPhotoBlobService());

        var created = await submissionRepository.CreateAsync(new NewPhotoSubmission(
            memberId,
            "No blobs",
            "desc",
            "Queen",
            1986,
            new DateOnly(1986, 7, 12),
            "members/x/original.jpg",
            "members/x/display.webp",
            "members/x/thumb.webp",
            "photo.jpg",
            2048,
            "image/jpeg",
            800,
            600));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.PromoteAsync(created, "Queen", "admin@test.local", null));
        Assert.Contains("missing from storage", ex.Message);
    }

    private static PhotoSubmissionPromotionService CreateService(
        IPhotoSubmissionRepository submissionRepository,
        IAdminPhotoRepository adminPhotoRepository,
        IBlobUploadService blobUploadService,
        IGalleryPhotoBlobService galleryPhotoBlobService) =>
        new(
            submissionRepository,
            adminPhotoRepository,
            blobUploadService,
            galleryPhotoBlobService,
            new ServiceCollection().BuildServiceProvider());

    private async Task<PhotoSubmission> CreateApprovableSubmissionAsync(
        IPhotoSubmissionRepository submissionRepository,
        FakeBlobUploadService blobUploadService,
        string suggestedCategory)
    {
        var id = Guid.NewGuid();
        var webPath = $"members/{memberId:N}/{id:N}/display.webp";
        var thumbPath = $"members/{memberId:N}/{id:N}/thumb.webp";

        blobUploadService.Seed(BlobUploadContainers.Photos, webPath, "image/webp"u8.ToArray());
        blobUploadService.Seed(BlobUploadContainers.Photos, thumbPath, "image/webp"u8.ToArray());

        return await submissionRepository.CreateAsync(new NewPhotoSubmission(
            memberId,
            "Promotable photo",
            "desc",
            suggestedCategory,
            1986,
            new DateOnly(1986, 7, 12),
            $"members/{memberId:N}/{id:N}/original.jpg",
            webPath,
            thumbPath,
            "photo.jpg",
            2048,
            "image/jpeg",
            800,
            600,
            id));
    }

    private sealed class FakeBlobUploadService : IBlobUploadService
    {
        private readonly Dictionary<string, byte[]> blobs = new(StringComparer.OrdinalIgnoreCase);

        public void Seed(string containerName, string blobName, byte[] content) =>
            blobs[Key(containerName, blobName)] = content;

        public Task<BlobUploadResult> UploadAsync(
            Stream content,
            string originalFileName,
            string containerName,
            BlobUploadContext? context = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not used by promotion — promotion only reads existing submission blobs.");

        public Task DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
        {
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
                ContentType = "image/webp",
            });
        }

        private static string Key(string containerName, string blobName) => $"{containerName}/{blobName}";
    }
}
