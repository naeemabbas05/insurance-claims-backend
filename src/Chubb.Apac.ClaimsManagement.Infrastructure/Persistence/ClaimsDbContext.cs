using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using StatusHistoryEntity = Chubb.Apac.ClaimsManagement.Domain.Entities.ClaimStatusHistory;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence;

public sealed class ClaimsDbContext(DbContextOptions<ClaimsDbContext> options) : DbContext(options)
{
    public DbSet<MarketDefinition> Markets => Set<MarketDefinition>();

    public DbSet<Policy> Policies => Set<Policy>();

    public DbSet<Claim> Claims => Set<Claim>();

    public DbSet<Claimant> Claimants => Set<Claimant>();

    public DbSet<ClaimDocument> ClaimDocuments => Set<ClaimDocument>();

    public DbSet<ClaimAssignment> ClaimAssignments => Set<ClaimAssignment>();

    public DbSet<ClaimAssessment> ClaimAssessments => Set<ClaimAssessment>();

    public DbSet<ClaimStatusHistory> ClaimStatusHistory => Set<ClaimStatusHistory>();

    public DbSet<ClaimAuditEntry> ClaimAuditHistory => Set<ClaimAuditEntry>();

    public DbSet<ClaimInformationRequest> ClaimInformationRequests => Set<ClaimInformationRequest>();

    public DbSet<ClaimSettlement> ClaimSettlements => Set<ClaimSettlement>();

    public DbSet<StaffUser> StaffUsers => Set<StaffUser>();

    public DbSet<ClaimSubmissionIdempotencyRecord> ClaimSubmissionIdempotencyRecords =>
        Set<ClaimSubmissionIdempotencyRecord>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        MarkNewClaimChildrenAsAdded();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        MarkNewClaimChildrenAsAdded();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("claims");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClaimsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    private void MarkNewClaimChildrenAsAdded()
    {
        ChangeTracker.DetectChanges();

        foreach (var entry in ChangeTracker.Entries()
                     .Where(entry =>
                         entry.State == EntityState.Modified &&
                         IsClaimChild(entry.Entity)))
        {
            var rowVersion = entry.Property("RowVersion");
            if (rowVersion.CurrentValue is null && rowVersion.OriginalValue is null)
            {
                // Client-generated child IDs can make newly appended entities look modified.
                entry.State = EntityState.Added;
            }
        }
    }

    private static bool IsClaimChild(object entity) =>
        entity is Claimant or
            ClaimDocument or
            ClaimAssignment or
            ClaimAssessment or
            StatusHistoryEntity or
            ClaimAuditEntry or
            ClaimInformationRequest or
            ClaimSettlement;
}
