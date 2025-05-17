using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class LobbyUiManager : MonoBehaviourPunCallbacks
{
    [Header("Lobby UI")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject startGameButton;
    [SerializeField] private TextMeshProUGUI roomCodeText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private TextMeshProUGUI roundSettingText;
    [SerializeField] private TextMeshProUGUI connectedPlayersText;

    private const int MinPlayersToStart = 2;

    public override void OnJoinedRoom()
    {
        lobbyPanel.SetActive(true);
        UpdateLobbyUI();
    }

    private void UpdateLobbyUI()
    {
        if (PhotonNetwork.CurrentRoom == null) return;

        if (roomCodeText != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("roomCode", out object code))
        {
            roomCodeText.text = code.ToString();
        }

        if (roundSettingText != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("rounds", out object rounds))
        {
            roundSettingText.text = $"Раунды: {rounds}";
        }

        UpdatePlayerCount();
    }

    private void UpdatePlayerCount()
    {
        int count = PhotonNetwork.CurrentRoom.PlayerCount;
        int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;
        playerCountText.text = $"Игроки: {count}/{maxPlayers}";

        connectedPlayersText.text = "";
        foreach (var p in PhotonNetwork.PlayerList)
            connectedPlayersText.text += $"{p.NickName}\n";

        startGameButton.SetActive(
            PhotonNetwork.IsMasterClient && count >= MinPlayersToStart
        );
    }

    public void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;
        PhotonNetwork.LoadLevel("Game");
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("Menu");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateLobbyUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateLobbyUI();
        if (PhotonNetwork.CurrentRoom.PlayerCount == 0)
            PhotonNetwork.LeaveRoom();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        SceneManager.LoadScene("Menu");
    }
}
