using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Azunt.RuleManagement
{
    /// <summary>
    /// Rules 테이블을 생성/보강하는 TableBuilder 클래스
    /// - 리소스(ResourceId)별 사용자/그룹/역할 권한 설정
    /// </summary>
    public class RulesTableBuilder
    {
        private readonly string _masterConnectionString;
        private readonly ILogger<RulesTableBuilder> _logger;

        public RulesTableBuilder(string masterConnectionString, ILogger<RulesTableBuilder> logger)
        {
            _masterConnectionString = masterConnectionString;
            _logger = logger;
        }

        public void BuildTenantDatabases()
        {
            var tenantConnectionStrings = GetTenantConnectionStrings();
            foreach (var connStr in tenantConnectionStrings)
            {
                try
                {
                    EnsureRulesTable(connStr);
                    _logger.LogInformation($"Rules table processed (tenant DB): {connStr}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[{connStr}] Error processing tenant DB (Rules)");
                }
            }
        }

        public void BuildMasterDatabase()
        {
            try
            {
                EnsureRulesTable(_masterConnectionString);
                _logger.LogInformation("Rules table processed (master DB)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing master DB (Rules)");
            }
        }

        private List<string> GetTenantConnectionStrings()
        {
            var result = new List<string>();

            using var connection = new SqlConnection(_masterConnectionString);
            connection.Open();
            using var cmd = new SqlCommand("SELECT ConnectionString FROM dbo.Tenants", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var connStr = reader["ConnectionString"]?.ToString();
                if (!string.IsNullOrEmpty(connStr))
                {
                    result.Add(connStr);
                }
            }
            return result;
        }

        private void EnsureRulesTable(string connectionString)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            var cmdCheck = new SqlCommand(@"
                SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_NAME = 'Rules'", connection);
            int tableExists = (int)cmdCheck.ExecuteScalar();

            if (tableExists == 0)
            {
                var createCmd = new SqlCommand(@"
                    CREATE TABLE [dbo].[Rules] (
                        [Id] INT NOT NULL PRIMARY KEY IDENTITY(1, 1),
                        [ResourceId] INT,
                        [AccountId] NVARCHAR(100) NULL,
                        [AccountType] NVARCHAR(100) NULL,
                        [NoAccess] BIT DEFAULT(0),
                        [List] BIT DEFAULT(1),
                        [ReadArticle] BIT DEFAULT(1),
                        [Download] BIT DEFAULT(1),
                        [Write] BIT DEFAULT(1),
                        [Upload] BIT DEFAULT(1),
                        [Extra] BIT DEFAULT(0),
                        [Admin] BIT DEFAULT(0),
                        [Comment] BIT DEFAULT(1),
                        [Menu] BIT DEFAULT(1)
                    )", connection);

                createCmd.ExecuteNonQuery();
                _logger.LogInformation("Rules table created.");
            }
            else
            {
                var expectedColumns = new Dictionary<string, string>
                {
                    ["ResourceId"] = "INT",
                    ["AccountId"] = "NVARCHAR(100) NULL",
                    ["AccountType"] = "NVARCHAR(100) NULL",
                    ["NoAccess"] = "BIT DEFAULT(0)",
                    ["List"] = "BIT DEFAULT(1)",
                    ["ReadArticle"] = "BIT DEFAULT(1)",
                    ["Download"] = "BIT DEFAULT(1)",
                    ["Write"] = "BIT DEFAULT(1)",
                    ["Upload"] = "BIT DEFAULT(1)",
                    ["Extra"] = "BIT DEFAULT(0)",
                    ["Admin"] = "BIT DEFAULT(0)",
                    ["Comment"] = "BIT DEFAULT(1)",
                    ["Menu"] = "BIT DEFAULT(1)"
                };

                foreach (var (columnName, columnType) in expectedColumns)
                {
                    var cmdColumnCheck = new SqlCommand(@"
                        SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'Rules' AND COLUMN_NAME = @ColumnName", connection);
                    cmdColumnCheck.Parameters.AddWithValue("@ColumnName", columnName);

                    int columnExists = (int)cmdColumnCheck.ExecuteScalar();

                    if (columnExists == 0)
                    {
                        var alterCmd = new SqlCommand(
                            $"ALTER TABLE [dbo].[Rules] ADD [{columnName}] {columnType}", connection);
                        alterCmd.ExecuteNonQuery();

                        _logger.LogInformation($"Column added: {columnName} ({columnType})");
                    }
                }
            }
        }

        public static void Run(IServiceProvider services, bool forMaster)
        {
            try
            {
                var logger = services.GetRequiredService<ILogger<RulesTableBuilder>>();
                var config = services.GetRequiredService<IConfiguration>();
                var masterConnectionString = config.GetConnectionString("DefaultConnection");

                if (string.IsNullOrWhiteSpace(masterConnectionString))
                    throw new InvalidOperationException("DefaultConnection is not configured in appsettings.json.");

                var builder = new RulesTableBuilder(masterConnectionString, logger);

                if (forMaster)
                    builder.BuildMasterDatabase();
                else
                    builder.BuildTenantDatabases();
            }
            catch (Exception ex)
            {
                var fallbackLogger = services.GetService<ILogger<RulesTableBuilder>>();
                fallbackLogger?.LogError(ex, "Error while processing Rules table.");
            }
        }
    }
}
