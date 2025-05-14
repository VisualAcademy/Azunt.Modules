using Azunt.Repositories;

namespace Azunt.ProgressiveTypeManagement;

/// <summary>
/// 기본 CRUD 작업을 위한 ProgressiveType 전용 저장소 인터페이스
/// </summary>
public interface IProgressiveTypeBaseRepository : IRepositoryBase<ProgressiveType, long>
{
}