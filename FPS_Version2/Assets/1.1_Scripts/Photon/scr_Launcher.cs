using UnityEngine;
using Photon.Pun;

public class scr_Launcher : MonoBehaviourPunCallbacks
{
    #region - Variable -
    string gameVersion = "0.0.0";

    scr_MenuManager menu;
    #endregion

    #region - MonoBehaviour -
    void Awake()
    {
        menu = GameObject.Find("Launcher").GetComponent<scr_MenuManager>();

        // 確保所有連線的玩家均載入相同的遊戲場景
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    void Start()
    {
        Connect();
    }
    #endregion

    #region - PunCallbacks -
    /// <summary>
    /// PUN 連接到主伺服器
    /// </summary>
    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");

        menu.create_match_btn.interactable = true;
        menu.join_match_btn.interactable = true;
        menu.quit_btn.interactable = true;

        base.OnConnectedToMaster();
    }

    /// <summary>
    /// PUN 連接到房間
    /// </summary>
    public override void OnJoinedRoom()
    {
        StartGame();

        base.OnJoinedRoom();
    }

    /// <summary>
    /// 加入房間失敗
    /// </summary>
    /// <param name="returnCode"></param>
    /// <param name="message">失敗原因</param>
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        CreateRoom();

        base.OnJoinRandomFailed(returnCode, message);
    }

    #endregion

    #region - Method -
    /// <summary>
    /// 連結
    /// </summary>
    public void Connect()
    {
        Debug.Log("Connecting ... ");

        // 遊戲版本的編碼 讓 Photon Server 同款遊戲有不同版本的區隔
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
    }

    /// <summary>
    /// 加入房間
    /// </summary>
    public void Join()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    /// <summary>
    /// 建立房間
    /// </summary>
    public void CreateRoom()
    {
        PhotonNetwork.CreateRoom("");
    }

    /// <summary>
    /// 開始遊戲
    /// </summary>
    public void StartGame()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            PhotonNetwork.LoadLevel("遊戲場景");
        }
    }
    #endregion
}
