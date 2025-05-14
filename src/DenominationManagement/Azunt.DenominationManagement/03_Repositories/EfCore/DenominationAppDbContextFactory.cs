using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Azunt.DenominationManagement;

public class DenominationAppDbContextFactory
{
    private readonly IConfiguration? _configuration;

    public DenominationAppDbContextFactory() { }

    public DenominationAppDbContextFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public DenominationAppDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<DenominationAppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new DenominationAppDbContext(options);
    }

    public DenominationAppDbContext CreateDbContext(DbContextOptions<DenominationAppDbContext> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new DenominationAppDbContext(options);
    }

    public DenominationAppDbContext CreateDbContext()
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