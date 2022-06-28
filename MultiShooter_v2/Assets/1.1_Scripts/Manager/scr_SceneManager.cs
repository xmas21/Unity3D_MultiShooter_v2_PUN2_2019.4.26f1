using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class scr_SceneManager : MonoBehaviour
{
    public static bool paused = false;
    bool disconnecting = false;

    private void Start()
    {
        paused = false;
        disconnecting = false;
    }

    /// <summary>
    /// 暫停
    /// </summary>
    public void Pause()
    {
        if (disconnecting) return;

        paused = !paused;

        transform.GetChild(1).gameObject.SetActive(paused);
        Cursor.lockState = (paused) ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;
    }

    /// <summary>
    /// 離開房間
    /// </summary>
    public void Quit()
    {
        disconnecting = true;
        PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("選單");
    }
}
