using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Npgsql;
using System.Data;

public class DropdownList : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    public TMP_Text scrollViewText;
    public ScrollRect scrollRect;
    public TMP_InputField searchInputField;

    private string searchValue = "";

    void Start()
    {
        PopulateDropdown();
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        searchInputField.onValueChanged.AddListener(UpdateSearchValue);
    }

    public void PopulateDropdown()
    {
        dropdown.ClearOptions();

        var tableNames = DatabaseManager.Instance.GetTableNames();

        dropdown.AddOptions(tableNames);
    }

    private void OnDropdownValueChanged(int index)
    {
        ShowTableData(dropdown.options[index].text);
    }

    private void UpdateSearchValue(string newValue)
    {
        searchValue = newValue;
        ShowTableData(dropdown.options[dropdown.value].text);
    }

    private void ShowTableData(string tableName)
    {
        var tableData = DatabaseManager.Instance.GetTableData(tableName);

        scrollViewText.text = "";

        foreach (DataRow row in tableData.Rows)
        {
            bool rowMatchesSearch = false;

            string rowData = "";
            foreach (var item in row.ItemArray)
            {
                rowData += item.ToString() + " ";

                if (item.ToString().Contains(searchValue, StringComparison.OrdinalIgnoreCase))
                {
                    rowMatchesSearch = true;
                }
            }

            if (rowMatchesSearch || string.IsNullOrEmpty(searchValue))
            {
                scrollViewText.text += rowData + "\n";
            }
        }
    }
}
