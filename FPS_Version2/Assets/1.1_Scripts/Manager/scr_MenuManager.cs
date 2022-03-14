using UnityEngine;
using UnityEngine.UI;

public class scr_MenuManager : MonoBehaviour
{
    #region - Variables - 
    Button join_match_btn;
    Button create_match_btn;
    Button quit_btn;
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

    void Update()
    {
        Onclick();
    }
    #endregion

    #region - Methods - 
    void Onclick()
    {
        join_match_btn.onClick.AddListener(JoinMatch);
        create_match_btn.onClick.AddListener(CreateMatch);
        quit_btn.onClick.AddListener(Quit);
    }

    void JoinMatch()
    {
        aud.PlayOneShot(aud.clip, 0.4f);
        launcher.Join();
    }

    void CreateMatch()
    {
        aud.PlayOneShot(aud.clip, 0.4f);
        launcher.CreateRoom();
    }

    void Quit()
    {
        aud.PlayOneShot(aud.clip, 0.4f);
        Application.Quit();
    }
    #endregion
}
