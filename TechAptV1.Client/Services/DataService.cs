﻿// Copyright © 2025 Always Active Technologies PTY Ltd

using System.Text;
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
        _logger = logger;
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("Default")
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
        try
        {
            _logger.LogInformation("Saving to Number database table...");
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            
            var valuesList = new List<string>();
            var sqlBuilder = new StringBuilder("INSERT INTO Number (Value, IsPrime) VALUES ");
            foreach (var number in dataList)
            {
                valuesList.Add($"({number.Value}, {number.IsPrime})");
            }

            sqlBuilder.Append(string.Join(",", valuesList));

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = sqlBuilder.ToString();
            await command.ExecuteNonQueryAsync();

            await transaction.CommitAsync();

            _logger.LogInformation($"Saved {dataList.Count} numbers.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert data into Number table.");
            throw;
        }
    }

    /// <summary>
    /// Fetch N records from the SQLite Database where N is specified by the count parameter
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public async Task<IEnumerable<Number>> Get(int count)
    {
        try
        {
            _logger.LogInformation($"Getting {count} records from Number Table..");
            var result = new List<Number>();

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Value, IsPrime FROM Number LIMIT @count";
            command.Parameters.AddWithValue("@count", count);

            using var reader = await command.ExecuteReaderAsync();
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch data from Number table.");
            throw;
        }   
    }

    /// <summary>
    /// Fetch All the records from the SQLite Database
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<Number>> GetAll()
    {
        try
        {
            _logger.LogInformation("GetAll");
            var result = new List<Number>();

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Value, IsPrime FROM Number";

            using var reader = await command.ExecuteReaderAsync();
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
        catch
        {
            _logger.LogError("Failed to fetch all data from Number table.");
            throw;
        }
    }

    /// <summary>
    /// Empty the Number Database
    /// </summary>
    /// <returns></returns>
    public async Task DeleteAllAsync()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Number";

            await command.ExecuteNonQueryAsync();
            _logger.LogInformation("Deleted all records.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete all records from Number table.");
            throw;
        }
        
    }
}
