using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class QueenZoneDbContext(DbContextOptions<QueenZoneDbContext> options) : DbContext(options)
{
    public DbSet<NewsTableRow> NewsRows => Set<NewsTableRow>();

    public DbSet<NewsAuditLogEntity> NewsAuditLogs => Set<NewsAuditLogEntity>();

    public DbSet<MemberAccount> MemberAccounts => Set<MemberAccount>();

    public DbSet<MemberExternalLogin> MemberExternalLogins => Set<MemberExternalLogin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NewsTableRow>(entity =>
        {
            entity.ToTable("NEWS_T", table => table.ExcludeFromMigrations());
            entity.HasKey(row => row.NewsId);

            entity.Property(row => row.NewsId).HasColumnName("NEWS_ID");
            entity.Property(row => row.Title).HasColumnName("TITLE").HasMaxLength(500);
            entity.Property(row => row.Excerpt).HasColumnName("EXCERPT");
            entity.Property(row => row.Body).HasColumnName("ARTICLE");
            entity.Property(row => row.PublishedAt).HasColumnName("DATE");
            entity.Property(row => row.SourceUrl).HasColumnName("SOURCE_URL");
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
    }
}