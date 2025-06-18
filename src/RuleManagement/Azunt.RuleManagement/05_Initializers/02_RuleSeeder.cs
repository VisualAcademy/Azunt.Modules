using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Azunt.RuleManagement;

public static class RuleSeeder
{
    public static void SeedAdministratorRoleRules(string connectionString, ILogger logger)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        // 0. 필수 테이블 존재 여부 확인
        if (!TableExists(connection, "AspNetRoles"))
        {
            logger.LogWarning("AspNetRoles 테이블이 없어 권한 시드를 건너뜁니다.");
            return;
        }

        if (!TableExists(connection, "Rules"))
        {
            logger.LogWarning("Rules 테이블이 없어 권한 시드를 건너뜁니다.");
            return;
        }

        // 1. Administrators 역할 ID 조회
        var getRoleIdCmd = new SqlCommand(@"
            SELECT TOP 1 Id FROM AspNetRoles WHERE NormalizedName = 'ADMINISTRATORS'", connection);

        var roleId = getRoleIdCmd.ExecuteScalar() as string;

        if (string.IsNullOrWhiteSpace(roleId))
        {
            logger.LogWarning("Administrators 역할이 존재하지 않아 권한 시드를 건너뜁니다.");
            return;
        }

        // 2. 모든 리소스 ID 조회
        var resourceIds = new List<int>();
        using (var getResourcesCmd = new SqlCommand("SELECT Id FROM Resources", connection))
        using (var reader = getResourcesCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                resourceIds.Add(reader.GetInt32(0));
            }
        }

        // 3. Rules 삽입 (중복 제외)
        foreach (var resourceId in resourceIds)
        {
            var checkCmd = new SqlCommand(@"
                SELECT COUNT(*) FROM Rules
                WHERE ResourceId = @ResourceId AND AccountId = @AccountId AND AccountType = 'Role'", connection);

            checkCmd.Parameters.AddWithValue("@ResourceId", resourceId);
            checkCmd.Parameters.AddWithValue("@AccountId", roleId);

            var exists = (int)checkCmd.ExecuteScalar();
            if (exists > 0) continue;

            var insertCmd = new SqlCommand(@"
                INSERT INTO Rules (
                    ResourceId, AccountId, AccountType,
                    NoAccess, List, ReadArticle, Download, Write,
                    Upload, Extra, Admin, Comment, Menu)
                VALUES (
                    @ResourceId, @AccountId, 'Role',
                    0, 1, 1, 1, 1,
                    1, 1, 1, 1, 1)", connection);

            insertCmd.Parameters.AddWithValue("@ResourceId", resourceId);
            insertCmd.Parameters.AddWithValue("@AccountId", roleId);

            insertCmd.ExecuteNonQuery();
            logger.LogInformation($"[INSERT] Role 'Administrators' granted full access to ResourceId={resourceId}");
        }

        logger.LogInformation("Administrators 역할에 대한 Rules 권한 삽입 완료.");
    }

    private static bool TableExists(SqlConnection connection, string tableName)
    {
        var checkCmd = new SqlCommand(@"
            SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_NAME = @TableName", connection);
        checkCmd.Parameters.AddWithValue("@TableName", tableName);

        var count = (int)checkCmd.ExecuteScalar();
        return count > 0;
    }
}
