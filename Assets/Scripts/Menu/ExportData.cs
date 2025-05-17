using System;
using System.Data;
using System.IO;
using UnityEngine;
using TMPro;
using System.Linq;
using System.Text;
using SimpleFileBrowser;

public class ExportData : MonoBehaviour
{
    public TMP_Dropdown dropdown;

    public void ExportCSV()
    {
        string tableName = dropdown.options[dropdown.value].text;
        DataTable table = DatabaseManager.Instance.GetTableData(tableName);

        if (table != null && table.Rows.Count > 0)
        {
            FileBrowser.SetFilters(true, new FileBrowser.Filter("CSV Files", ".csv"));
            FileBrowser.SetDefaultFilter(".csv");
            FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");

            FileBrowser.ShowSaveDialog((path) =>
            {
                if (!string.IsNullOrEmpty(path[0]))
                {
                    string firstPath = path[0];
                    ExportDataToCSV(table, firstPath);
                }

                else
                {
                    Debug.Log("Export canceled.");
                }
            }, () => { Debug.Log("Export canceled."); }, FileBrowser.PickMode.Files);
        }
        else
        {
            Debug.LogWarning("No data to export.");
        }
    }

    private void ExportDataToCSV(DataTable table, string filePath)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                string headerLine = string.Join(",", table.Columns.Cast<DataColumn>().Select(column => column.ColumnName));
                writer.WriteLine(headerLine);

                foreach (DataRow row in table.Rows)
                {
                    string dataLine = string.Join(",", row.ItemArray.Select(value => value.ToString()));
                    writer.WriteLine(dataLine);
                }
            }

            Debug.Log($"Data exported to {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error exporting data: {e.Message}");
        }
    }
}
