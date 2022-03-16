using UnityEngine;
using UnityEngine.UI;

public class scr_MenuManager : MonoBehaviour
{
    #region - Variables - 
    [HideInInspector] public Button join_match_btn;
    [HideInInspector] public Button create_match_btn;
    [HideInInspector] public Button quit_btn;

    AudioSource aud;
    scr_Launcher launcher;
    #endregion

    #region - Monobehaviour - 
    void Awake()
    {
        launcher = GameObject.Find("Launcher").GetComponent<scr_Launcher>();
        aud = GameObject.Find("Audio Source").GetComponent<AudioSource>();
        join_match_btn = GameObject.Find("Join Match Btn").GetComponent<Button>();
        create_match_btn = GameObject.Find("Create Match Btn").GetComponent<Button>();
        quit_btn = GameObject.Find("Quit Game Btn").GetComponent<Button>();
    }

    private void Start()
    {
        scr_SceneManager.paused = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        Onclick();
    }
    #endregion

    #region - Methods - 
    /// <summary>
    /// 點擊按鈕
    /// </summary>
    void Onclick()
    {
        join_match_btn.onClick.AddListener(JoinMatch);
        create_match_btn.onClick.AddListener(CreateMatch);
        quit_btn.onClick.AddListener(Quit);
    }

    /// <summary>
    /// 加入房間
    /// </summary>
    void JoinMatch()
    {
        aud.PlayOneShot(aud.clip, 0.4f);
        launcher.Join();
    }

    /// <summary>
    /// 創建房間
    /// </summary>
    void CreateMatch()
    {
        aud.PlayOneShot(aud.clip, 0.4f);
        launcher.CreateRoom();
    }

    /// <summary>
    /// 離開遊戲
    /// </summary>
    void Quit()
    {
        aud.PlayOneShot(aud.clip, 0.4f);
        Application.Quit();
    }
    #endregion
}
