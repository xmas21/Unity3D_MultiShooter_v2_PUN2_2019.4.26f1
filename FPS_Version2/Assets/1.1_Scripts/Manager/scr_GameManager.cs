using UnityEngine;
using Photon.Pun;

public class scr_GameManager : MonoBehaviour
{
    [SerializeField] [Header("玩家預置物名稱")] string playerPrefab;
    [SerializeField] [Header("生成點")] Transform[] spawnPoints;

    private void Start()
    {
        Spawn();
    }

    /// <summary>
    /// 生成
    /// </summary>
    public void Spawn()
    {
        Transform temp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        PhotonNetwork.Instantiate(playerPrefab, temp.position, temp.rotation);
    }
}
