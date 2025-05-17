using Npgsql;
using System.Data;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class DatabaseManager : MonoBehaviour
{
    private static DatabaseManager instance;
    private string connectionString = "Host=localhost;Username=posgres;Password=root;Database=BombersOnline2";
    private NpgsqlConnection connection;

    public static DatabaseManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<DatabaseManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = "DatabaseManager";
                    instance = obj.AddComponent<DatabaseManager>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        ConnectToDatabase();
        PopulateDropdown();
    }

    public void PopulateDropdown()
    {
        DropdownList dropdown = FindObjectOfType<DropdownList>();
        if (dropdown != null)
        {
            dropdown.PopulateDropdown();
        }
    }

    public void ConnectToDatabase()
    {
        connection = new NpgsqlConnection(connectionString);

        try
        {
            connection.Open();
            Debug.Log("Connected to PostgreSQL!");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to connect to PostgreSQL: " + e.Message);
        }
    }

    public NpgsqlConnection GetConnection()
    {
        return connection;
    }

    public bool RegisterUser(string username, string login, string password, string role)
    {
        if (IsUsernameOrLoginExists(username, login))
        {
            return false;
        }

        string query = "INSERT INTO \"users\" (username, login, password, role) VALUES (@username, @login, @password, @role)";
        using (var cmd = new NpgsqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("username", username);
            cmd.Parameters.AddWithValue("login", login);
            cmd.Parameters.AddWithValue("password", password);
            cmd.Parameters.AddWithValue("role", role);

            cmd.ExecuteNonQuery();
            return true;
        }
    }


    private bool IsUsernameOrLoginExists(string username, string login)
    {
        string query = "SELECT COUNT(*) FROM \"users\" WHERE username = @username OR login = @login";
        using (var cmd = new NpgsqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("username", username);
            cmd.Parameters.AddWithValue("login", login);

            int count = Convert.ToInt32(cmd.ExecuteScalar());
            return count > 0;
        }
    }

    public bool Login(string login, string password)
    {
        string query = "SELECT COUNT(*) FROM \"users\" WHERE login = @login AND password = @password";
        using (var cmd = new NpgsqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("login", login);
            cmd.Parameters.AddWithValue("password", password);

            int count = Convert.ToInt32(cmd.ExecuteScalar());

            return count > 0;
        }
    }

    public string GetUserRole(int userId)
    {
        string query = "SELECT role FROM \"users\" WHERE id = @userId";
        using (var cmd = new NpgsqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("userId", userId);

            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    return reader.GetString(0);
                }
            }
        }

        return null;
    }


    public DataTable GetTableData(string tableName)
    {
        DataTable dataTable = new DataTable();

        string formattedTableName = tableName.ToLower() == "users" ? "\"users\"" : tableName;

        string query = $"SELECT * FROM {formattedTableName}";

        using (var cmd = new NpgsqlCommand(query, connection))
        using (var reader = cmd.ExecuteReader())
        {
            dataTable.Load(reader);
        }

        return dataTable;
    }

    public List<string> GetTableNames()
    {
        List<string> tableNames = new List<string>();
        string query = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'";

        try
        {
            using (var cmd = new NpgsqlCommand(query, connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    tableNames.Add(reader.GetString(0));
                }
            }
        }
        catch (Exception e)
        {
            // Debug.LogError("Error getting table names: " + e.Message);
        }

        return tableNames;
    }

    public void ClearTable(string tableName)
    {
        string query = $"DELETE FROM {tableName}";
        using (var cmd = new NpgsqlCommand(query, connection))
        {
            cmd.ExecuteNonQuery();
        }
    }




    private string GetTableColumns(string tableName)
    {
        string query = $"SELECT column_name FROM information_schema.columns WHERE table_name = @tableName";
        List<string> columns = new List<string>();

        using (var cmd = new NpgsqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("tableName", tableName);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    columns.Add(reader.GetString(0));
                }
            }
        }

        return string.Join(", ", columns);
    }

    public int GetUserId(string login)
    {
        string query = "SELECT id FROM users WHERE login = @login";
        using (var cmd = new NpgsqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("login", login);
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    return reader.GetInt32(0);
                }
            }
        }
        return -1;
    }


    public void InsertGameRecord(int idMap, int idUser, int rounds, int winnerHero)
    {
        string query = "INSERT INTO games (idMap, idUser, rounds, winnerHero) VALUES (@idMap, @idUser, @rounds, @winnerHero)";
        using (var cmd = new NpgsqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("idMap", idMap);
            cmd.Parameters.AddWithValue("idUser", idUser);
            cmd.Parameters.AddWithValue("rounds", rounds);
            cmd.Parameters.AddWithValue("winnerHero", winnerHero);
            cmd.ExecuteNonQuery();
        }
    }


    public string GetUsername(int userId)
    {
        string username = null;
        string query = "SELECT username FROM users WHERE id = @userId";
        using (var cmd = new NpgsqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("userId", userId);
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    username = reader.GetString(0);
                }
            }
        }
        return username;
    }


    public void UpdatePlayerStats(int idUser, bool isWinner)
    {
        string queryCheckUser = "SELECT COUNT(*) FROM playerstats WHERE idUser = @idUser";
        using (var cmdCheckUser = new NpgsqlCommand(queryCheckUser, connection))
        {
            cmdCheckUser.Parameters.AddWithValue("idUser", idUser);
            int userCount = Convert.ToInt32(cmdCheckUser.ExecuteScalar());

            if (userCount == 0)
            {
                string queryAddStats = "INSERT INTO playerstats (idUser, gamesPlayed, gamesWon, gamesLosed) VALUES (@idUser, 1, @gamesWon, @gamesLosed)";
                using (var cmdAddStats = new NpgsqlCommand(queryAddStats, connection))
                {
                    cmdAddStats.Parameters.AddWithValue("idUser", idUser);
                    cmdAddStats.Parameters.AddWithValue("gamesWon", isWinner ? 1 : 0);
                    cmdAddStats.Parameters.AddWithValue("gamesLosed", isWinner ? 0 : 1);
                    cmdAddStats.ExecuteNonQuery();
                }
            }
            else
            {
                string queryUpdateStats = @"
                UPDATE playerstats
                SET gamesPlayed = gamesPlayed + 1,
                    gamesWon = gamesWon + @gamesWon,
                    gamesLosed = gamesLosed + @gamesLosed
                WHERE idUser = @idUser";
                using (var cmdUpdateStats = new NpgsqlCommand(queryUpdateStats, connection))
                {
                    cmdUpdateStats.Parameters.AddWithValue("idUser", idUser);
                    cmdUpdateStats.Parameters.AddWithValue("gamesWon", isWinner ? 1 : 0);
                    cmdUpdateStats.Parameters.AddWithValue("gamesLosed", isWinner ? 0 : 1);
                    cmdUpdateStats.ExecuteNonQuery();
                }
            }
        }
    }




    private void OnApplicationQuit()
    {
        if (connection != null && connection.State != ConnectionState.Closed)
        {
            connection.Close();
            Debug.Log("Connection to PostgreSQL closed.");
        }
    }
}
