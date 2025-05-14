using Dapper;
using Dul.Articles;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Azunt.DenominationManagement;

public class DenominationRepositoryDapper : IDenominationRepository
{
    private readonly string _connectionString;
    private readonly ILogger<DenominationRepositoryDapper> _logger;

    public DenominationRepositoryDapper(string connectionString, ILoggerFactory loggerFactory)
    {
        _connectionString = connectionString;
        _logger = loggerFactory.CreateLogger<DenominationRepositoryDapper>();
    }

    private SqlConnection GetConnection() => new(_connectionString);

    public async Task<Denomination> AddAsync(Denomination model)
    {
        const string sql = @"
            INSERT INTO Denominations (Active, Created, CreatedBy, Name, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@Active, @Created, @CreatedBy, @Name, 0)";

        model.Created = DateTimeOffset.UtcNow;

        using var conn = GetConnection();
        model.Id = await conn.ExecuteScalarAsync<long>(sql, model);
        return model;
    }

    public async Task<IEnumerable<Denomination>> GetAllAsync()
    {
        const string sql = @"
            SELECT Id, Active, Created, CreatedBy, Name 
            FROM Denominations 
            WHERE IsDeleted = 0 
            ORDER BY Id DESC";

        using var conn = GetConnection();
        return await conn.QueryAsync<Denomination>(sql);
    }

    public async Task<Denomination> GetByIdAsync(long id)
    {
        const string sql = @"
            SELECT Id, Active, Created, CreatedBy, Name 
            FROM Denominations 
            WHERE Id = @Id AND IsDeleted = 0";

        using var conn = GetConnection();
        return await conn.QuerySingleOrDefaultAsync<Denomination>(sql, new { Id = id }) ?? new Denomination();
    }

    public async Task<bool> UpdateAsync(Denomination model)
    {
        const string sql = @"
            UPDATE Denominations SET
                Active = @Active,
                Name = @Name
            WHERE Id = @Id AND IsDeleted = 0";

        using var conn = GetConnection();
        var affected = await conn.ExecuteAsync(sql, model);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        const string sql = @"
            UPDATE Denominations SET IsDeleted = 1 
            WHERE Id = @Id AND IsDeleted = 0";

        using var conn = GetConnection();
        var affected = await conn.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    public async Task<ArticleSet<Denomination, int>> GetAllAsync<TParentIdentifier>(
        int pageIndex, int pageSize, string searchField, string searchQuery, string sortOrder, TParentIdentifier parentIdentifier)
    {
        var all = await GetAllAsync();
        var filtered = string.IsNullOrWhiteSpace(searchQuery)
            ? all
            : all.Where(m => m.Name != null && m.Name.Contains(searchQuery)).ToList();

        var paged = filtered
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        return new ArticleSet<Denomination, int>(paged, filtered.Count());
    }

    public async Task<ArticleSet<Denomination, long>> GetAllAsync<TParentIdentifier>(Dul.Articles.FilterOptions<TParentIdentifier> options)
    {
        var all = await GetAllAsync();
        var filtered = all
            .Where(m => string.IsNullOrWhiteSpace(options.SearchQuery)
                     || (m.Name != null && m.Name.Contains(options.SearchQuery)))
            .ToList();

        var paged = filtered
            .Skip(options.PageIndex * options.PageSize)
            .Take(options.PageSize)
            .ToList();

        return new ArticleSet<Denomination, long>(paged, filtered.Count);
    }
}