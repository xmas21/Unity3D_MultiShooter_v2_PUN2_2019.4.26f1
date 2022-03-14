using UnityEngine;
using Photon.Pun;
using System.Collections;

public class scr_Weapon : MonoBehaviourPunCallbacks
{
    #region - Variables -
    [Header("武器資料")] public scr_WeaponData[] weaponDatas;
    [Header("武器座標")] public Transform weaponPosition;
    [Header("彈孔預置物")] public GameObject bulletHolePrefab;
    [Header("可以射擊的圖層")] public LayerMask canBeShot;

    [HideInInspector] public bool isAim;

    int currentWeaponIndex;              // 武器編號
    float currentCoolDown;               // 開槍計時器
    bool isReloading = false;            // 是否換彈中

    Transform anchor_Trans;              // 武器座標
    Transform base_Trans;                // 一般武器座標
    Transform aim_Trans;                 // 瞄準武器座標

    GameObject currentWeapon;            // 目前手上的武器
    scr_PlayerController playerController;

    WeaponMode weaponMode;

    #endregion

    #region - Monobehavior
    void Awake()
    {
        playerController = GetComponent<scr_PlayerController>();
    }

    void Start()
    {
        foreach (scr_WeaponData data in weaponDatas) data.Initialize();
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        Onclick();
        CoolDown();
    }
    #endregion

    #region - RPC -
    /// <summary>
    /// 裝備武器
    /// </summary>
    /// <param name="weapon_ID">武器編號</param>
    [PunRPC]
    void Equip(int weapon_ID)
    {
        // 裝備前先清除所有手上槍枝
        if (currentWeapon != null)
        {
            if (isReloading) StopCoroutine("Reload");
            Destroy(currentWeapon);
        }

        currentWeaponIndex = weapon_ID;

        GameObject newWeapon = PhotonView.Instantiate(weaponDatas[weapon_ID].weaponPrefab, weaponPosition.position, weaponPosition.rotation, weaponPosition) as GameObject;
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localEulerAngles = Vector3.zero;
        weaponMode = weaponDatas[currentWeaponIndex].mode;

        currentWeapon = newWeapon;
    }

    /// <summary>
    /// 製造彈孔
    /// </summary>
    [PunRPC]
    void Shoot()
    {
        Transform spawn = transform.Find("Fire_point/Bullet_Point");

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
    #endregion

    #region - IEnumerator -
    /// <summary>
    /// 換子彈
    /// </summary>
    /// <returns>延遲時間</returns>
    IEnumerator Reload(float time)
    {
        isReloading = true;
        currentWeapon.SetActive(false);

        yield return new WaitForSeconds(time);
        weaponDatas[currentWeaponIndex].Reload();

        currentWeapon.SetActive(true);
        isReloading = false;
    }
    #endregion

    #region - Methods - 
    /// <summary>
    /// 更新子彈UI
    /// </summary>
    /// <returns>子彈UI</returns>
    public string UpdateAmmo()
    {
        string hud;

        if (currentWeapon == null)
        {
            hud = "";
        }
        else
        {
            int clip_mount = weaponDatas[currentWeaponIndex].CallClip();
            int ammo_mount = weaponDatas[currentWeaponIndex].CallAmmo();

            hud = clip_mount.ToString() + " / " + ammo_mount.ToString();
        }

        return hud;
    }

    /// <summary>
    /// 按鍵觸發
    /// </summary>
    void Onclick()
    {
        // 裝備武器
        if (Input.GetKeyDown(KeyCode.Alpha1)) { photonView.RPC("Equip", RpcTarget.All, 0); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { photonView.RPC("Equip", RpcTarget.All, 1); }

        // 射擊
        if (currentWeapon != null)
        {
            // 按右鍵 瞄準
            Aim(Input.GetMouseButton(1));

            // 切換武器模式
            if (Input.GetKeyDown(KeyCode.B)) ChangeMode();

            // 射擊
            switch (weaponMode)
            {
                case WeaponMode.auto:
                    if (Input.GetMouseButton(0) && currentCoolDown <= 0)
                    {
                        if (weaponDatas[currentWeaponIndex].FireBullet()) photonView.RPC("Shoot", RpcTarget.All);

                        else if (!isReloading)
                        {
                            StartCoroutine(Reload(weaponDatas[currentWeaponIndex].reload_time));
                        }
                    }
                    break;
                case WeaponMode.single:
                    if (Input.GetMouseButtonDown(0) && currentCoolDown <= 0)
                    {
                        if (weaponDatas[currentWeaponIndex].FireBullet()) photonView.RPC("Shoot", RpcTarget.All);

                        else if (!isReloading)
                        {
                            StartCoroutine(Reload(weaponDatas[currentWeaponIndex].reload_time));
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        // 換子彈
        if (Input.GetKeyDown(KeyCode.R) && weaponDatas[currentWeaponIndex].current_clip != weaponDatas[currentWeaponIndex].clip_size) StartCoroutine(Reload(weaponDatas[currentWeaponIndex].reload_time));
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

    /// <summary>
    /// 切換武器模式
    /// </summary>
    void ChangeMode()
    {
        if (weaponDatas[currentWeaponIndex].can_change_mode)
        {
            if (weaponMode == WeaponMode.auto)
            {
                weaponMode = WeaponMode.single;
            }
            else
            {
                weaponMode = WeaponMode.auto;
            }
        }
        else
        {
            return;
        }
    }
    #endregion
}
