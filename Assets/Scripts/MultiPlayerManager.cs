using Dan.Main;
using System.Collections;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Transactions;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
using System.Net;

public class MultiPlayerManager : NetworkBehaviour
{
    public static MultiPlayerManager instance;

    [SerializeField] Image titleScreen;

    public Image fullScreen;
    public TextMeshProUGUI fullScreenText;
    public Image[] playerScreens;
    public TextMeshProUGUI[] playerScreenTexts;
    public Image divider;
    public GameObject leaderboard;

    private const int serverPort = 7777; // Default port
    [SerializeField] Button host;
    [SerializeField] Button client;
    [SerializeField] Button server;
    [SerializeField] TextMeshProUGUI transportText;
    [SerializeField] TextMeshProUGUI modeText;

    public NetworkVariable<int> playersWon = new NetworkVariable<int>(0);
    public int playerHealthIncrease;
    public int lastPlayerHealthDecrease;

    public float counter;
    private float[] deathCounters = { 0, 0 };
    public List<GameObject> players;

    public bool arenaActive;

    private void Start()
    {
        instance = this;
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            host.gameObject.SetActive(true);
            client.gameObject.SetActive(true);
            // Server button isn't used, so it is disabled
            //server.gameObject.SetActive(true);
        }
        NetworkManager.Singleton.OnClientConnectedCallback += HideWaiting;
        NetworkManager.Singleton.OnClientDisconnectCallback += PlayerLeft;
    }

    public void Host()
    {
        //if (!Application.isEditor)
        //{
        fullScreen.gameObject.SetActive(true);
        fullScreenText.text = "Waiting for Player 2...";
        //}
        NetworkManager.Singleton.StartHost();
        //if (!Application.isEditor)
        //{
        players[0].GetComponent<PlayerMovement>().enabled = false;
        players[0].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezePosition;
        //}
    }
    public void Client()
    {
        NetworkManager.Singleton.StartClient();
    }
    public void Server()
    {
        NetworkManager.Singleton.StartServer();
    }

    public void Hide()
    {
        titleScreen.gameObject.SetActive(false);

        var mode = NetworkManager.Singleton.IsHost ? "Host" : NetworkManager.Singleton.IsServer ? "Server" : "Client";

        transportText.text = "Transport: " + NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name;
        modeText.text = "Mode: " + mode;
    }

    private void HideWaiting(ulong clientID)
    {
        if (clientID == 1 && players[0])
        {
            fullScreen.gameObject.SetActive(false);
            divider.gameObject.SetActive(true);
            fullScreenText.text = "";
            players[0].GetComponent<PlayerMovement>().enabled = true;
            players[0].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
            if (NetworkManager.Singleton.IsServer)
            {
                players[0].GetComponent<PlayerScore>().time.Value = 0f;
                players[1].GetComponent<PlayerScore>().time.Value = 0f;
            }
        }
    }

    private void PlayerLeft(ulong clientID)
    {
        divider.gameObject.SetActive(false);
        fullScreen.gameObject.SetActive(true);
        fullScreenText.text = $"Player {clientID + 1} left!\nPlease restart the game!";
        if (players.Count > 0)
        {
            for (int i = 0; i < players.Count; i++)
            {
                foreach (BoxCollider2D boxCollider in players[(int)clientID].GetComponents<BoxCollider2D>())
                    boxCollider.enabled = false;
                players[i].GetComponent<PlayerMovement>().enabled = false;
                players[i].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezePosition;
            }
        }
    }

    [ClientRpc]
    public void PlayerDiedClientRpc(ulong clientID)
    {
        foreach (BoxCollider2D boxCollider in players[(int)clientID].GetComponents<BoxCollider2D>())
            boxCollider.enabled = false;
        players[(int)clientID].GetComponent<PlayerMovement>().enabled = false;
        players[(int)clientID].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezePosition;
        playerScreens[clientID].gameObject.SetActive(true);
        deathCounters[clientID] = 4;
    }

    // Logic that always happens when a player reaches the goal
    public void PlayerWonGlobal()
    {
        if (IsServer)
           playersWon.Value++;

        if (playersWon.Value == 1)
            playerHealthIncrease -= lastPlayerHealthDecrease;

        if (playersWon.Value == 2)
            PlayersWonClientRpc();
    }

    // Logic for individual players winning
    public void PlayerWon(ulong clientID, GameObject player)
    {
        if (players[(int)clientID].GetComponent<NetworkBehaviour>().IsOwner)
            players[(int)clientID].GetComponent<Health>().UpdateHealthServerRpc(playerHealthIncrease);
        players[(int)clientID].GetComponent<PlayerMovement>().enabled = false;
        foreach (BoxCollider2D boxCollider in players[(int)clientID].GetComponents<BoxCollider2D>())
            boxCollider.enabled = false;
        players[(int)clientID].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezePosition;
        playerScreens[clientID].gameObject.SetActive(true);
        playerScreenTexts[clientID].text = $"Player {clientID + 1} has won first!\nPlayer {clientID + 1} has been granted a larger Health bonus!\nWaiting on other Player...";
    }

    // Logic for when both players have won
    [ClientRpc]
    private void PlayersWonClientRpc()
    {
        for (int i = 0; i < players.Count; i++)
        {
            players[i].GetComponent<PlayerMovement>().enabled = false;
            foreach (BoxCollider2D boxCollider in players[i].GetComponents<BoxCollider2D>())
                boxCollider.enabled = false;
            players[i].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezePosition;
        }
        divider.gameObject.SetActive(false);
        playerScreens[0].gameObject.SetActive(false);
        playerScreens[1].gameObject.SetActive(false);
        fullScreen.gameObject.SetActive(true);
        fullScreenText.text = "Both players have reached the end!\nPress R to fire a Projectile!";
        players[0].transform.position = new Vector3(495, 1, 0);
        players[1].transform.position = new Vector3(505, 1, 0);
        counter = 8;
    }

    private void Update()
    {
        // Logic for when a player dies
        for (int i = 0; i < players.Count; i++)
        {
            if (deathCounters[i] > 0)
            {
                deathCounters[i] -= Time.deltaTime;
                playerScreenTexts[i].text = $"Player {i + 1} has died!\nRespawning in {(int)(deathCounters[i])}...";
            }
            if (deathCounters[i] < 0)
                RespawnPlayer(i);
        }

        // Logic for just before arena battle
        if (counter > 0)
            counter -= Time.deltaTime;
        if (counter <= 4 && counter > 0)
            fullScreenText.text = "Starting battle in " + (int)(counter) + "...\nPress R to fire a Projectile!";
        if (counter < 0)
        {
            fullScreen.gameObject.SetActive(false);
            for (int i = 0; i < players.Count; i++)
            {
                players[i].GetComponent<PlayerMovement>().enabled = true;
                foreach (BoxCollider2D boxCollider in players[i].GetComponents<BoxCollider2D>())
                    boxCollider.enabled = true;
                players[i].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
                players[0].GetComponent<PlayerMovement>().camera1.enabled = false;
                players[1].GetComponent<PlayerMovement>().camera2.enabled = false;
            }
            arenaActive = true;
            counter = 0;
        }
    }

    // Logic for when a player dies during the race/not in arena
    private void RespawnPlayer(int playedWhoDied)
    {
        players[playedWhoDied].transform.position = playedWhoDied == 0 ? new Vector3(0f, 0f, 0f) : new Vector3(0f, -250f, 0f);
        if (players[playedWhoDied].GetComponent<NetworkBehaviour>().IsServer)
            players[playedWhoDied].GetComponent<Health>().health.Value = 3;
        players[playedWhoDied].GetComponent<Health>().HealthModify(0);
        players[playedWhoDied].GetComponent<PlayerMovement>().enabled = true;
        foreach (BoxCollider2D boxCollider in players[playedWhoDied].GetComponents<BoxCollider2D>())
            boxCollider.enabled = true;
        players[playedWhoDied].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
        playerScreens[playedWhoDied].gameObject.SetActive(false);

        deathCounters[playedWhoDied] = 0;
    }

    // Logic for when a player wins in the arena (other player dies)
    [ClientRpc]
    public void PlayerWonArenaClientRpc(ulong clientID)
    {
        for (int i = 0; i < players.Count; i++)
        {
            players[i].GetComponent<PlayerMovement>().enabled = false;
            foreach (BoxCollider2D boxCollider in players[(int)clientID].GetComponents<BoxCollider2D>())
                boxCollider.enabled = false;
            players[i].GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezePosition;
            playerScreens[i].gameObject.SetActive(false);
        }

        fullScreen.gameObject.SetActive(true);
        if (clientID == 0)
            clientID = 1;
        else if (clientID == 1)
            clientID = 0;
        fullScreenText.text = $"Player {clientID + 1} has won!";
        GetComponent<Leaderboard>().inputTime = players[(int)clientID].GetComponent<PlayerScore>().time.Value;
        GetComponent<Leaderboard>().inputScore = players[(int)clientID].GetComponent<PlayerScore>().score.Value;

        if (NetworkManager.Singleton.LocalClientId == clientID)
            leaderboard.SetActive(true);
        else
            leaderboard.SetActive(false);
    }
}

