using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WhoSabi.Infrastructure.Persistence;

// Design-time migration generation must never boot the API or apply changes to a live database.
public sealed class WhoSabiDbContextFactory : IDesignTimeDbContextFactory<WhoSabiDbContext>
{
    public WhoSabiDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<WhoSabiDbContext>()
        .UseNpgsql("Host=localhost;Database=whosabi_design_only;Username=design_only;Password=design_only")
        .Options);
}
