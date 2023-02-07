using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public List<PlayerInfo> allPlayers = new List<PlayerInfo>();
    private int index;
    private List<LeaderboardPlayer> lboardPlayers = new List<LeaderboardPlayer>();

    public enum EventCodes : byte
    {
        NewPlayer,
        ListPlayer,
        UpdateStat,
        NextMatch,
        TimeSync
    }

    public enum GameState
    {
        Waiting,
        Playing,
        Ending
    }

    [SerializeField]
    private int killsToWin = 3;
    [SerializeField]
    private Transform mapCamPoint;
    private GameState state = GameState.Waiting;
    [SerializeField]
    private float waitAfterEnding = 5f;
    [SerializeField]
    private bool perpetual;

    [SerializeField]
    private float matchLength = 180f;
    private float currentMatchTime;

    private float sendTimer;

    private static MatchManager _instance;
    public static MatchManager Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("MatchManager is Null");
            }
            return _instance;
        }
    }

    private void Awake()
    {
        _instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene("Main Menu");
        }
        else
        {
            NewPlayerSend(PhotonNetwork.NickName);
            state = GameState.Playing;

            SetupTimer();

            if (!PhotonNetwork.IsMasterClient)
            {
                UIController.Instance.SetTimer(false);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && state != GameState.Ending)
        {
            if (UIController.Instance.GetLeaderboard().activeInHierarchy)
            {
                UIController.Instance.GetLeaderboard().SetActive(false);
            }
            else
            {
                ShowLeaderboard();
            }
        }

        if(PhotonNetwork.IsMasterClient && currentMatchTime >= 0f && state == GameState.Playing)
        {
            currentMatchTime -= Time.deltaTime;

            if(currentMatchTime <= 0f)
            {
                currentMatchTime = 0f;

                state = GameState.Ending;

                ListPlayerSend();
                
                //StateCheck();
            }

            UpdateTimerDisplay();

            sendTimer -= Time.deltaTime;

            if (sendTimer <= 0)
            {
                sendTimer += 1f;

                TimerSend();
            }

        }
    }

    public GameState GetCurrentGameState()
    {
        return state;
    }

    public Transform GetCamPosition()
    {
        return mapCamPoint;
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code < 200)
        {
            EventCodes theEvent = (EventCodes)photonEvent.Code;
            object[] data = (object[])photonEvent.CustomData;

            //Debug.Log("Received Event " + theEvent);

            switch (theEvent)
            {
                case EventCodes.NewPlayer:
                    NewPlayerReceive(data);
                    break;

                case EventCodes.ListPlayer:
                    ListPlayerReceive(data);
                    break;

                case EventCodes.UpdateStat:
                    UpdateStatsReceive(data);
                    break;

                case EventCodes.NextMatch:
                    NextMatchReceive();
                    break;

                case EventCodes.TimeSync:
                    TimerReceiver(data);
                    break;
            }
        }
    }

    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void NewPlayerSend(string username)
    {
        object[] package = new object[4];
        package[0] = username;
        package[1] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[2] = 0;
        package[3] = 0;

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewPlayer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true });
    }

    public void NewPlayerReceive(object[] dataReceived)
    {
        PlayerInfo player = new PlayerInfo((string)dataReceived[0], (int)dataReceived[1], (int)dataReceived[2], (int)dataReceived[3]);
        allPlayers.Add(player);

        ListPlayerSend();
    }

    public void ListPlayerSend()
    {
        object[] package = new object[allPlayers.Count + 1];

        package[0] = state;

        for (int i = 0; i < allPlayers.Count; i++)
        {
            object[] piece = new object[4];

            piece[0] = allPlayers[i].name;
            piece[1] = allPlayers[i].actor;
            piece[2] = allPlayers[i].kills;
            piece[3] = allPlayers[i].deaths;

            package[i + 1] = piece;
        }

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.ListPlayer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true });
    }

    public void ListPlayerReceive(object[] dataReceived)
    {
        allPlayers.Clear();

        state = (GameState)dataReceived[0];

        for (int i = 1; i < dataReceived.Length; i++)
        {
            object[] piece = (object[])dataReceived[i];

            PlayerInfo player = new PlayerInfo((string)piece[0], (int)piece[1], (int)piece[2], (int)piece[3]);

            allPlayers.Add(player);

            //set Ourselves in the list
            if (PhotonNetwork.LocalPlayer.ActorNumber == player.actor)
            {
                index = i - 1;
            }
        }

        StateCheck();
    }

    public void UpdateStatsSend(int actorSending, int statToUpdate, int amountToChange)
    {
        object[] package = new object[] { actorSending, statToUpdate, amountToChange };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.UpdateStat,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true });
    }

    public void UpdateStatsReceive(object[] dataReceived)
    {
        int actor = (int)dataReceived[0];
        int statType = (int)dataReceived[1];
        int amount = (int)dataReceived[2];

        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (allPlayers[i].actor == actor)
            {
                switch (statType)
                {
                    case 0: //Kills
                        allPlayers[i].kills += amount;
                        //Debug.Log("Player " + allPlayers[i].name + " kills " + allPlayers[i].kills);
                        break;
                    case 1: //Deaths
                        allPlayers[i].deaths += amount;
                        //Debug.Log("Player " + allPlayers[i].name + " deaths " + allPlayers[i].deaths);
                        break;
                }
                if (i == index)
                {
                    UpdateStatsDisplay();
                }

                if (UIController.Instance.GetLeaderboard().activeInHierarchy)
                {
                    ShowLeaderboard();
                }

                break;
            }
        }

        ScoreCheck();
    }

    public void TimerSend()
    {
        object[] package = new object[] { (int)currentMatchTime, state };
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.TimeSync,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true });
    }

    public void TimerReceiver(object[] dataReceived)
    {
        currentMatchTime = (int)dataReceived[0];
        state = (GameState)dataReceived[1];

        UpdateTimerDisplay();

        UIController.Instance.SetTimer(true);
    }

    public void UpdateStatsDisplay()
    {
        if (allPlayers.Count > index)
        {
            UIController.Instance.SetKillText(allPlayers[index].kills);
            UIController.Instance.SetDeathText(allPlayers[index].deaths);
        }
        else
        {
            UIController.Instance.SetKillText(0);
            UIController.Instance.SetDeathText(0);
        }
    }

    void ShowLeaderboard()
    {
        UIController.Instance.SetLeaderboardActive();
        //UIController.Instance.leaderboard.SetActive(true);

        foreach (LeaderboardPlayer lp in lboardPlayers)
        {
            Destroy(lp.gameObject);
        }
        lboardPlayers.Clear();

        UIController.Instance.SetLeaderboardPlayerDisplay();
        //UIController.Instance.leaderboardPlayerDisplay.gameObject.SetActive(false);

        List<PlayerInfo> sorted = SortPlayers(allPlayers);

        foreach (PlayerInfo player in sorted)
        {
            LeaderboardPlayer newPlayerDisplay = Instantiate(UIController.Instance.GetLeaderboardPlayerDisplay(), UIController.Instance.GetLeaderboardPlayerDisplay().transform.parent);
            newPlayerDisplay.SetDeatils(player.name, player.kills, player.deaths);
            newPlayerDisplay.gameObject.SetActive(true);
            lboardPlayers.Add(newPlayerDisplay);
        }
    }

    private List<PlayerInfo> SortPlayers(List<PlayerInfo> players)
    {
        List<PlayerInfo> sorted = new List<PlayerInfo>();

        while (sorted.Count < players.Count)
        {
            int highest = -1;
            PlayerInfo selectedPlayer = players[0];

            foreach (PlayerInfo player in players)
            {
                if (!sorted.Contains(player))
                {
                    if (player.kills > highest)
                    {
                        selectedPlayer = player;
                        highest = player.kills;
                    }
                }
            }
            sorted.Add(selectedPlayer);
        }

        return sorted;
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();

        SceneManager.LoadScene("Main Menu");
    }

    void ScoreCheck()
    {
        bool winnerFound = false;

        foreach (PlayerInfo player in allPlayers)
        {
            if (player.kills >= killsToWin && killsToWin > 0)
            {
                winnerFound = true;
                break;
            }
        }

        if (winnerFound)
        {
            if (PhotonNetwork.IsMasterClient && state != GameState.Ending)
            {
                state = GameState.Ending;
                ListPlayerSend();
            }
        }
    }

    void StateCheck()
    {
        if (state == GameState.Ending)
        {
            EndGame();
        }
    }

    void EndGame()
    {
        state = GameState.Ending;

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
        }

        UIController.Instance.GetOptionsScreen().SetActive(false);
        UIController.Instance.SetEndScreen(true);
        ShowLeaderboard();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Camera.main.transform.position = mapCamPoint.position;
        Camera.main.transform.rotation = mapCamPoint.rotation;

        StartCoroutine(EndCo());
    }

    private IEnumerator EndCo()
    {
        yield return new WaitForSeconds(waitAfterEnding);

        if (!perpetual)
        {
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
        } 
        else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (!MapsDetails.Instance.GetChangeMapBetweenRounds())
                {
                    NextMatchSend();
                }
                else
                {
                    int newLevel = Random.Range(0, MapsDetails.Instance.GetAllMaps().Length);
                    if(MapsDetails.Instance.GetAllMaps()[newLevel] == SceneManager.GetActiveScene().name)
                    {
                        NextMatchSend();
                    } else
                    {
                        PhotonNetwork.LoadLevel(MapsDetails.Instance.GetAllMaps()[newLevel]);
                        Debug.Log("New Scene To Load" + MapsDetails.Instance.GetAllMaps()[newLevel] + " " + PhotonNetwork.NickName);
                    }
                }
            }
        }
    }

    public void NextMatchSend()
    {
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NextMatch,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true });
    }

    public void NextMatchReceive()
    {
        state = GameState.Playing;

        UIController.Instance.SetEndScreen(false);
        UIController.Instance.GetLeaderboard().SetActive(false);

        foreach(PlayerInfo player in allPlayers)
        {
            player.kills = 0;
            player.deaths = 0;
        }

        UpdateStatsDisplay();

        PlayerSpawner.Instance.SpawnPlayer();

        SetupTimer();
    }

    public void SetupTimer()
    {
        if(matchLength > 0)
        {
            currentMatchTime = matchLength;
            UpdateTimerDisplay();
        }
    }

    public void UpdateTimerDisplay()
    {
        var timeToDisplay = System.TimeSpan.FromSeconds(currentMatchTime);

        UIController.Instance.SetTimerText(timeToDisplay.Minutes.ToString("00") + ":" + timeToDisplay.Seconds.ToString("00"));
    }
}

[System.Serializable]
public class PlayerInfo
{
    public string name;
    public int actor, kills, deaths;

    public PlayerInfo(string _name, int _actor, int _kills, int _deaths)
    {
        name = _name;
        actor = _actor;
        kills = _kills;
        deaths = _deaths;
    }
}