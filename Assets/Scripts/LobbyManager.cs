using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public static LobbyManager Instance;

    [Header("Menu Panels")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject lobbyPanel;

    [Header("UI Elements")]
    [SerializeField] private TMP_InputField roomCodeInputField;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private TMP_Dropdown maxPlayersDropdown;
    [SerializeField] private TMP_Dropdown roundsDropdown;
    [SerializeField] private TMP_Dropdown roundsOfflineDropdown;
    [SerializeField] private TMP_Dropdown botCountDropdown;
    [SerializeField] private TMP_Dropdown difficultyDropdown;
    [SerializeField] private Toggle isPrivateToggle;

    private const int MinPlayersToStart = 2;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public void OnStartOfflineGame()
    {
        int rounds = roundsOfflineDropdown.value + 1;
        int botCount = botCountDropdown.value + 1;
        PlayerPrefs.SetInt("OfflineRounds", rounds);
        PlayerPrefs.SetInt("OfflineBotCount", botCount);
        PlayerPrefs.SetInt("OfflineBotDifficulty", difficultyDropdown.value);
        PlayerPrefs.Save();

        SceneManager.LoadScene("OfflineGame");
    }

    public void CreateRoom()
    {
        string roomCode = Random.Range(1000, 9999).ToString();

        if (!byte.TryParse(maxPlayersDropdown.options[maxPlayersDropdown.value].text, out byte maxPlayers))
        {
            Debug.LogError("Invalid max players value.");
            return;
        }

        int rounds = roundsDropdown.value + 1;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayers,
            IsVisible = !isPrivateToggle.isOn,
            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable
            {
                { "roomCode", roomCode },
                { "rounds",    rounds }
            },
            CustomRoomPropertiesForLobby = new[] { "roomCode", "rounds" }
        };

        PhotonNetwork.CreateRoom(roomCode, options);
    }

    public override void OnCreatedRoom()
    {
        ShowLobbyPanel();
    }

    public void JoinRoomByCode()
    {
        string code = roomCodeInputField.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            errorText.text = "Впишите код комнаты";
            return;
        }
        PhotonNetwork.JoinRoom(code);
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        ShowLobbyPanel();
    }

    public void JoinRandomRoom()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        errorText.text = "Нет свободных комнат";
        Debug.LogWarning("No random rooms available.");
    }
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        PhotonNetwork.LeaveRoom();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        errorText.text = "Ошибка подключения к комнате";
        Debug.LogError($"Failed to join room: {message}");
    }

    public override void OnLeftRoom()
    {
        ShowMenuPanel();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        ShowMenuPanel();
    }

    private void ShowLobbyPanel()
    {
        menuPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    private void ShowMenuPanel()
    {
        lobbyPanel.SetActive(false);
        menuPanel.SetActive(true);
    }
}
