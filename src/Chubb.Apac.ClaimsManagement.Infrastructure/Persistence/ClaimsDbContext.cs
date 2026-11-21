using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

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

    public DbSet<ClaimInformationRequest> ClaimInformationRequests => Set<ClaimInformationRequest>();

    public DbSet<ClaimSettlement> ClaimSettlements => Set<ClaimSettlement>();

    public DbSet<StaffUser> StaffUsers => Set<StaffUser>();

    public DbSet<ClaimSubmissionIdempotencyRecord> ClaimSubmissionIdempotencyRecords =>
        Set<ClaimSubmissionIdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("claims");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClaimsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
