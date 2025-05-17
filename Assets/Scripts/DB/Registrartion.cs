using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Security.Cryptography;
using System.Text;

public class Registration : MonoBehaviour
{
    [SerializeField] TMP_InputField usernameInput;
    [SerializeField] TMP_InputField loginInput;
    [SerializeField] TMP_InputField passwordInput;
    [SerializeField] GameObject errorText;
    [SerializeField] GameObject sucText;

    private string HashPassword(string password)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }

    public void OnRegisterButtonClicked()
    {
        string username = usernameInput.text;
        string login = loginInput.text;
        string password = passwordInput.text;

        string hashedPassword = HashPassword(password);

        if (DatabaseManager.Instance.RegisterUser(username, login, hashedPassword, "Игрок"))
        {
            errorText.SetActive(false);
            sucText.SetActive(true);
            Debug.Log("Регистрация прошла успешно!");
        }
        else
        {
            sucText.SetActive(false);
            errorText.SetActive(true);
            Debug.LogWarning("Ошибка регистрации");
        }
    }

    public void OnRegisterAdminButtonClicked()
    {
        string username = usernameInput.text;
        string login = loginInput.text;
        string password = passwordInput.text;

        string hashedPassword = HashPassword(password);

        if (DatabaseManager.Instance.RegisterUser(username, login, hashedPassword, "Администратор"))
        {
            errorText.SetActive(false);
            sucText.SetActive(true);
            Debug.Log("Регистрация прошла успешно!");
        }
        else
        {
            sucText.SetActive(false);
            errorText.SetActive(true);
            Debug.LogWarning("Ошибка регистрации");
        }
    }
}
