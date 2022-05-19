using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class scr_GameManager : MonoBehaviour, IOnEventCallback
{
    #region - Variables -
    [SerializeField] [Header("玩家預置物名稱")] string playerPrefab_String;
    [SerializeField] [Header("玩家預置物名稱")] GameObject playerPrefab;
    [SerializeField] [Header("生成點")] Transform[] spawnPoints;

    [SerializeField] [Header("玩家列表")] List<PlayerInfo> playerInfos = new List<PlayerInfo>();
    [SerializeField] [Header("個人 ID")] int myID;
    #endregion

    #region - MonoBehaviour -
    void Start()
    {
        ValidateConnection();
        Spawn();
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
                break;
            case EventCodes.UpdatePlayers:
                break;
            case EventCodes.ChangeState:
                break;
            default:
                break;
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
        object[] package = new object[6];

        package[0] = profile.username;
        package[1] = profile.level;
        package[2] = profile.xp;
        package[3] = PhotonNetwork.LocalPlayer.ActorNumber;
        package[4] = (short)0;
        package[5] = (short)0;

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
        PlayerInfo pi = new PlayerInfo(new scr_profile((string)data[0], (int)data[1], (int)data[2]), (int)data[3], (short)data[4], (short)data[5]);

        playerInfos.Add(pi);

        UpdatePlayer_S(playerInfos);
    }

    /// <summary>
    /// Host更新玩家 -寄送 (Send)
    /// </summary>
    /// <param name="info">玩家資訊</param>
    public void UpdatePlayer_S(List<PlayerInfo> info)
    {
        // 房間內玩家數量
        object[] package = new object[info.Count];

        for (int i = 0; i < info.Count; i++)
        {
            // 玩家的個人資料
            object[] piece = new object[6];

            piece[0] = info[i].profile.username;
            piece[1] = info[i].profile.level;
            piece[2] = info[i].profile.xp;
            piece[3] = info[i].actor;
            piece[4] = info[i].kills;
            piece[5] = info[i].deaths;

            package[i] = piece[i];
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
        playerInfos = new List<PlayerInfo>();

        // 每個玩家的資訊
        for (int i = 0; i < data.Length; i++)
        {
            object[] extract = (object[])data[i];

            PlayerInfo pi = new PlayerInfo(new scr_profile((string)extract[0], (int)extract[1], (int)extract[2]), (int)extract[3], (short)extract[4], (short)extract[5]);

            playerInfos.Add(pi);

            if (PhotonNetwork.LocalPlayer.ActorNumber == pi.actor) myID = i;
        }
    }

    /// <summary>
    /// 改變狀態 - 發送 (Send)
    /// </summary>
    /// <param name="actor">ID</param>
    /// <param name="state">狀態</param>
    /// <param name="amt">數量</param>
    public void ChangeState_S(int actor, byte state, byte amt)
    {
        object[] package = new object[] { actor, state, amt };

        PhotonNetwork.RaiseEvent(
         (byte)EventCodes.ChangeState,
         package,
         new RaiseEventOptions { Receivers = ReceiverGroup.All },
         new SendOptions { Reliability = true });
    }

    /// <summary>
    /// 改變狀態 - 接收 (Recieve)
    /// </summary>
    /// <param name="data">玩家資訊</param>
    public void ChangeState_R(object[] data)
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
                return;
            }
        }
    }
    #endregion

    #region - Methods -
    /// <summary>
    /// 生成
    /// </summary>
    public void Spawn()
    {
        Transform temp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        PhotonNetwork.Instantiate(playerPrefab_String, temp.position, temp.rotation);
    }

    /// <summary>
    /// 驗證連結
    /// </summary>
    private void ValidateConnection()
    {
        if (PhotonNetwork.IsConnected) return;
        SceneManager.LoadScene(0);
    }
    #endregion
}

public enum EventCodes : byte
{
    Newplayer, UpdatePlayers, ChangeState
}

/// <summary>
/// 角色資訊
/// </summary>
public class PlayerInfo
{
    public scr_profile profile;
    [Header("玩家 ID")] public int actor;
    public short kills;
    public short deaths;

    public PlayerInfo(scr_profile p, int a, short k, short d)
    {
        this.profile = p;
        this.actor = a;
        this.kills = k;
        this.deaths = d;
    }
}
