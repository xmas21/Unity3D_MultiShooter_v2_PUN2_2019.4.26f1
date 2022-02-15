using UnityEngine;
using Photon.Pun;

public class scr_GameManager : MonoBehaviour
{
    [SerializeField] [Header("玩家預置物名稱")] string playerPrefab;
    [SerializeField] [Header("生成點")] Transform spawnPoint;

    private void Start()
    {
        Spawn();
    }

    /// <summary>
    /// 生成
    /// </summary>
    void Spawn()
    {
        PhotonNetwork.Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}
