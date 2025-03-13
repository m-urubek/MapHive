﻿using System.Data;
using System.Data.SQLite;

public class SqlClient
{
    private readonly string _connectionString;

    public SqlClient(string databaseFilePath)
    {
        if (string.IsNullOrEmpty(databaseFilePath))
        {
            throw new ArgumentException("Database file path cannot be null or empty.", nameof(databaseFilePath));
        }

        this._connectionString = $"Data Source={databaseFilePath};Version=3;";
    }

    /// <summary>
    /// Executes an INSERT query and returns the autogenerated ID of the inserted row.
    /// </summary>
    /// <param name="query">The SQL query string to execute (INSERT).</param>
    /// <param name="parameters">An array of SQLite parameters to use in the query.</param>
    /// <returns>The autogenerated ID of the inserted row.</returns>
    public int Insert(string query, SQLiteParameter[]? parameters = null)
    {
        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));
        }

        using SQLiteConnection connection = new(this._connectionString);
        connection.Open();
        using SQLiteCommand command = new(query, connection);
        if (parameters != null)
        {
            command.Parameters.AddRange(parameters);
        }

        _ = command.ExecuteNonQuery();

        // Use the connection to get the last inserted row ID
        return Convert.ToInt32(connection.LastInsertRowId);
    }

    /// <summary>
    /// Executes an UPDATE query and returns the number of rows affected.
    /// </summary>
    /// <param name="query">The SQL query string to execute (UPDATE).</param>
    /// <param name="parameters">An array of SQLite parameters to use in the query.</param>
    /// <returns>The number of rows affected by the query.</returns>
    public int Update(string query, SQLiteParameter[]? parameters = null)
    {
        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));
        }

        using SQLiteConnection connection = new(this._connectionString);
        connection.Open();
        using SQLiteCommand command = new(query, connection);
        if (parameters != null)
        {
            command.Parameters.AddRange(parameters);
        }

        return command.ExecuteNonQuery();  // Return the number of affected rows
    }

    /// <summary>
    /// Executes a SELECT query and returns the result as a DataTable.
    /// </summary>
    /// <param name="query">The SQL query string to execute (SELECT).</param>
    /// <param name="parameters">An array of SQLite parameters to use in the query.</param>
    /// <returns>A DataTable containing the query results.</returns>
    public DataTable Select(string query, SQLiteParameter[]? parameters = null)
    {
        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));
        }

        using SQLiteConnection connection = new(this._connectionString);
        connection.Open();
        using SQLiteCommand command = new(query, connection);
        if (parameters != null)
        {
            command.Parameters.AddRange(parameters);
        }

        using SQLiteDataAdapter adapter = new(command);
        DataTable dataTable = new();
        _ = adapter.Fill(dataTable);
        return dataTable;  // Return the result set as DataTable
    }

    /// <summary>
    /// Executes a DELETE query and returns the number of rows affected.
    /// </summary>
    /// <param name="query">The SQL query string to execute (DELETE).</param>
    /// <param name="parameters">An array of SQLite parameters to use in the query.</param>
    /// <returns>The number of rows affected by the query.</returns>
    public int Delete(string query, SQLiteParameter[]? parameters = null)
    {
        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));
        }

        using SQLiteConnection connection = new(this._connectionString);
        connection.Open();
        using SQLiteCommand command = new(query, connection);
        if (parameters != null)
        {
            command.Parameters.AddRange(parameters);
        }

        return command.ExecuteNonQuery();  // Return the number of rows affected
    }

    /// <summary>
    /// Executes an ALTER query and returns the number of rows affected.
    /// </summary>
    /// <param name="query">The SQL query string to execute (ALTER).</param>
    /// <param name="parameters">An array of SQLite parameters to use in the query.</param>
    /// <returns>The number of rows affected by the query.</returns>
    public int Alter(string query, SQLiteParameter[]? parameters = null)
    {
        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));
        }

        using SQLiteConnection connection = new(this._connectionString);
        connection.Open();
        using SQLiteCommand command = new(query, connection);
        if (parameters != null)
        {
            command.Parameters.AddRange(parameters);
        }

        return command.ExecuteNonQuery();  // Return the number of rows affected
    }
}
