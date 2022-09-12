using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class scr_GameManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    #region - Variables -
    [SerializeField] [Header("玩家預置物名稱")] string playerPrefab_String;
    [SerializeField] [Header("玩家預置物名稱")] GameObject playerPrefab;
    [SerializeField] [Header("生成點")] Transform[] spawnPoints;

    [SerializeField] [Header("玩家列表")] List<PlayerInfo> playerInfos = new List<PlayerInfo>();
    [SerializeField] [Header("個人 ID")] int myID;

    [SerializeField] [Header("matchLength")] int matchLength = 180;
    [SerializeField] [Header("matchTimer")] Text matchTimer_text;
    [SerializeField] [Header("currentMatchTime")] int currentMatchTime;
    [SerializeField] [Header("timerCoroutine")] Coroutine timerCoroutine;

    public int mainMenu = 0;
    public int killCount = 3;
    public bool perpetual;

    [Header("地圖攝影機")] public GameObject mapCamera;

    Text myKill_text;
    Text myDeath_text;
    Transform leaderBoard_Page;
    Transform endGame_Page;

    GameState gameState = GameState.Waiting;
    #endregion

    #region - MonoBehaviour -
    void Start()
    {
        ValidateConnection();
        InitializeUI();
        InitTimer();
        NewPlayer_S(scr_Launcher.profile);
        Spawn();
    }

    void Update()
    {
        if (gameState == GameState.Ending) return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // true => false
            if (leaderBoard_Page.gameObject.activeSelf) leaderBoard_Page.gameObject.SetActive(false);
            else LeaderBoard(leaderBoard_Page);
        }
    }
    #endregion

    #region - Photon -
    /// <summary>
    /// Called for any incoming events
    /// </summary>
    /// <param name="photonEvent">Photon事件</param>
    public void OnEvent(EventData photonEvent)
    {
        // 255 - 55 內存 > 200下
        if (photonEvent.Code >= 200) return;

        EventCodes ec = (EventCodes)photonEvent.Code;
        object[] o = (object[])photonEvent.CustomData;

        switch (ec)
        {
            case EventCodes.Newplayer:
                NewPlayer_R(o); break;

            case EventCodes.UpdatePlayers:
                UpdatePlayer_R(o); break;

            case EventCodes.ChangeStat:
                ChangeStat_R(o); break;

            case EventCodes.NewMatch:
                NewMatch_R(); break;

            case EventCodes.RefreshTimer:
                RefreshTimer_R(o); break;
        }
    }

    void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
    #endregion

    #region - Events -
    /// <summary>
    /// 新玩家 - 寄送 (Send)
    /// </summary>
    /// <param name="profile">角色資料</param>
    public void NewPlayer_S(scr_profile profile)
    {
        object[] package = new object[7];

        package[0] = profile.username;
        package[1] = profile.level;
        package[2] = profile.xp;
        package[3] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[4] = (short)0;
        package[5] = (short)0;
        package[6] = CalculateTeam();

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.Newplayer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            new SendOptions { Reliability = true }
            );
    }

    /// <summary>
    /// 新玩家 - 接收 (Recieve)
    /// </summary>
    /// <param name="data"></param>
    public void NewPlayer_R(object[] data)
    {
        PlayerInfo pi = new PlayerInfo(
            new scr_profile(
               (string)data[0],
               (int)data[1],
               (int)data[2]),
               (int)data[3],
               (short)data[4],
               (short)data[5],
               (bool)data[6]);

        playerInfos.Add(pi);

        UpdatePlayer_S((int)gameState, playerInfos);
    }

    /// <summary>
    /// Host更新玩家 -寄送 (Send)
    /// </summary>
    /// <param name="info">玩家資訊</param>
    public void UpdatePlayer_S(int _state, List<PlayerInfo> info)
    {
        // 房間內玩家數量
        object[] package = new object[info.Count + 1];

        package[0] = _state;
        for (int i = 0; i < info.Count; i++)
        {
            // 玩家的個人資料
            object[] piece = new object[7];

            piece[0] = info[i].profile.username;
            piece[1] = info[i].profile.level;
            piece[2] = info[i].profile.xp;
            piece[3] = info[i].actor;
            piece[4] = info[i].kills;
            piece[5] = info[i].deaths;
            piece[6] = info[i].awayTeam;

            package[i + 1] = piece;
        }

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.UpdatePlayers,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true });
    }

    /// <summary>
    /// Server所有玩家 - 接收 (Recieve)
    /// </summary>
    /// <param name="data"></param>
    public void UpdatePlayer_R(object[] data)
    {
        gameState = (GameState)data[0];
        playerInfos = new List<PlayerInfo>();

        // 每個玩家的資訊
        for (int i = 1; i < data.Length; i++)
        {
            object[] extract = (object[])data[i];

            PlayerInfo pi = new PlayerInfo(
                new scr_profile(
                    (string)extract[0],
                    (int)extract[1],
                    (int)extract[2]),
                    (int)extract[3],
                    (short)extract[4],
                    (short)extract[5],
                    (bool)extract[6]);

            playerInfos.Add(pi);

            if (PhotonNetwork.LocalPlayer.ActorNumber == pi.actor) myID = i - 1;
        }

        StateCheck();
    }

    /// <summary>
    /// 改變狀態 - 發送 (Send)
    /// </summary>
    /// <param name="actor">ID</param>
    /// <param name="state">0 : Death | 1 : Kill</param>
    /// <param name="amt">數量</param>
    public void ChangeStat_S(int actor, byte stat, byte amt)
    {
        object[] package = new object[] { actor, stat, amt };

        PhotonNetwork.RaiseEvent(
         (byte)EventCodes.ChangeStat,
         package,
         new RaiseEventOptions { Receivers = ReceiverGroup.All },
         new SendOptions { Reliability = true });
    }

    /// <summary>
    /// 改變狀態 - 接收 (Recieve)
    /// </summary>
    /// <param name="data">玩家資訊</param>
    public void ChangeStat_R(object[] data)
    {
        int actor = (int)data[0];
        byte state = (byte)data[1];
        byte amt = (byte)data[2];

        for (int i = 0; i < playerInfos.Count; i++)
        {
            // 確認 ID
            if (playerInfos[i].actor == actor)
            {
                // 狀態切換
                switch (state)
                {
                    case 0: // Kills
                        playerInfos[i].kills += amt;
                        Debug.Log($"Player { playerInfos[i].profile.username} : kills = {playerInfos[i].kills}");
                        break;

                    case 1: // Deaths
                        playerInfos[i].deaths += amt;
                        Debug.Log($"Player { playerInfos[i].profile.username} : deaths = {playerInfos[i].deaths}");
                        break;
                }
                if (i == myID) RefreshMyStats();
                if (leaderBoard_Page.gameObject.activeSelf) LeaderBoard(leaderBoard_Page);

                break;
            }
        }
        ScoreCheck();
    }

    /// <summary>
    /// 新配對 - 發送 (Send)
    /// </summary>
    public void NewMatch_S()
    {
        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.NewMatch,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );
    }

    /// <summary>
    /// 新配對 - 接收 (Recieve)
    /// </summary>
    public void NewMatch_R()
    {
        // set game state to waiting
        gameState = GameState.Waiting;

        // deactivate map camera
        mapCamera.SetActive(false);

        // hide end game ui
        endGame_Page.gameObject.SetActive(false);

        // reset score
        foreach (PlayerInfo pi in playerInfos)
        {
            pi.kills = 0;
            pi.deaths = 0;
        }

        // reset personal stat
        RefreshMyStats();

        // reset matchTimer
        RefreshTimerUI();

        // spawn
        Spawn();
    }

    /// <summary>
    /// 刷新時間 - 發送 (Send)
    /// </summary>
    public void RefreshTimer_S()
    {
        object[] package = new object[] { currentMatchTime };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.RefreshTimer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
            );
    }

    /// <summary>
    /// 刷新時間 - 接收 (Recieve)
    /// </summary>
    public void RefreshTimer_R(object[] _data)
    {
        currentMatchTime = (int)_data[0];
        RefreshTimerUI();
    }

    /// <summary>
    /// 離開房間
    /// </summary>
    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        SceneManager.LoadScene(mainMenu);
    }
    #endregion

    #region - Methods -
    //  Spawn Player Prefabs  //
    public void Spawn()
    {
        Transform temp = spawnPoints[Random.Range(0, spawnPoints.Length)];

        PhotonNetwork.Instantiate(playerPrefab_String, temp.position, temp.rotation);
    }

    //  Valid if PhotonNetwork is Connected  //
    void ValidateConnection()
    {
        if (PhotonNetwork.IsConnected) return;

        SceneManager.LoadScene(mainMenu);
    }

    //  Initialize Player HUD  //
    void InitializeUI()
    {
        myKill_text = GameObject.Find("HUD/KD/Kill Text").GetComponent<Text>();
        myDeath_text = GameObject.Find("HUD/KD/Death Text").GetComponent<Text>();
        matchTimer_text = GameObject.Find("HUD/Timer/Match Timer Text").GetComponent<Text>();
        leaderBoard_Page = GameObject.Find("HUD").transform.Find("Leader Board").transform;
        endGame_Page = GameObject.Find("畫布 Canvas").transform.Find("HUD Gameover").transform;

        mapCamera.SetActive(false);

        RefreshMyStats();
    }

    //  Initialize Match Timer  //
    void InitTimer()
    {
        currentMatchTime = matchLength;

        RefreshTimerUI();

        if (PhotonNetwork.IsMasterClient) timerCoroutine = StartCoroutine(Timer());
    }

    //  Update Self Count  //
    void RefreshMyStats()
    {
        if (playerInfos.Count > myID)
        {
            myKill_text.text = $"{playerInfos[myID].kills} K";
            myDeath_text.text = $"{playerInfos[myID].deaths} D";
        }
        else
        {
            myKill_text.text = "0 K";
            myDeath_text.text = "0 D";
        }
    }

    //  Update Match Timer UI  //
    void RefreshTimerUI()
    {
        string minute = (currentMatchTime / 60).ToString("00");
        string second = (currentMatchTime % 60).ToString("00");

        matchTimer_text.text = $"{minute} : {second}";
    }

    //  Match Timer  //
    IEnumerator Timer()
    {
        yield return new WaitForSeconds(1f);

        currentMatchTime -= 1;

        if (currentMatchTime <= 0)
        {
            timerCoroutine = null;
            UpdatePlayer_S((int)GameState.Ending, playerInfos);
        }
        else
        {
            RefreshTimer_S();
            timerCoroutine = StartCoroutine(Timer());
        }
    }

    //  Leader Board  //
    void LeaderBoard(Transform _leaderboard)
    {
        // clean (超過一個的都刪除)
        for (int i = 2; i < _leaderboard.childCount; i++) Destroy(_leaderboard.GetChild(1).gameObject);

        // set detail
        _leaderboard.Find("Header/Mode").GetComponent<Text>().text = System.Enum.GetName(typeof(GameMode), GameSetting.gameMode);
        _leaderboard.Find("Header/Map").GetComponent<Text>().text = SceneManager.GetActiveScene().name;

        // cache prefab
        GameObject playercard = _leaderboard.GetChild(1).gameObject;
        playercard.SetActive(false);

        // sort
        List<PlayerInfo> sorted = SortPlayer(playerInfos);

        // display per card (already sorted)
        bool alternateColor = false;
        foreach (PlayerInfo pi in sorted)
        {
            GameObject newcard = Instantiate(playercard, _leaderboard) as GameObject;

            if (alternateColor) newcard.GetComponent<Image>().color += new Color32(0, 0, 0, 180);
            alternateColor = !alternateColor;

            newcard.transform.Find("Level").GetComponent<Text>().text = pi.profile.level.ToString("00");
            newcard.transform.Find("Username").GetComponent<Text>().text = pi.profile.username;
            newcard.transform.Find("Score Value").GetComponent<Text>().text = (pi.kills * 100).ToString();
            newcard.transform.Find("Kill Value").GetComponent<Text>().text = pi.kills.ToString();
            newcard.transform.Find("Death Value").GetComponent<Text>().text = pi.deaths.ToString();

            newcard.SetActive(true);
        }

        // activate
        _leaderboard.gameObject.SetActive(true);
    }

    //  Match State Check  //
    void StateCheck()
    {
        if (gameState == GameState.Ending)
            EndGame();
    }

    //  Score Check  //
    void ScoreCheck()
    {
        // Define temporary variables
        bool detectWin = false;

        // Check to see if any player has met the win conditions
        foreach (PlayerInfo pi in playerInfos)
        {
            // free for all
            if (pi.kills >= killCount)
            {
                detectWin = true;
                break;
            }
        }

        // Did we find a winner
        if (detectWin)
        {
            // Are we the master client? is the game still going on
            if (PhotonNetwork.IsMasterClient && gameState != GameState.Ending)
                UpdatePlayer_S((int)GameState.Ending, playerInfos);
        }
    }

    //   End Game
    void EndGame()
    {
        // set game state to end
        gameState = GameState.Ending;

        // set matchTimer to 0
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        currentMatchTime = 0;
        RefreshTimerUI();

        // disable room
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();

            if (!perpetual)
            {
                PhotonNetwork.CurrentRoom.IsVisible = false;
                PhotonNetwork.CurrentRoom.IsOpen = false;
            }
        }

        // activate map camera
        mapCamera.SetActive(true);

        // show end game ui
        endGame_Page.gameObject.SetActive(true);
        LeaderBoard(endGame_Page.Find("Leader Board"));

        // wait x seconds and then return to the main menu
        StartCoroutine(End(6f));
    }

    //   Calculate Team  //
    bool CalculateTeam()
    {
        return false;
    }

    //   End Game  //
    IEnumerator End(float _time)
    {
        yield return new WaitForSeconds(_time);

        if (perpetual)
        {
            // New match
            if (PhotonNetwork.IsMasterClient)
                NewMatch_S();
        }
        else
        {
            // Disconnect
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
        }
    }

    //  Sort Player In Kill Amount  //
    List<PlayerInfo> SortPlayer(List<PlayerInfo> _info)
    {
        List<PlayerInfo> sorted = new List<PlayerInfo>();

        while (sorted.Count < _info.Count)
        {
            // set default
            short highest = -1;
            PlayerInfo killLeader = _info[0];

            foreach (PlayerInfo pi in _info)
            {
                if (sorted.Contains(pi))
                    continue;

                if (pi.kills > highest)
                {
                    killLeader = pi;
                    highest = pi.kills;
                }
            }
            // Add player
            sorted.Add(killLeader);
        }
        return sorted;
    }
    #endregion
}

/// <summary>
/// 事件狀態
/// </summary>
public enum EventCodes : byte
{
    Newplayer,
    UpdatePlayers,
    ChangeStat,
    NewMatch,
    RefreshTimer
}

/// <summary>
/// 遊戲狀態
/// </summary>
public enum GameState
{
    Waiting = 0,
    Starting = 1,
    Playing = 2,
    Ending = 3
}

/// <summary>
/// 角色資訊
/// </summary>
public class PlayerInfo
{
    [Header("玩家資料")] public scr_profile profile;
    [Header("玩家 ID")] public int actor;
    public short kills;
    public short deaths;
    public bool awayTeam;

    public PlayerInfo(scr_profile p, int a, short k, short d, bool t)
    {
        this.profile = p;
        this.actor = a;
        this.kills = k;
        this.deaths = d;
        this.awayTeam = t;
    }
}
