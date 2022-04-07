using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class scr_Pickup : MonoBehaviourPunCallbacks
{
    #region - Variables -
    [SerializeField] [Header("目標物件 - 開關顯示")] List<GameObject> targets;
    [SerializeField] [Header("拾取的武器")] scr_WeaponData weapon;
    [SerializeField] [Header("拾取點")] GameObject pickUpPoint;
    [SerializeField] [Header("顯示的武器")] GameObject gunDisplay;
    [SerializeField] [Header("重新充能時間")] float coolDown;

    bool isDisabled;    // 關閉狀態
    float wait;         // 等待時間
    #endregion

    #region - Monobehaviour -
    void Start()
    {
        // clear all weapon display
        foreach (Transform t in pickUpPoint.transform) Destroy(t.gameObject);

        GameObject newDisplay = PhotonView.Instantiate(gunDisplay, pickUpPoint.transform.position, pickUpPoint.transform.rotation) as GameObject;

        newDisplay.transform.SetParent(pickUpPoint.transform);
    }

    void Update()
    {
        if (isDisabled) Recharge();
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.attachedRigidbody == null) return;

        if (col.attachedRigidbody.gameObject.tag.Equals("Player"))
        {
            scr_Weapon weaponController = col.attachedRigidbody.gameObject.GetComponent<scr_Weapon>();
            weaponController.photonView.RPC("PickWeapon", RpcTarget.All, weapon.name);
            photonView.RPC("Disable", RpcTarget.All);
        }
    }
    #endregion

    #region - RPC -
    /// <summary>
    /// 不可拾取
    /// </summary>
    [PunRPC]
    void Disable()
    {
        isDisabled = true;
        wait = coolDown;

        foreach (GameObject item in targets) item.SetActive(false);
    }
    #endregion

    #region - Methods -
    /// <summary>
    /// 可拾取
    /// </summary>
    void Enable()
    {
        isDisabled = false;
        wait = 0;

        foreach (GameObject item in targets) item.SetActive(true);
    }

    /// <summary>
    /// 充能
    /// </summary>
    void Recharge()
    {
        if (wait >= 0) wait -= Time.deltaTime;

        else Enable();
    }
    #endregion
}
