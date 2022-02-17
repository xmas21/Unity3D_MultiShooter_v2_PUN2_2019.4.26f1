﻿using UnityEngine;
using Photon.Pun;

public class scr_Weapon : MonoBehaviourPunCallbacks
{
    [Header("武器資料")] public scr_WeaponData[] weaponDatas;
    [Header("武器座標")] public Transform weaponPosition;
    [Header("彈孔預置物")] public GameObject bulletHolePrefab;
    [Header("可以射擊的圖層")] public LayerMask canBeShot;

    [HideInInspector] public bool isAim;

    int currentWeaponIndex;      // 武器編號
    float currentCoolDown;       // 開槍計時器

    Transform anchor_Trans;      // 武器座標
    Transform base_Trans;        // 一般武器座標
    Transform aim_Trans;         // 瞄準武器座標

    GameObject currentWeapon;    // 目前手上的武器
    scr_PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<scr_PlayerController>();
    }

    void Update()
    {
        if (!photonView.IsMine) return;
        Onclick();
        CoolDown();
    }

    /// <summary>
    /// 裝備武器
    /// </summary>
    /// <param name="weapon_ID">武器編號</param>
    [PunRPC]
    void Equip(int weapon_ID)
    {
        // 裝備前先清除所有手上槍枝
        if (currentWeapon != null) Destroy(currentWeapon);

        currentWeaponIndex = weapon_ID;

        GameObject newWeapon = Instantiate(weaponDatas[weapon_ID].weaponPrefab, weaponPosition.position, weaponPosition.rotation, weaponPosition) as GameObject;
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localEulerAngles = Vector3.zero;

        currentWeapon = newWeapon;
    }

    /// <summary>
    /// 製造彈孔
    /// </summary>
    [PunRPC]
    void Shoot()
    {
        Transform spawn = transform.Find("攝影機座標/玩家攝影機");

        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(spawn.position, spawn.forward, out hit, 1000f, canBeShot))
        {
            // point : The impact point in world space where the ray hit the collider > 射線的準確點
            // normal : The normal of the surface the ray hit. > 平面的垂直線
            GameObject bulletHole = Instantiate(bulletHolePrefab, hit.point + hit.normal * 0.001f, Quaternion.LookRotation(hit.normal, Vector3.up));
            bulletHole.transform.SetParent(hit.collider.transform);
            Destroy(bulletHole, 6f);

            if (hit.collider.gameObject.layer == 11)
            {
                hit.collider.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, weaponDatas[currentWeaponIndex].damage);
            }
        }

        currentCoolDown = weaponDatas[currentWeaponIndex].fireRate;
    }

    /// <summary>
    /// 受傷
    /// </summary>
    /// <param name="damage">傷害值</param>
    [PunRPC]
    void TakeDamage(int damage)
    {
        playerController.TakeDamage(damage);
    }

    /// <summary>
    /// 按鍵觸發
    /// </summary>
    void Onclick()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) { photonView.RPC("Equip", RpcTarget.All, 0); }
        if (currentWeapon != null)
        {
            Aim(Input.GetMouseButton(1));

            if (Input.GetMouseButton(0) && currentCoolDown <= 0) photonView.RPC("Shoot", RpcTarget.All);
        }
    }

    /// <summary>
    /// 瞄準
    /// </summary>
    /// <param name="isAiming">是否瞄準中</param>
    void Aim(bool isAiming)
    {
        isAim = isAiming;
        // 抓取
        anchor_Trans = currentWeapon.transform.Find("Anchor");
        base_Trans = currentWeapon.transform.Find("States/Base");
        aim_Trans = currentWeapon.transform.Find("States/Aim");

        // 假如瞄準中 換武器座標
        if (isAiming)
        {
            anchor_Trans.position = Vector3.Lerp(anchor_Trans.position, aim_Trans.position, Time.deltaTime * weaponDatas[currentWeaponIndex].aimSpeed);
            anchor_Trans.rotation = Quaternion.Lerp(anchor_Trans.rotation, aim_Trans.rotation, Time.deltaTime * weaponDatas[currentWeaponIndex].aimSpeed);
        }
        else
        {
            anchor_Trans.position = Vector3.Lerp(anchor_Trans.position, base_Trans.position, Time.deltaTime * weaponDatas[currentWeaponIndex].aimSpeed);
            anchor_Trans.rotation = Quaternion.Lerp(anchor_Trans.rotation, base_Trans.rotation, Time.deltaTime * weaponDatas[currentWeaponIndex].aimSpeed);
        }
    }

    /// <summary>
    /// 槍枝冷卻
    /// </summary>
    void CoolDown()
    {
        if (currentCoolDown > 0)
        {
            currentCoolDown -= Time.deltaTime;
        }
    }

}
