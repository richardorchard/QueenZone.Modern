using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class QueenZoneDbContext : DbContext
{
    public QueenZoneDbContext(DbContextOptions<QueenZoneDbContext> options)
        : base(options)
    {
    }

    public DbSet<NewsTableRow> NewsRows => Set<NewsTableRow>();

    public DbSet<NewsAuditLogEntity> NewsAuditLogs => Set<NewsAuditLogEntity>();

    public DbSet<MemberAccount> MemberAccounts => Set<MemberAccount>();

    public DbSet<MemberExternalLogin> MemberExternalLogins => Set<MemberExternalLogin>();

    public DbSet<ModernForumCategoryEntity> ModernForumCategories => Set<ModernForumCategoryEntity>();

    public DbSet<ModernForumThreadEntity> ModernForumThreads => Set<ModernForumThreadEntity>();

    public DbSet<ModernForumPostEntity> ModernForumPosts => Set<ModernForumPostEntity>();

    public DbSet<ForumPostAttachmentEntity> ForumPostAttachments => Set<ForumPostAttachmentEntity>();

    public DbSet<ForumPollEntity> ForumPolls => Set<ForumPollEntity>();

    public DbSet<ForumPollOptionEntity> ForumPollOptions => Set<ForumPollOptionEntity>();

    public DbSet<ForumPollVoteEntity> ForumPollVotes => Set<ForumPollVoteEntity>();

    public DbSet<NewsDiscoverySourceEntity> NewsDiscoverySources => Set<NewsDiscoverySourceEntity>();

    public DbSet<NewsCandidateEntity> NewsCandidates => Set<NewsCandidateEntity>();

    public DbSet<NewsCandidateEvidenceEntity> NewsCandidateEvidence => Set<NewsCandidateEvidenceEntity>();

    public DbSet<NewsAiRunEntity> NewsAiRuns => Set<NewsAiRunEntity>();

    public DbSet<NewsAgentDraftEntity> NewsAgentDrafts => Set<NewsAgentDraftEntity>();

    public DbSet<NewsAgentRunLeaseEntity> NewsAgentRunLeases => Set<NewsAgentRunLeaseEntity>();

    public DbSet<QueenHistoryEventEntity> QueenHistoryEvents => Set<QueenHistoryEventEntity>();

    public DbSet<PhotoSubmissionEntity> PhotoSubmissions => Set<PhotoSubmissionEntity>();

    public DbSet<PhotoSubmissionAuditLogEntity> PhotoSubmissionAuditLogs => Set<PhotoSubmissionAuditLogEntity>();

    public DbSet<ArticleSubmissionEntity> ArticleSubmissions => Set<ArticleSubmissionEntity>();

    public DbSet<NewsSuggestionEntity> NewsSuggestions => Set<NewsSuggestionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NewsTableRow>(entity =>
        {
            entity.ToTable("NEWS_T", table => table.ExcludeFromMigrations());
            entity.HasKey(row => row.NewsId);

            entity.Property(row => row.NewsId)
                .HasColumnName("NEWS_ID")
                .ValueGeneratedOnAdd();
            entity.Property(row => row.Title).HasColumnName("TITLE").HasMaxLength(150);
            entity.Property(row => row.Excerpt).HasColumnName("EXCERPT").HasMaxLength(NewsValidation.MaxExcerptLength);
            entity.Property(row => row.Body).HasColumnName("ARTICLE");
            entity.Property(row => row.PublishedAt).HasColumnName("DATE");
            entity.Property(row => row.SourceUrl).HasColumnName("SOURCE_URL").HasMaxLength(NewsValidation.MaxSourceUrlLength);
            entity.Property(row => row.Slug).HasColumnName("SLUG").HasMaxLength(200);
            entity.Property(row => row.CreatedAt).HasColumnName("CREATED_AT");
            entity.Property(row => row.UpdatedAt).HasColumnName("UPDATED_AT");
            entity.Property(row => row.EditorEmail).HasColumnName("EDITOR_EMAIL").HasMaxLength(256);
            entity.Property(row => row.UserId).HasColumnName("USER_ID");
            entity.Property(row => row.Type).HasColumnName("TYPE");
            entity.Property(row => row.QueenOnline).HasColumnName("QUEEN_ONLINE");
            entity.Property(row => row.IsPublished)
                .HasColumnName("DISPLAY")
                .HasConversion(
                    value => value ? 1 : 0,
                    value => value == 1);
        });

        modelBuilder.Entity<NewsAuditLogEntity>(entity =>
        {
            entity.ToTable("NewsAuditLog");
            entity.HasKey(log => log.Id);

            entity.Property(log => log.NewsId).IsRequired();
            entity.Property(log => log.Action).HasMaxLength(50).IsRequired();
            entity.Property(log => log.ActorEmail).HasMaxLength(256).IsRequired();
            entity.Property(log => log.Details).HasMaxLength(2000);
            entity.Property(log => log.OccurredAt)
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .IsRequired();

            entity.HasIndex(log => new { log.NewsId, log.OccurredAt })
                .IsDescending(false, true)
                .HasDatabaseName("IX_NewsAuditLog_NewsId_OccurredAt");
        });

        modelBuilder.Entity<MemberAccount>(entity =>
        {
            entity.ToTable("MemberAccounts");
            entity.HasKey(account => account.Id);

            entity.Property(account => account.Email).HasMaxLength(256).IsRequired();
            entity.Property(account => account.NormalizedEmail).HasMaxLength(256).IsRequired();
            entity.Property(account => account.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(account => account.AvatarUrl).HasMaxLength(512);
            entity.Property(account => account.PasswordHash).HasMaxLength(512);
            entity.Property(account => account.CreatedAt).IsRequired();
            entity.Property(account => account.LastLoginAt);

            entity.HasIndex(account => account.NormalizedEmail)
                .IsUnique()
                .HasDatabaseName("IX_MemberAccounts_NormalizedEmail");
        });

        modelBuilder.Entity<MemberExternalLogin>(entity =>
        {
            entity.ToTable("MemberExternalLogins");
            entity.HasKey(login => login.Id);

            entity.Property(login => login.Provider).HasMaxLength(50).IsRequired();
            entity.Property(login => login.ProviderKey).HasMaxLength(256).IsRequired();
            entity.Property(login => login.Email).HasMaxLength(256).IsRequired();
            entity.Property(login => login.LinkedAt).IsRequired();

            entity.HasIndex(login => new { login.Provider, login.ProviderKey })
                .IsUnique()
                .HasDatabaseName("IX_MemberExternalLogins_Provider_ProviderKey");

            entity.HasOne<MemberAccount>()
                .WithMany()
                .HasForeignKey(login => login.MemberAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ModernForumCategoryEntity>(entity =>
        {
            entity.ToTable("ModernForumCategory", table => table.ExcludeFromMigrations());
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Name).HasMaxLength(100).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(400);
            entity.HasIndex(category => category.LegacyForumId)
                .IsUnique()
                .HasDatabaseName("UQ_ModernForumCategory_LegacyForumId");
        });

        modelBuilder.Entity<ModernForumThreadEntity>(entity =>
        {
            entity.ToTable("ModernForumThread", table => table.ExcludeFromMigrations());
            entity.HasKey(thread => thread.Id);
            entity.Property(thread => thread.Title).HasMaxLength(200).IsRequired();
            entity.Property(thread => thread.StartedByDisplayName).HasMaxLength(100).IsRequired();
            entity.Property(thread => thread.StarterAttachment).HasMaxLength(120);
            entity.Property(thread => thread.StarterFileSize).HasMaxLength(12);
            entity.HasIndex(thread => thread.LegacyTopicId)
                .IsUnique()
                .HasDatabaseName("UQ_ModernForumThread_LegacyTopicId");
            entity.HasOne(thread => thread.Category)
                .WithMany()
                .HasForeignKey(thread => thread.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ModernForumPostEntity>(entity =>
        {
            entity.ToTable("ModernForumPost", table => table.ExcludeFromMigrations());
            entity.HasKey(post => post.Id);
            entity.Property(post => post.AuthorDisplayName).HasMaxLength(100).IsRequired();
            entity.Property(post => post.BodyHtml).HasMaxLength(8000).IsUnicode(false).IsRequired();
            entity.Property(post => post.SignatureHtml).HasMaxLength(8000).IsUnicode(false);
            entity.Property(post => post.Attachment).HasMaxLength(120).IsUnicode(false);
            entity.Property(post => post.FileSize).HasMaxLength(12).IsUnicode(false);
            entity.Property(post => post.EditCount).HasDefaultValue(0);
            entity.HasIndex(post => post.LegacyPostId)
                .IsUnique()
                .HasDatabaseName("UQ_ModernForumPost_LegacyPostId");
            entity.HasIndex(post => post.AuthorMemberId)
                .HasDatabaseName("IX_ModernForumPost_AuthorMemberId");
            entity.HasOne(post => post.Thread)
                .WithMany()
                .HasForeignKey(post => post.ThreadId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ForumPostAttachmentEntity>(entity =>
        {
            entity.ToTable("ForumPostAttachments");
            entity.HasKey(attachment => attachment.Id);

            entity.Property(attachment => attachment.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(attachment => attachment.BlobPath).HasMaxLength(512).IsRequired();
            entity.Property(attachment => attachment.ContainerName).HasMaxLength(64).IsRequired();
            entity.Property(attachment => attachment.MimeType).HasMaxLength(100).IsRequired();
            entity.Property(attachment => attachment.UploadedAt).IsRequired();
            entity.Property(attachment => attachment.DownloadCount).HasDefaultValue(0);

            entity.HasIndex(attachment => attachment.LegacyPostId)
                .HasDatabaseName("IX_ForumPostAttachments_LegacyPostId");
            entity.HasIndex(attachment => attachment.PostId)
                .HasDatabaseName("IX_ForumPostAttachments_PostId");

            // ModernForumPost is excluded from EF migrations; SQL Server migration adds the FK in SQL.
            entity.HasOne(attachment => attachment.Post)
                .WithMany()
                .HasForeignKey(attachment => attachment.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ForumPollEntity>(entity =>
        {
            entity.ToTable("ForumPolls");
            entity.HasKey(poll => poll.Id);
            entity.Property(poll => poll.Question).HasMaxLength(300).IsRequired();
            entity.Property(poll => poll.CreatedAt).IsRequired();
            entity.HasIndex(poll => poll.LegacyTopicId)
                .IsUnique()
                .HasDatabaseName("UQ_ForumPolls_LegacyTopicId");
            entity.HasIndex(poll => poll.ThreadId)
                .IsUnique()
                .HasDatabaseName("UQ_ForumPolls_ThreadId");
            entity.HasOne(poll => poll.Thread)
                .WithMany()
                .HasForeignKey(poll => poll.ThreadId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(poll => poll.Options)
                .WithOne(option => option.Poll)
                .HasForeignKey(option => option.PollId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(poll => poll.Votes)
                .WithOne(vote => vote.Poll)
                .HasForeignKey(vote => vote.PollId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ForumPollOptionEntity>(entity =>
        {
            entity.ToTable("ForumPollOptions");
            entity.HasKey(option => option.Id);
            entity.Property(option => option.OptionText).HasMaxLength(200).IsRequired();
            entity.HasIndex(option => new { option.PollId, option.DisplayOrder })
                .HasDatabaseName("IX_ForumPollOptions_PollId_DisplayOrder");
        });

        modelBuilder.Entity<ForumPollVoteEntity>(entity =>
        {
            entity.ToTable("ForumPollVotes");
            entity.HasKey(vote => vote.Id);
            entity.Property(vote => vote.VotedAt).IsRequired();
            entity.HasIndex(vote => new { vote.PollId, vote.MemberAccountId, vote.OptionId })
                .IsUnique()
                .HasDatabaseName("UQ_ForumPollVotes_Poll_Member_Option");
            entity.HasOne(vote => vote.Option)
                .WithMany(option => option.Votes)
                .HasForeignKey(vote => vote.OptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NewsDiscoverySourceEntity>(entity =>
        {
            entity.ToTable("NewsDiscoverySources");
            entity.HasKey(source => source.Id);

            entity.Property(source => source.Key).HasMaxLength(100).IsRequired();
            entity.Property(source => source.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(source => source.HomepageUrl).HasMaxLength(2000).IsRequired();
            entity.Property(source => source.FeedOrSiteUrl).HasMaxLength(2000);
            entity.Property(source => source.SourceType).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(source => source.TrustTier).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(source => source.RelevanceKeywords).HasMaxLength(1000);
            entity.Property(source => source.CreatedAt).IsRequired();
            entity.Property(source => source.UpdatedAt).IsRequired();

            entity.HasIndex(source => source.Key)
                .IsUnique()
                .HasDatabaseName("IX_NewsDiscoverySources_Key");
        });

        modelBuilder.Entity<NewsCandidateEntity>(entity =>
        {
            entity.ToTable("NewsCandidates");
            entity.HasKey(candidate => candidate.Id);

            entity.Property(candidate => candidate.SourceUrl).HasMaxLength(2000).IsRequired();
            entity.Property(candidate => candidate.CanonicalUrl).HasMaxLength(2000).IsRequired();
            entity.Property(candidate => candidate.CanonicalUrlHash).HasMaxLength(64).IsRequired();
            entity.Property(candidate => candidate.SourceTitle).HasMaxLength(500).IsRequired();
            entity.Property(candidate => candidate.ContentHash).HasMaxLength(64);
            entity.Property(candidate => candidate.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(candidate => candidate.RelevanceScore).HasPrecision(5, 4);
            entity.Property(candidate => candidate.ConfidenceScore).HasPrecision(5, 4);
            entity.Property(candidate => candidate.ReviewNotes).HasMaxLength(2000);
            entity.Property(candidate => candidate.DiscoveredAt).IsRequired();
            entity.Property(candidate => candidate.CreatedAt).IsRequired();
            entity.Property(candidate => candidate.UpdatedAt).IsRequired();

            entity.HasIndex(candidate => candidate.CanonicalUrlHash)
                .IsUnique()
                .HasDatabaseName("IX_NewsCandidates_CanonicalUrlHash");

            entity.HasIndex(candidate => new { candidate.Status, candidate.DiscoveredAt })
                .IsDescending(false, true)
                .HasDatabaseName("IX_NewsCandidates_Status_DiscoveredAt");

            entity.HasIndex(candidate => candidate.ContentHash)
                .HasDatabaseName("IX_NewsCandidates_ContentHash");

            entity.HasOne(candidate => candidate.Source)
                .WithMany()
                .HasForeignKey(candidate => candidate.SourceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(candidate => candidate.DuplicateOfCandidate)
                .WithMany()
                .HasForeignKey(candidate => candidate.DuplicateOfCandidateId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NewsCandidateEvidenceEntity>(entity =>
        {
            entity.ToTable("NewsCandidateEvidence");
            entity.HasKey(evidence => evidence.Id);

            entity.Property(evidence => evidence.SourceUrl).HasMaxLength(2000).IsRequired();
            entity.Property(evidence => evidence.CanonicalUrl).HasMaxLength(2000).IsRequired();
            entity.Property(evidence => evidence.SourceName).HasMaxLength(200).IsRequired();
            entity.Property(evidence => evidence.SourceTrustTier).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(evidence => evidence.FetchedTitle).HasMaxLength(500).IsRequired();
            entity.Property(evidence => evidence.Excerpt).HasMaxLength(4000);
            entity.Property(evidence => evidence.ContentHash).HasMaxLength(64);
            entity.Property(evidence => evidence.Etag).HasMaxLength(256);
            entity.Property(evidence => evidence.FetchedAt).IsRequired();
            entity.Property(evidence => evidence.CreatedAt).IsRequired();

            entity.HasIndex(evidence => new { evidence.CandidateId, evidence.FetchedAt })
                .IsDescending(false, true)
                .HasDatabaseName("IX_NewsCandidateEvidence_CandidateId_FetchedAt");

            entity.HasOne(evidence => evidence.Candidate)
                .WithMany(candidate => candidate.Evidence)
                .HasForeignKey(evidence => evidence.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NewsAiRunEntity>(entity =>
        {
            entity.ToTable("NewsAiRuns");
            entity.HasKey(run => run.Id);

            entity.Property(run => run.Kind).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(run => run.ModelProvider).HasMaxLength(100).IsRequired();
            entity.Property(run => run.ModelId).HasMaxLength(200).IsRequired();
            entity.Property(run => run.PromptVersion).HasMaxLength(100).IsRequired();
            entity.Property(run => run.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(run => run.EstimatedCostUsd).HasPrecision(10, 6);
            entity.Property(run => run.StructuredResultJson).HasMaxLength(8000);
            entity.Property(run => run.ErrorMessage).HasMaxLength(2000);
            entity.Property(run => run.StartedAt).IsRequired();
            entity.Property(run => run.CreatedAt).IsRequired();

            entity.HasIndex(run => new { run.CandidateId, run.StartedAt })
                .IsDescending(false, true)
                .HasDatabaseName("IX_NewsAiRuns_CandidateId_StartedAt");

            entity.HasOne(run => run.Candidate)
                .WithMany(candidate => candidate.AiRuns)
                .HasForeignKey(run => run.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NewsAgentDraftEntity>(entity =>
        {
            entity.ToTable("NewsAgentDrafts");
            entity.HasKey(draft => draft.Id);

            entity.Property(draft => draft.ProposedTitle).HasMaxLength(500).IsRequired();
            entity.Property(draft => draft.ProposedSlug).HasMaxLength(200);
            entity.Property(draft => draft.ProposedExcerpt).HasMaxLength(2000).IsRequired();
            entity.Property(draft => draft.ProposedBody).IsRequired();
            entity.Property(draft => draft.AttributionText).HasMaxLength(2000);
            entity.Property(draft => draft.SourceNotes).HasMaxLength(2000);
            entity.Property(draft => draft.ConfidenceNotes).HasMaxLength(2000);
            entity.Property(draft => draft.CreatedAt).IsRequired();
            entity.Property(draft => draft.UpdatedAt).IsRequired();

            entity.HasIndex(draft => draft.CandidateId)
                .IsUnique()
                .HasDatabaseName("IX_NewsAgentDrafts_CandidateId");

            entity.HasOne(draft => draft.Candidate)
                .WithOne(candidate => candidate.Draft)
                .HasForeignKey<NewsAgentDraftEntity>(draft => draft.CandidateId)
                .OnDelete(DeleteBehavior.ClientCascade);

            entity.HasOne(draft => draft.AiRun)
                .WithMany()
                .HasForeignKey(draft => draft.AiRunId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<NewsAgentRunLeaseEntity>(entity =>
        {
            entity.ToTable("NewsAgentRunLeases");
            entity.HasKey(lease => lease.LeaseName);

            entity.Property(lease => lease.LeaseName).HasMaxLength(100).IsRequired();
            entity.Property(lease => lease.HolderId).HasMaxLength(64).IsRequired();
            entity.Property(lease => lease.AcquiredAtUtc).IsRequired();
            entity.Property(lease => lease.ExpiresAtUtc).IsRequired();
        });

        modelBuilder.Entity<QueenHistoryEventEntity>(entity =>
        {
            entity.ToTable("QueenHistoryEvents");
            entity.HasKey(historyEvent => historyEvent.Id);

            entity.Property(historyEvent => historyEvent.Title).HasMaxLength(200).IsRequired();
            entity.Property(historyEvent => historyEvent.Summary).HasMaxLength(1000).IsRequired();
            entity.Property(historyEvent => historyEvent.EventDate).IsRequired();
            entity.Property(historyEvent => historyEvent.DatePrecision).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(historyEvent => historyEvent.Category).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(historyEvent => historyEvent.Importance).IsRequired();
            entity.Property(historyEvent => historyEvent.SourceType).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(historyEvent => historyEvent.SourceKey).HasMaxLength(200).IsRequired();
            entity.Property(historyEvent => historyEvent.SourceUrl).HasMaxLength(2000);
            entity.Property(historyEvent => historyEvent.IsPublished).IsRequired();
            entity.Property(historyEvent => historyEvent.CreatedAt).IsRequired();
            entity.Property(historyEvent => historyEvent.UpdatedAt).IsRequired();

            entity.HasIndex(historyEvent => new { historyEvent.IsPublished, historyEvent.DatePrecision, historyEvent.EventDate })
                .HasDatabaseName("IX_QueenHistoryEvents_Published_Date");

            entity.HasIndex(historyEvent => new { historyEvent.SourceType, historyEvent.SourceKey })
                .IsUnique()
                .HasDatabaseName("IX_QueenHistoryEvents_Source");
        });

        modelBuilder.Entity<PhotoSubmissionEntity>(entity =>
        {
            entity.ToTable("PhotoSubmissions");
            entity.HasKey(submission => submission.Id);

            entity.Property(submission => submission.Title).HasMaxLength(200).IsRequired();
            entity.Property(submission => submission.Description).HasMaxLength(1000);
            entity.Property(submission => submission.SuggestedCategory).HasMaxLength(100);
            entity.Property(submission => submission.ApprovedCategory).HasMaxLength(100);
            entity.Property(submission => submission.BlobPath).HasMaxLength(512).IsRequired();
            entity.Property(submission => submission.WebOptimizedBlobPath).HasMaxLength(512).IsRequired();
            entity.Property(submission => submission.ThumbnailBlobPath).HasMaxLength(512).IsRequired();
            entity.Property(submission => submission.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(submission => submission.MimeType).HasMaxLength(100).IsRequired();
            entity.Property(submission => submission.Status).HasMaxLength(50).IsRequired();
            entity.Property(submission => submission.SubmittedAt).IsRequired();
            entity.Property(submission => submission.ReviewerEmail).HasMaxLength(256);
            entity.Property(submission => submission.ReviewNotes).HasMaxLength(500);
            entity.Property(submission => submission.RejectionReason).HasMaxLength(500);

            entity.HasIndex(submission => new { submission.Status, submission.SubmittedAt })
                .IsDescending(false, true)
                .HasDatabaseName("IX_PhotoSubmissions_Status_SubmittedAt");

            entity.HasIndex(submission => new { submission.SubmitterMemberId, submission.SubmittedAt })
                .IsDescending(false, true)
                .HasDatabaseName("IX_PhotoSubmissions_Submitter_SubmittedAt");

            entity.HasOne(submission => submission.Submitter)
                .WithMany()
                .HasForeignKey(submission => submission.SubmitterMemberId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ArticleSubmissionEntity>(entity =>
        {
            entity.ToTable("ArticleSubmissions");
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Title).HasMaxLength(300).IsRequired();
            entity.Property(a => a.Slug).HasMaxLength(300).IsRequired();
            entity.Property(a => a.Excerpt).HasMaxLength(500);
            entity.Property(a => a.Body).IsRequired();
            entity.Property(a => a.CoverImageBlobPath).HasMaxLength(512);
            entity.Property(a => a.Tags).HasMaxLength(500);
            entity.Property(a => a.Status).HasMaxLength(50).IsRequired();
            entity.Property(a => a.ReviewerEmail).HasMaxLength(256);
            entity.Property(a => a.ReviewNotes).HasMaxLength(1000);
            entity.Property(a => a.RejectionReason).HasMaxLength(1000);

            entity.HasIndex(a => a.Slug)
                .HasDatabaseName("IX_ArticleSubmissions_Slug");

            entity.HasIndex(a => new { a.Status, a.SubmittedAt })
                .IsDescending(false, true)
                .HasDatabaseName("IX_ArticleSubmissions_Status_SubmittedAt");

            entity.HasIndex(a => new { a.AuthorMemberId, a.SubmittedAt })
                .IsDescending(false, true)
                .HasDatabaseName("IX_ArticleSubmissions_Author_SubmittedAt");

            entity.HasOne(a => a.Author)
                .WithMany()
                .HasForeignKey(a => a.AuthorMemberId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PhotoSubmissionAuditLogEntity>(entity =>
        {
            entity.ToTable("PhotoSubmissionAuditLog");
            entity.HasKey(log => log.Id);

            entity.Property(log => log.Action).HasMaxLength(50).IsRequired();
            entity.Property(log => log.ActorEmail).HasMaxLength(256).IsRequired();
            entity.Property(log => log.OccurredAt).IsRequired();
            entity.Property(log => log.Details).HasMaxLength(2000);

            entity.HasIndex(log => new { log.PhotoSubmissionId, log.OccurredAt })
                .IsDescending(false, true)
                .HasDatabaseName("IX_PhotoSubmissionAuditLog_Submission_OccurredAt");

            entity.HasOne(log => log.Submission)
                .WithMany(submission => submission.AuditLogs)
                .HasForeignKey(log => log.PhotoSubmissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NewsSuggestionEntity>(entity =>
        {
            entity.ToTable("NewsSuggestions");
            entity.HasKey(suggestion => suggestion.Id);

            entity.Property(suggestion => suggestion.Url).HasMaxLength(2000).IsRequired();
            entity.Property(suggestion => suggestion.UrlHash).HasMaxLength(64).IsRequired();
            entity.Property(suggestion => suggestion.Title).HasMaxLength(300);
            entity.Property(suggestion => suggestion.Notes).HasMaxLength(1000);
            entity.Property(suggestion => suggestion.Status).HasMaxLength(50).IsRequired();
            entity.Property(suggestion => suggestion.SubmittedAt).IsRequired();
            entity.Property(suggestion => suggestion.ReviewerEmail).HasMaxLength(256);
            entity.Property(suggestion => suggestion.ReviewNotes).HasMaxLength(500);

            entity.HasIndex(suggestion => suggestion.UrlHash)
                .IsUnique()
                .HasFilter("[Status] IN ('Pending', 'UnderReview')")
                .HasDatabaseName("IX_NewsSuggestions_UrlHash_Active");

            entity.HasIndex(suggestion => new { suggestion.Status, suggestion.SubmittedAt })
                .IsDescending(false, true)
                .HasDatabaseName("IX_NewsSuggestions_Status_SubmittedAt");

            entity.HasIndex(suggestion => new { suggestion.SubmitterMemberId, suggestion.SubmittedAt })
                .IsDescending(false, true)
                .HasDatabaseName("IX_NewsSuggestions_Submitter_SubmittedAt");

            entity.HasOne(suggestion => suggestion.Submitter)
                .WithMany()
                .HasForeignKey(suggestion => suggestion.SubmitterMemberId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(suggestion => suggestion.DuplicateCandidate)
                .WithMany()
                .HasForeignKey(suggestion => suggestion.DuplicateCandidateId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
