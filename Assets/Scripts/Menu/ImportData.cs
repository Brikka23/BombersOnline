using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using Npgsql;
using System;
using SimpleFileBrowser;

public class ImportData : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    public void ImportCSV()
    {
        string tableName = dropdown.options[dropdown.value].text;

        FileBrowser.SetFilters(true, new FileBrowser.Filter("CSV Files", ".csv"));
        FileBrowser.SetDefaultFilter(".csv");
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");

        FileBrowser.ShowLoadDialog((paths) =>
        {
            string filePath = paths[0];

            if (!string.IsNullOrEmpty(filePath))
            {
                List<string[]> csvData = ReadCSVFile(filePath);

                if (csvData != null && csvData.Count > 0)
                {
                    InsertRecordsIntoTable(tableName, csvData);
                    Debug.Log($"Data imported from {filePath} to table {tableName}");
                }
            }
        }, () => { Debug.Log("Canceled"); }, FileBrowser.PickMode.Files);
    }

    private List<string[]> ReadCSVFile(string filePath)
    {
        List<string[]> data = new List<string[]>();

        try
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] values = line.Split(',');
                    data.Add(values);
                }
            }
        }
        catch (IOException e)
        {
            Debug.LogError($"Error reading CSV file: {e.Message}");
            return null;
        }

        return data;
    }

    public void InsertRecordsIntoTable(string tableName, List<string[]> records)
    {
        foreach (var record in records)
        {
            string username = record[0];
            string login = record[1];
            string password = record[2];
            string role = record[3];

            if (IsUserExists(username, login))
            {
                Debug.LogWarning($"User '{username}' or login '{login}' already exists. Skipping insertion.");
                continue;
            }

            string query = $"INSERT INTO {tableName} (username, login, password, role) VALUES (@username, @login, @password, @role)";
            using (var cmd = new NpgsqlCommand(query, DatabaseManager.Instance.GetConnection()))
            {
                cmd.Parameters.AddWithValue("username", username);
                cmd.Parameters.AddWithValue("login", login);
                cmd.Parameters.AddWithValue("password", password);
                cmd.Parameters.AddWithValue("role", role);

                cmd.ExecuteNonQuery();
            }
        }
    }

    private bool IsUserExists(string username, string login)
    {
        string query = "SELECT COUNT(*) FROM users WHERE username = @username OR login = @login";
        using (var cmd = new NpgsqlCommand(query, DatabaseManager.Instance.GetConnection()))
        {
            cmd.Parameters.AddWithValue("username", username);
            cmd.Parameters.AddWithValue("login", login);

            int count = Convert.ToInt32(cmd.ExecuteScalar());
            return count > 0;
        }
    }
}
