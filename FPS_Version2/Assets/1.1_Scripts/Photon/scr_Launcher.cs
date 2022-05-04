using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class scr_Launcher : MonoBehaviourPunCallbacks
{
    #region - Variable -
    [Header("玩家資料")] public static scr_profile profile = new scr_profile();

    [SerializeField] InputField usernameField;

    string gameVersion = "0.0.0"; // 遊戲版本

    scr_MenuManager menu;
    #endregion

    #region - MonoBehaviour -
    void Awake()
    {
        menu = GameObject.Find("Launcher").GetComponent<scr_MenuManager>();

        gameVersion = "0.0.0";

        // 確保所有連線的玩家均載入相同的遊戲場景
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    void Start()
    {
        profile = scr_PlayerData.LoadProfile();
        usernameField.text = profile.username;

        Connect();
    }
    #endregion

    #region - PunCallbacks -
    /// <summary>
    /// PUN 連接到主伺服器
    /// </summary>
    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();

        Debug.Log("Connected to Master");

        PhotonNetwork.JoinLobby();
    }

    /// <summary>
    /// 連接到 Lobby
    /// </summary>
    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();

        Debug.Log("Connected to Lobby");

        menu.create_match_btn.interactable = true;
        menu.join_match_btn.interactable = true;
        menu.quit_btn.interactable = true;
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
        if (string.IsNullOrEmpty(usernameField.text)) profile.username = "RANDOM_USER_" + Random.Range(0, 999);

        else profile.username = usernameField.text;

        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            scr_PlayerData.SaveProfile(profile);

            PhotonNetwork.LoadLevel("遊戲場景");
        }
    }
    #endregion
}

[System.Serializable]
public class scr_profile
{
    [Header("玩家名稱")] public string username;
    [Header("等級")] public int level;
    [Header("經驗值")] public int xp;

    public scr_profile()
    {
        this.username = "DEFAULT USERNAME";
        this.level = 1;
        this.xp = 0;
    }

    public scr_profile(string name, int lv, int x)
    {
        this.username = name;
        this.level = lv;
        this.xp = x;
    }
}
