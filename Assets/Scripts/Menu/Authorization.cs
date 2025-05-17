using TMPro;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;
using UnityEngine.Networking;

public class Authorization : MonoBehaviour
{
    [SerializeField] private TMP_InputField loginInput;
    [SerializeField] private TMP_InputField passwordInput;

    [SerializeField] private GameObject authMenuScene;
    [SerializeField] private GameObject mainMenuScene;
    [SerializeField] private GameObject adminMenuScene;
    [SerializeField] private GameObject ErrorText;

    public static int CurrentUserId { get; private set; }
    public static string CurrentUserRole { get; private set; }
    public static bool IsGuest { get; private set; } = false;

    private void Start()
    {
        DatabaseManager.Instance.ConnectToDatabase();

        if (PlayerPrefs.HasKey("CurrentUserId"))
        {
            CurrentUserId = PlayerPrefs.GetInt("CurrentUserId");
            string role = DatabaseManager.Instance.GetUserRole(CurrentUserId);
            switch (role)
            {
                case "Игрок":
                    authMenuScene.SetActive(false);
                    mainMenuScene.SetActive(true);
                    break;

                case "Администратор":
                    authMenuScene.SetActive(false);
                    adminMenuScene.SetActive(true);
                    break;
            }
        }
    }

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

    public void OnLoginButtonClicked()
    {
        string login = loginInput.text;
        string password = passwordInput.text;

        string hashedPassword = HashPassword(password);

        if (DatabaseManager.Instance.Login(login, hashedPassword))
        {
            CurrentUserId = DatabaseManager.Instance.GetUserId(login);

            PlayerPrefs.SetInt("CurrentUserId", CurrentUserId);
            PlayerPrefs.Save();

            string role = DatabaseManager.Instance.GetUserRole(CurrentUserId);

            switch (role)
            {
                case "Игрок":
                    authMenuScene.SetActive(false);
                    mainMenuScene.SetActive(true);
                    break;

                case "Администратор":
                    authMenuScene.SetActive(false);
                    adminMenuScene.SetActive(true);
                    break;
            }
        }
        else
        {
            ErrorText.SetActive(true);
            Debug.LogWarning("Login failed. Invalid username or password.");
        }
    }

    public void OnGuestButtonClicked()
    {
        IsGuest = true;
        CurrentUserId = -1;
        CurrentUserRole = "Гость";
        authMenuScene.SetActive(false);
        mainMenuScene.SetActive(true);
    }
}
