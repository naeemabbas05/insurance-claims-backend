using Microsoft.EntityFrameworkCore;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence;

public sealed class ClaimsManagementDbContext(
    DbContextOptions<ClaimsManagementDbContext> options) : DbContext(options)
{
}
