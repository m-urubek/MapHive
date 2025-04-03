using System;
using System.Data;
using System.Data.SQLite;
using MapHive.Singletons;

namespace MapHive.Utilities
{
    /// <summary>
    /// Adds missing tables and columns to the database
    /// </summary>
    public class DatabaseManipulator
    {
        /// <summary>
        /// Adds missing tables and columns to the database
        /// </summary>
        public void UpdateDatabase()
        {
            // Add Categories table if it doesn't exist
            if (!this.TableExists("Categories"))
            {
                this.CreateCategoriesTable();
            }
        }

        /// <summary>
        /// Creates the Categories table
        /// </summary>
        private void CreateCategoriesTable()
        {
            string query = @"
                CREATE TABLE Categories (
                    Id_Category INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    Icon TEXT
                );";

            _ = MainClient.SqlClient.Alter(query);
        }

        /// <summary>
        /// Checks if a table exists in the database
        /// </summary>
        private bool TableExists(string tableName)
        {
            string query = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';";
            DataTable result = MainClient.SqlClient.Select(query);
            return result.Rows.Count > 0;
        }
    }
} 