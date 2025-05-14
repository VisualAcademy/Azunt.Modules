using Dul.Articles;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Azunt.DenominationManagement;

public class DenominationRepositoryAdoNet : IDenominationRepository
{
    private readonly string _connectionString;
    private readonly ILogger<DenominationRepositoryAdoNet> _logger;

    public DenominationRepositoryAdoNet(string connectionString, ILoggerFactory loggerFactory)
    {
        _connectionString = connectionString;
        _logger = loggerFactory.CreateLogger<DenominationRepositoryAdoNet>();
    }

    private SqlConnection GetConnection() => new(_connectionString);

    public async Task<Denomination> AddAsync(Denomination model)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Denominations (Active, Created, CreatedBy, Name, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@Active, @Created, @CreatedBy, @Name, 0)";
        cmd.Parameters.AddWithValue("@Active", model.Active ?? true);
        cmd.Parameters.AddWithValue("@Created", DateTimeOffset.UtcNow);
        cmd.Parameters.AddWithValue("@CreatedBy", model.CreatedBy ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Name", model.Name ?? (object)DBNull.Value);

        await conn.OpenAsync();
        var result = await cmd.ExecuteScalarAsync();
        if (result == null)
        {
            throw new InvalidOperationException("Failed to insert Denomination. No ID was returned.");
        }
        model.Id = (long)result;
        return model;
    }

    public async Task<IEnumerable<Denomination>> GetAllAsync()
    {
        var result = new List<Denomination>();
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Active, Created, CreatedBy, Name FROM Denominations WHERE IsDeleted = 0 ORDER BY Id DESC";

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new Denomination
            {
                Id = reader.GetInt64(0),
                Active = reader.IsDBNull(1) ? (bool?)null : reader.GetBoolean(1),
                Created = reader.GetDateTimeOffset(2),
                CreatedBy = reader.IsDBNull(3) ? null : reader.GetString(3),
                Name = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }

        return result;
    }

    public async Task<Denomination> GetByIdAsync(long id)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Active, Created, CreatedBy, Name FROM Denominations WHERE Id = @Id AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("@Id", id);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Denomination
            {
                Id = reader.GetInt64(0),
                Active = reader.IsDBNull(1) ? (bool?)null : reader.GetBoolean(1),
                Created = reader.GetDateTimeOffset(2),
                CreatedBy = reader.IsDBNull(3) ? null : reader.GetString(3),
                Name = reader.IsDBNull(4) ? null : reader.GetString(4)
            };
        }

        return new Denomination();
    }

    public async Task<bool> UpdateAsync(Denomination model)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Denominations SET
                Active = @Active,
                Name = @Name
            WHERE Id = @Id AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("@Active", model.Active ?? true);
        cmd.Parameters.AddWithValue("@Name", model.Name ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Id", model.Id);

        await conn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var conn = GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Denominations SET IsDeleted = 1 WHERE Id = @Id AND IsDeleted = 0";
        cmd.Parameters.AddWithValue("@Id", id);

        await conn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
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

    public async Task<ArticleSet<Denomination, long>> GetAllAsync<TParentIdentifier>(FilterOptions<TParentIdentifier> options)
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