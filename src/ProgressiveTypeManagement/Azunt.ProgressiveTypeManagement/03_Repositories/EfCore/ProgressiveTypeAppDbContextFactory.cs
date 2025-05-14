using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Azunt.ProgressiveTypeManagement;

public class ProgressiveTypeAppDbContextFactory
{
    private readonly IConfiguration? _configuration;

    public ProgressiveTypeAppDbContextFactory() { }

    public ProgressiveTypeAppDbContextFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public ProgressiveTypeAppDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ProgressiveTypeAppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ProgressiveTypeAppDbContext(options);
    }

    public ProgressiveTypeAppDbContext CreateDbContext(DbContextOptions<ProgressiveTypeAppDbContext> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new ProgressiveTypeAppDbContext(options);
    }

    public ProgressiveTypeAppDbContext CreateDbContext()
    {
        if (_configuration == null)
        {
            throw new InvalidOperationException("Configuration is not provided.");
        }

        var defaultConnection = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(defaultConnection))
        {
            throw new InvalidOperationException("DefaultConnection is not configured properly.");
        }

        return CreateDbContext(defaultConnection);
    }
}