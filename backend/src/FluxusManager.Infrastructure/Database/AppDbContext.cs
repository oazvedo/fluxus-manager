using Microsoft.EntityFrameworkCore;

namespace FluxusManager.Infrastructure.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Mapeamentos de cada entidade ficam em DatabaseConfigs/ (IEntityTypeConfiguration<T>)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
