using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class scr_Launcher : MonoBehaviourPunCallbacks
{
    #region - Variable -
    [Header("玩家資料")] public static scr_profile profile = new scr_profile();

    [SerializeField] InputField usernameField;
    [SerializeField] [Header("主頁面")] GameObject mainPage;
    [SerializeField] [Header("房間頁面")] GameObject roomPage;
    [SerializeField] [Header("房間按鈕")] GameObject room_Btn;
    [SerializeField] [Header("房間列表")] List<RoomInfo> room_List;

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
        Debug.Log("Connected to Master");

        PhotonNetwork.JoinLobby();

        base.OnConnectedToMaster();
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

    /// <summary>
    /// 房間資訊更新
    /// </summary>
    /// <param name="roomList">房間列表</param>
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        room_List = roomList;
        ClearRoomList();

        Transform content = roomPage.transform.Find("Scroll View/Viewport/Content");

        foreach (RoomInfo info in room_List)
        {
            GameObject newRoomButton = Instantiate(room_Btn, content) as GameObject;

            newRoomButton.transform.Find("Name").GetComponent<Text>().text = info.Name;
            newRoomButton.transform.Find("Count").GetComponent<Text>().text = info.PlayerCount + " / " + info.MaxPlayers;

            newRoomButton.GetComponent<Button>().onClick.AddListener(delegate { Join(newRoomButton.transform); });
        }

        base.OnRoomListUpdate(roomList);
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
    /// <param name="t_button">房間的按鈕</param>
    public void Join(Transform t_button)
    {
        Debug.Log("JOINING ROOM @ " + Time.time);
        string t_roomname = t_button.transform.Find("Name").GetComponent<Text>().text;
        PhotonNetwork.JoinRoom(t_roomname);
    }

    /// <summary>
    /// 建立房間
    /// </summary>
    public void CreateRoom()
    {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 8;

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

    /// <summary>
    /// 關閉全部頁面
    /// </summary>
    public void PageCloseAll()
    {
        mainPage.SetActive(false);
        roomPage.SetActive(false);
    }

    /// <summary>
    /// 開啟主要頁面
    /// </summary>
    public void OpenMainPage()
    {
        PageCloseAll();
        mainPage.SetActive(true);
    }

    /// <summary>
    /// 開啟房間頁面
    /// </summary>
    public void OpenRoomPage()
    {
        PageCloseAll();
        roomPage.SetActive(true);
    }

    /// <summary>
    /// 清除房間清單
    /// </summary>
    public void ClearRoomList()
    {
        Transform content = roomPage.transform.Find("Scroll View/Viewport/Content");
        foreach (Transform room in content) Destroy(room.gameObject);
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
