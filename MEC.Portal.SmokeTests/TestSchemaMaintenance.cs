using MySqlConnector;

internal static class TestSchemaMaintenance
{
    public static async Task RunAsync(bool apply)
    {
        var portalPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../MEC.Portal"));
        var configuration = new ConfigurationBuilder().SetBasePath(portalPath)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables().Build();
        if (configuration["AppSettings:Environment"] != "Test")
            throw new InvalidOperationException("This maintenance command only supports the configured Test database.");
        await using var connection = new MySqlConnection(configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();
        Console.WriteLine($"Test database: {connection.DataSource}/{connection.Database}");
        var checkpointExists = await HasColumnAsync(connection, "employee_portal", "annual_leave_processed_through");
        var managerExists = await HasColumnAsync(connection, "location", "manager_employee_portal_id");
        Console.WriteLine($"annual_leave_processed_through: {checkpointExists}; manager_employee_portal_id: {managerExists}");
        if (!apply) return;
        if (checkpointExists && managerExists)
        {
            Console.WriteLine("Schema already present; no changes applied.");
            return;
        }
        if (checkpointExists || managerExists)
            throw new InvalidOperationException("Partially applied schema; inspect the database before continuing.");
        var sql = await File.ReadAllTextAsync(Path.Combine(portalPath, "DatabaseScripts/mysql_school_managers_annual_job.sql"));
        await using var command = new MySqlCommand(sql, connection) { CommandTimeout = 60 };
        await command.ExecuteNonQueryAsync();
        if (!await HasColumnAsync(connection, "employee_portal", "annual_leave_processed_through") ||
            !await HasColumnAsync(connection, "location", "manager_employee_portal_id"))
            throw new InvalidOperationException("Schema verification failed.");
        Console.WriteLine("Test schema migration completed and verified.");
    }

    private static async Task<bool> HasColumnAsync(MySqlConnection connection, string table, string column)
    {
        await using var command = new MySqlCommand(
            "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = @table AND column_name = @column", connection);
        command.Parameters.AddWithValue("@table", table);
        command.Parameters.AddWithValue("@column", column);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }
}
