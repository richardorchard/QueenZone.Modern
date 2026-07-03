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

    public DbSet<NewsDiscoverySourceEntity> NewsDiscoverySources => Set<NewsDiscoverySourceEntity>();

    public DbSet<NewsCandidateEntity> NewsCandidates => Set<NewsCandidateEntity>();

    public DbSet<NewsCandidateEvidenceEntity> NewsCandidateEvidence => Set<NewsCandidateEvidenceEntity>();

    public DbSet<NewsAiRunEntity> NewsAiRuns => Set<NewsAiRunEntity>();

    public DbSet<NewsAgentDraftEntity> NewsAgentDrafts => Set<NewsAgentDraftEntity>();

    public DbSet<NewsAgentRunLeaseEntity> NewsAgentRunLeases => Set<NewsAgentRunLeaseEntity>();

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
            entity.Property(row => row.Excerpt).HasColumnName("EXCERPT");
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
            entity.Property(account => account.PasswordHash).HasMaxLength(512);
            entity.Property(account => account.CreatedAt).IsRequired();

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
    }
}
