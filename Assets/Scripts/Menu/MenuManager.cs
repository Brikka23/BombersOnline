using Npgsql;
using System.Runtime.Remoting.Messaging;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [SerializeField] AudioSource menuMusic;
    [SerializeField] TMP_Dropdown playersList;
    [SerializeField] TMP_Dropdown roundsList;
    [SerializeField] Slider sliderOfMusic;

    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text gamesPlayedText;
    [SerializeField] private TMP_Text gamesWonText;
    [SerializeField] private TMP_Text gamesLostText;


    private void Start()
    {
        LoadGameSettings();
    }

    public void ShowPlayerStats()
    {
        int userId = Authorization.CurrentUserId;
        if (userId != -1)
        {
            DatabaseManager databaseManager = DatabaseManager.Instance;
            if (databaseManager != null)
            {
                string username = databaseManager.GetUsername(userId);
                if (username != null)
                {
                    string query = "SELECT money, gamesplayed, gameswon, gameslosed FROM playerstats WHERE idUser = @userId";
                    using (var cmd = new NpgsqlCommand(query, databaseManager.GetConnection()))
                    {
                        cmd.Parameters.AddWithValue("userId", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int coins = reader.GetInt32(0);
                                int gamesPlayed = reader.GetInt32(1);
                                int gamesWon = reader.GetInt32(2);
                                int gamesLost = reader.GetInt32(3);

                                usernameText.text = username;
                                coinsText.text = "Монет: " + coins;
                                gamesPlayedText.text = "Сыграно игр: " + gamesPlayed;
                                gamesWonText.text = "Выигрышей: " + gamesWon;
                                gamesLostText.text = "Проигрышей: " + gamesLost;
                            }
                            else
                            {
                                Debug.LogWarning("No player stats found for user ID: " + userId);
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Username not found for user ID: " + userId);
                }
            }
            else
            {
                Debug.LogError("Failed to show player stats: DatabaseManager instance is null.");
            }
        }
        else
        {
            Debug.LogWarning("Cannot show player stats: Current user ID is invalid (-1).");
        }
    }




    public void QuitApp()
    {
        Application.Quit();
    }

    public void StartGame()
    {
        int numPlayers = playersList.value + 2;
        int numRounds = roundsList.value + 1;

        PlayerPrefs.SetInt("NumPlayers", numPlayers);
        PlayerPrefs.SetInt("NumRounds", numRounds);
        PlayerPrefs.Save();

        SceneManager.LoadScene("Game");
    }

    public void LoadGameSettings()
    {
        DatabaseManager databaseManager = DatabaseManager.Instance;
        if (databaseManager != null)
        {
            string query = "SELECT musicVolume, rounds, players FROM settings WHERE idsetting = '1'";
            using (var cmd = new NpgsqlCommand(query, databaseManager.GetConnection()))
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    menuMusic.volume = reader.GetFloat(0);
                    sliderOfMusic.value = reader.GetFloat(0);
                    roundsList.value = reader.GetInt32(1)-1;
                    playersList.value = reader.GetInt32(2)-2;
                    menuMusic.enabled = true;
                }
            }
        }
        else
        {
            Debug.LogError("Failed to load game settings: DatabaseManager instance is null.");
        }
    }



    public void SaveGameSettings()
    {
        DatabaseManager databaseManager = DatabaseManager.Instance;
        if (databaseManager != null)
        {
            string query = "UPDATE settings SET musicVolume = @musicVolume, rounds = @rounds, players = @players WHERE idsetting = '1'";
            using (var cmd = new NpgsqlCommand(query, databaseManager.GetConnection()))
            {
                cmd.Parameters.AddWithValue("musicVolume", sliderOfMusic.value);
                cmd.Parameters.AddWithValue("rounds", roundsList.value + 1);
                cmd.Parameters.AddWithValue("players", playersList.value + 2);
                cmd.ExecuteNonQuery();
            }
        }
        else
        {
            Debug.LogError("Failed to save game settings: DatabaseManager instance is null.");
        }
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.DeleteAll();
    }
}
