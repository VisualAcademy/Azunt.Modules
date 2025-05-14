using Azunt.ProgressiveTypeManagement;
using Azunt.Repositories;
using Dul.Articles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Azunt.ProgressiveTypeManagement;

/// <summary>
/// ProgressiveType 테이블에 대한 Entity Framework Core 기반 리포지토리 구현체입니다.
/// Blazor Server 회로 유지 이슈를 피하고, 멀티테넌트 연결 문자열 지원을 위해 팩터리 사용.
/// </summary>
public class ProgressiveTypeRepository : IProgressiveTypeRepository
{
    private readonly ProgressiveTypeAppDbContextFactory _factory;
    private readonly ILogger<ProgressiveTypeRepository> _logger;
    private readonly string? _connectionString;

    public ProgressiveTypeRepository(
        ProgressiveTypeAppDbContextFactory factory,
        ILoggerFactory loggerFactory)
    {
        _factory = factory;
        _logger = loggerFactory.CreateLogger<ProgressiveTypeRepository>();
    }

    public ProgressiveTypeRepository(
        ProgressiveTypeAppDbContextFactory factory,
        ILoggerFactory loggerFactory,
        string connectionString)
    {
        _factory = factory;
        _logger = loggerFactory.CreateLogger<ProgressiveTypeRepository>();
        _connectionString = connectionString;
    }

    private ProgressiveTypeAppDbContext CreateContext() =>
        string.IsNullOrWhiteSpace(_connectionString)
            ? _factory.CreateDbContext()
            : _factory.CreateDbContext(_connectionString);

    public async Task<ProgressiveType> AddAsyncDefault(ProgressiveType model)
    {
        await using var context = CreateContext();
        model.Created = DateTime.UtcNow;
        model.IsDeleted = false;
        context.ProgressiveTypes.Add(model);
        await context.SaveChangesAsync();
        return model;
    }

    public async Task<ProgressiveType> AddAsync(ProgressiveType model)
    {
        await using var context = CreateContext();
        model.Created = DateTime.UtcNow;
        model.IsDeleted = false;

        // 현재 가장 높은 DisplayOrder 값 조회
        var maxDisplayOrder = await context.ProgressiveTypes
            .Where(m => !m.IsDeleted)
            .MaxAsync(m => (int?)m.DisplayOrder) ?? 0;

        model.DisplayOrder = maxDisplayOrder + 1;

        context.ProgressiveTypes.Add(model);
        await context.SaveChangesAsync();
        return model;
    }

    public async Task<IEnumerable<ProgressiveType>> GetAllAsync()
    {
        await using var context = CreateContext();
        return await context.ProgressiveTypes
            .Where(m => !m.IsDeleted)
            //.OrderByDescending(m => m.Id)
            .OrderBy(m => m.DisplayOrder) // 정렬 순서 변경
            .ToListAsync();
    }

    public async Task<ProgressiveType> GetByIdAsync(long id)
    {
        await using var context = CreateContext();
        return await context.ProgressiveTypes
            .Where(m => m.Id == id && !m.IsDeleted)
            .SingleOrDefaultAsync()
            ?? new ProgressiveType();
    }

    public async Task<bool> UpdateAsync(ProgressiveType model)
    {
        await using var context = CreateContext();
        context.Attach(model);
        context.Entry(model).State = EntityState.Modified;
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        await using var context = CreateContext();
        var entity = await context.ProgressiveTypes.FindAsync(id);
        if (entity == null || entity.IsDeleted) return false;

        entity.IsDeleted = true;
        context.ProgressiveTypes.Update(entity);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<ArticleSet<ProgressiveType, int>> GetAllAsync<TParentIdentifier>(
        int pageIndex,
        int pageSize,
        string searchField,
        string searchQuery,
        string sortOrder,
        TParentIdentifier parentIdentifier)
    {
        await using var context = CreateContext();
        var query = context.ProgressiveTypes
            .Where(m => !m.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(m => m.Name != null && m.Name.Contains(searchQuery));
        }

        query = sortOrder switch
        {
            "Name" => query.OrderBy(m => m.Name),
            "NameDesc" => query.OrderByDescending(m => m.Name),
            "DisplayOrder" => query.OrderBy(m => m.DisplayOrder),
            _ => query.OrderBy(m => m.DisplayOrder) // 기본 정렬도 DisplayOrder
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new ArticleSet<ProgressiveType, int>(items, totalCount);
    }

    public async Task<ArticleSet<ProgressiveType, long>> GetAllAsync<TParentIdentifier>(
        FilterOptions<TParentIdentifier> options)
    {
        await using var context = CreateContext();
        var query = context.ProgressiveTypes
            .Where(m => !m.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrEmpty(options.SearchQuery))
        {
            query = query.Where(m => m.Name != null && m.Name.Contains(options.SearchQuery));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(m => m.DisplayOrder)
            .Skip(options.PageIndex * options.PageSize)
            .Take(options.PageSize)
            .ToListAsync();

        return new ArticleSet<ProgressiveType, long>(items, totalCount);
    }

    public async Task<bool> MoveUpAsync(long id)
    {
        await using var context = CreateContext();
        var current = await context.ProgressiveTypes.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (current == null) return false;

        var upper = await context.ProgressiveTypes
            .Where(x => x.DisplayOrder < current.DisplayOrder && !x.IsDeleted)
            .OrderByDescending(x => x.DisplayOrder)
            .FirstOrDefaultAsync();

        if (upper == null) return false;

        // Swap
        int temp = current.DisplayOrder;
        current.DisplayOrder = upper.DisplayOrder;
        upper.DisplayOrder = temp;

        // 명시적 변경 추적
        context.ProgressiveTypes.Update(current);
        context.ProgressiveTypes.Update(upper);

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MoveDownAsync(long id)
    {
        await using var context = CreateContext();
        var current = await context.ProgressiveTypes.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (current == null) return false;

        var lower = await context.ProgressiveTypes
            .Where(x => x.DisplayOrder > current.DisplayOrder && !x.IsDeleted)
            .OrderBy(x => x.DisplayOrder)
            .FirstOrDefaultAsync();

        if (lower == null) return false;

        // Swap
        int temp = current.DisplayOrder;
        current.DisplayOrder = lower.DisplayOrder;
        lower.DisplayOrder = temp;

        // 명시적 변경 추적
        context.ProgressiveTypes.Update(current);
        context.ProgressiveTypes.Update(lower);

        await context.SaveChangesAsync();
        return true;
    }
}
