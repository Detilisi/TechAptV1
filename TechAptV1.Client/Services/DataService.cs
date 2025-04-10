// Copyright © 2025 Always Active Technologies PTY Ltd

using Microsoft.Data.Sqlite;
using TechAptV1.Client.Models;

namespace TechAptV1.Client.Services;

/// <summary>
/// Data Access Service for interfacing with the SQLite Database
/// </summary>
public sealed class DataService
{
    private readonly string _connectionString;
    private readonly ILogger<DataService> _logger;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Default constructor providing DI Logger and Configuration
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="configuration"></param>
    public DataService(ILogger<DataService> logger, IConfiguration configuration)
    {
        this._logger = logger;
        this._configuration = configuration;
        this._connectionString = _configuration.GetConnectionString("Default")
                           ?? throw new InvalidOperationException("Missing DB connection string");

        EnsureTableExists();
    }

    /// <summary>
    /// Ensure the SQLite Database and Table exist
    /// </summary>
    private void EnsureTableExists()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Number (
                Value INTEGER NOT NULL,
                IsPrime INTEGER NOT NULL DEFAULT 0
            );";

            command.ExecuteNonQuery();
            _logger.LogInformation("Ensured 'Number' table exists.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure the 'Number' table exists.");
            throw;
        }
    }


    /// <summary>
    /// Save the list of data to the SQLite Database
    /// </summary>
    /// <param name="dataList"></param>
    public async Task Save(List<Number> dataList)
    {
        this._logger.LogInformation("Save");
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();
        var command = connection.CreateCommand();

        command.CommandText = "INSERT INTO Number (Value, IsPrime) VALUES (@value, @isPrime)";

        foreach (var number in dataList)
        {
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@value", number.Value);
            command.Parameters.AddWithValue("@isPrime", number.IsPrime);
            await command.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();

        _logger.LogInformation($"Saved {dataList.Count} numbers.");

    }

    /// <summary>
    /// Fetch N records from the SQLite Database where N is specified by the count parameter
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public IEnumerable<Number> Get(int count)
    {
        this._logger.LogInformation("Get");
        var result = new List<Number>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value, IsPrime FROM Number LIMIT @count";
        command.Parameters.AddWithValue("@count", count);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Number
            {
                Value = reader.GetInt32(0),
                IsPrime = reader.GetInt32(1)
            });
        }

        return result;
    }

    /// <summary>
    /// Fetch All the records from the SQLite Database
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Number> GetAll()
    {
        this._logger.LogInformation("GetAll");
        var result = new List<Number>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value, IsPrime FROM Number";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Number
            {
                Value = reader.GetInt32(0),
                IsPrime = reader.GetInt32(1)
            });
        }

        return result;
    }
}
