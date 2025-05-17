using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class OfflineGameManager : MonoBehaviour
{
    [Header("Prefabs & Spawn Points")]
    public GameObject playerPrefab;
    public GameObject botPrefab;
    public Transform[] spawnPoints;

    [Header("UI")]
    public TMP_Text playerScoreLabel;
    public TMP_Text botScoreLabel;
    public GameObject endGamePanel;
    public TMP_Text endGameText;
    public float endDelay = 1f;

    private static bool s_initialized = false;
    private static int s_totalRounds = 1;
    private static int s_roundsPlayed = 0;
    private static int s_playerScore = 0;
    private static int s_botScore = 0;

    private GameObject player;
    private List<GameObject> bots = new List<GameObject>();
    private bool gameEnding = false;

    void Start()
    {
        if (!s_initialized)
        {
            s_totalRounds = PlayerPrefs.GetInt("OfflineRounds", 1);
            s_roundsPlayed = 0;
            s_playerScore = 0;
            s_botScore = 0;
            s_initialized = true;
        }

        UpdateScoreLabels();
        SpawnAll();
        endGamePanel.SetActive(false);
        gameEnding = false;
    }

    void Update()
    {
        if (gameEnding) return;

        bool playerAlive = player != null && player.activeSelf;
        bool anyBotAlive = bots.Exists(b => b != null && b.activeSelf);

        if (!playerAlive || !anyBotAlive)
        {
            gameEnding = true;

            bool playerWon = playerAlive;
            if (playerWon) s_playerScore++;
            else s_botScore++;

            s_roundsPlayed++;
            UpdateScoreLabels();

            StartCoroutine(NextRoundRoutine(playerWon));
        }
    }

    private IEnumerator NextRoundRoutine(bool playerWon)
    {
        yield return new WaitForSeconds(endDelay);

        if (s_roundsPlayed < s_totalRounds)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            endGamePanel.SetActive(true);
            endGameText.text = playerWon
                ? "Вы выиграли матч!"
                : "Вы проиграли матч!";
            s_initialized = false;
        }
    }

    private void SpawnAll()
    {
        bots.Clear();

        player = Instantiate(playerPrefab, spawnPoints[0].position, Quaternion.identity);
        player.tag = "Player";
        var pm = player.GetComponent<MovementController>();
        var bc = player.GetComponent<BombController>();
        if (pm != null) pm.isOfflineMode = true;
        if (bc != null) bc.isOfflineMode = true;

        int botCount = PlayerPrefs.GetInt("OfflineBotCount", 1);
        int diffIdx = PlayerPrefs.GetInt("OfflineBotDifficulty", 0);

        for (int i = 0; i < botCount; i++)
        {
            int idx = Mathf.Clamp(i + 1, 1, spawnPoints.Length - 1);
            var bot = Instantiate(botPrefab, spawnPoints[idx].position, Quaternion.identity);
            bot.name = $"Bot_{i + 1}";
            var botCtrl = bot.GetComponent<BotController>();
            if (botCtrl != null) botCtrl.difficultyIndex = diffIdx;
            pm = bot.GetComponent<MovementController>();
            bc = bot.GetComponent<BombController>();
            if (pm != null) pm.isOfflineMode = true;
            if (bc != null) bc.isOfflineMode = true;

            bots.Add(bot);
        }
    }

    private void UpdateScoreLabels()
    {
        playerScoreLabel.text = s_playerScore.ToString();
        botScoreLabel.text = s_botScore.ToString();
    }

    public void LoadMenu()
    {
        s_initialized = false;
        SceneManager.LoadScene("Menu");
    }
}
