using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Azunt.RuleManagement;

public static class RuleSeeder
{
    public static void SeedAdministratorRoleRules(string connectionString, ILogger logger)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        // 0. Required table existence check
        if (!TableExists(connection, "AspNetRoles"))
        {
            logger.LogWarning("AspNetRoles table does not exist. Skipping Administrator role seeding.");
            return;
        }

        if (!TableExists(connection, "Rules"))
        {
            logger.LogWarning("Rules table does not exist. Skipping Administrator role seeding.");
            return;
        }

        if (!TableExists(connection, "Resources"))
        {
            logger.LogWarning("Resources table does not exist. Skipping Administrator role seeding.");
            return;
        }

        // 1. Retrieve Administrators role ID
        var getRoleIdCmd = new SqlCommand(@"
            SELECT TOP 1 Id FROM AspNetRoles WHERE NormalizedName = 'ADMINISTRATORS'", connection);

        var roleId = getRoleIdCmd.ExecuteScalar() as string;

        if (string.IsNullOrWhiteSpace(roleId))
        {
            logger.LogWarning("Administrators role does not exist. Skipping rule seeding.");
            return;
        }

        // 2. Retrieve all Resource IDs
        var resourceIds = new List<int>();
        using (var getResourcesCmd = new SqlCommand("SELECT Id FROM Resources", connection))
        using (var reader = getResourcesCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                resourceIds.Add(reader.GetInt32(0));
            }
        }

        // 3. Insert rules (if not already present)
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
            logger.LogInformation($"[INSERT] Full permissions granted to 'Administrators' for ResourceId = {resourceId}");
        }

        logger.LogInformation("Administrator role permissions seeding completed.");
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
