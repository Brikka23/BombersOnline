using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    [Header("Spawn & UI")]
    public GameObject[] playerPrefabs;
    public Transform[] spawnPoints;
    public TMP_Text[] playerScoreLabels;
    public GameObject endGamePanel;
    public TMP_Text endGameText;

    [Header("Settings")]
    public int defaultRounds = 1;

    int totalRounds;
    int roundsPlayed;
    int[] playerScores;
    List<Player> playersInGame = new List<Player>();
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            PhotonNetwork.AutomaticallySyncScene = true;
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        RefreshPlayers();
        var room = PhotonNetwork.CurrentRoom;
        totalRounds = room.CustomProperties.TryGetValue("rounds", out object r) ? (int)r : defaultRounds;

        InitializeScores();
        UpdateScoreUI();
        StartNewRound();
    }

    void RefreshPlayers()
    {
        playersInGame = new List<Player>(PhotonNetwork.PlayerList);
        playersInGame.Sort((a, b) => a.ActorNumber.CompareTo(b.ActorNumber));
    }

    void InitializeScores()
    {
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        bool needInit = true;
        if (props.TryGetValue("playerScores", out object psObj) && psObj is int[] arr && arr.Length == playersInGame.Count)
        {
            playerScores = arr;
            needInit = false;
        }
        else
        {
            playerScores = new int[playersInGame.Count];
        }
        roundsPlayed = props.TryGetValue("roundsPlayed", out object rp) ? (int)rp : 0;

        if (needInit)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable {
                { "playerScores", playerScores },
                { "roundsPlayed", roundsPlayed }
            });
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propsThatChanged)
    {
        if (propsThatChanged.ContainsKey("playerScores"))
        {
            playerScores = (int[])PhotonNetwork.CurrentRoom.CustomProperties["playerScores"];
            UpdateScoreUI();
        }
        if (propsThatChanged.ContainsKey("roundsPlayed"))
        {
            roundsPlayed = (int)PhotonNetwork.CurrentRoom.CustomProperties["roundsPlayed"];
        }
    }

    void UpdateScoreUI()
    {
        for (int i = 0; i < playerScoreLabels.Length; i++)
        {
            bool show = i < playerScores.Length;
            playerScoreLabels[i].gameObject.SetActive(show);
            if (show)
                playerScoreLabels[i].text = playerScores[i].ToString();
        }
    }

    void StartNewRound()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            foreach (var pl in playersInGame)
                pl.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "IsAlive", true } });
        }
        SpawnLocalPlayer();
    }

    void SpawnLocalPlayer()
    {
        int idx = playersInGame.IndexOf(PhotonNetwork.LocalPlayer);
        if (idx < 0 || idx >= spawnPoints.Length)
        {
            return;
        }
        if (idx < 0 || idx >= playerPrefabs.Length)
        {
            return;
        }
        var prefab = playerPrefabs[idx];

        var go = PhotonNetwork.Instantiate(prefab.name,
                                           spawnPoints[idx].position,
                                           Quaternion.identity);
        go.name = $"Player_{PhotonNetwork.LocalPlayer.ActorNumber}";
    }

    [PunRPC]
    public void NotifyPlayerDeath(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        var pl = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        pl?.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { "IsAlive", false } });

        StartCoroutine(DelayedRoundCheck());
    }

    public void ExitToMenu()
    {
        SceneManager.LoadScene("Menu");
    }

    IEnumerator DelayedRoundCheck()
    {
        yield return new WaitForSeconds(0.1f);

        int alive = 0, lastIdx = -1;
        for (int i = 0; i < playersInGame.Count; i++)
        {
            if (playersInGame[i].CustomProperties.TryGetValue("IsAlive", out object a) && (bool)a)
            {
                alive++;
                lastIdx = i;
            }
        }

        if (alive <= 1)
        {
            int winnerActor = -1;
            if (lastIdx >= 0)
            {
                playerScores[lastIdx]++;
                winnerActor = playersInGame[lastIdx].ActorNumber;
            }
            roundsPlayed++;

            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable {
                { "playerScores", playerScores },
                { "roundsPlayed", roundsPlayed }
            });

            if (roundsPlayed < totalRounds)
                photonView.RPC(nameof(RPC_Reload), RpcTarget.AllBuffered);
            else
                photonView.RPC(nameof(RPC_EndGame), RpcTarget.AllBuffered, winnerActor);
        }
    }

    [PunRPC]
    void RPC_Reload()
    {
        PhotonNetwork.LoadLevel(SceneManager.GetActiveScene().name);
    }

    [PunRPC]
    void RPC_EndGame(int winnerActorNumber)
    {
        endGamePanel.SetActive(true);
        var sb = new StringBuilder();
        for (int i = 0; i < playerScores.Length; i++)
        {
            sb.AppendLine($"Игрок{i + 1}: {playerScores[i]}");
        }

        endGameText.text = sb.ToString();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RefreshPlayers();
        UpdateScoreUI();

        if (playersInGame.Count <= 1)
        {
            int winner = playersInGame.Count == 1 ? playersInGame[0].ActorNumber : -1;
            photonView.RPC(nameof(RPC_EndGame), RpcTarget.AllBuffered, winner);
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("Menu");
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        photonView.RPC(nameof(RPC_EndGame), RpcTarget.AllBuffered, -1);
    }
}
