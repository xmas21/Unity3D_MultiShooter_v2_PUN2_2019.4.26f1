using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

public class scr_Weapon : MonoBehaviourPunCallbacks
{
    #region - Variables -
    [Header("武器資料")] public List<scr_WeaponData> weaponDatas;
    [Header("武器座標")] public Transform weaponPosition;
    [Header("彈孔預置物")] public GameObject bulletHolePrefab;
    [Header("可以射擊的圖層")] public LayerMask canBeShot;

    [HideInInspector] public bool isAim;

    public Image hitmarker_img;
    public float hitmarkerTime;

    int currentWeaponIndex;              // 武器編號
    float currentCoolDown;               // 開槍計時器
    bool isReloading = false;            // 是否換彈中

    Transform anchor_Trans;              // 武器座標
    Transform base_Trans;                // 一般武器座標
    Transform aim_Trans;                 // 瞄準武器座標
    Transform cameraHolder;

    GameObject currentWeapon;            // 目前手上的武器
    scr_PlayerController playerController;

    WeaponMode weaponMode;
    #endregion

    #region - Monobehavior
    void Awake()
    {
        playerController = GetComponent<scr_PlayerController>();
        hitmarker_img = GameObject.Find("HUD/Hit_marker/Image").GetComponent<Image>();
        cameraHolder = playerController.cameraHolder.transform;
    }

    void Start()
    {
        foreach (scr_WeaponData data in weaponDatas) data.Initialize();
        currentWeaponIndex = -1;
        hitmarker_img.color = new Color(1, 1, 1, 0);
    }

    void Update()
    {
        if (scr_SceneManager.paused) return;
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
        // clear before equip weapon
        if (currentWeapon != null && currentWeaponIndex != weapon_ID)
        {
            if (isReloading) StopCoroutine("Reload");
            Destroy(currentWeapon);
        }
        else if (currentWeaponIndex == weapon_ID) return;

        currentWeaponIndex = weapon_ID;

        GameObject newWeapon = PhotonView.Instantiate(weaponDatas[weapon_ID].weaponPrefab, weaponPosition.position, weaponPosition.rotation, weaponPosition) as GameObject;
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localEulerAngles = Vector3.zero;
        weaponMode = weaponDatas[currentWeaponIndex].mode;

        newWeapon.GetComponent<Animator>().SetTrigger("裝備");

        currentWeapon = newWeapon;
    }

    /// <summary>
    /// 製造彈孔
    /// </summary>
    [PunRPC]
    void Shoot()
    {
        Transform spawn = transform.Find("Fire_point/Bullet_Point");

        // cooldown
        currentCoolDown = weaponDatas[currentWeaponIndex].fireRate;

        for (int i = 0; i < Mathf.Max(1, weaponDatas[currentWeaponIndex].pellets); i++)
        {
            // bloom
            Vector3 v_bloom = spawn.position + spawn.forward * 1000f;
            v_bloom += Random.Range(-weaponDatas[currentWeaponIndex].bloom, weaponDatas[currentWeaponIndex].bloom) * spawn.up * 2;
            v_bloom += Random.Range(-weaponDatas[currentWeaponIndex].bloom, weaponDatas[currentWeaponIndex].bloom) * spawn.right * 0.8f;
            v_bloom -= spawn.position;
            v_bloom.Normalize();

            // Raycast
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(spawn.position, v_bloom, out hit, 1000f, canBeShot))
            {
                // point : The impact point in world space where the ray hit the collider > 射線的準確點
                // normal : The normal of the surface the ray hit. > 平面的垂直線
                GameObject bulletHole = Instantiate(bulletHolePrefab, hit.point + hit.normal * 0.001f, Quaternion.LookRotation(hit.normal, Vector3.up));
                bulletHole.transform.SetParent(hit.collider.transform);
                Destroy(bulletHole, 6f);

                if (hit.collider.gameObject.layer == 11)
                {
                    // give damages
                    hit.collider.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, weaponDatas[currentWeaponIndex].damage);

                    // show hitmarker
                    hitmarker_img.color = new Color(1, 1, 1, 1);
                    hitmarkerTime = 0.5f;
                }
            }
        }
        if (weaponDatas[currentWeaponIndex].recovery) currentWeapon.GetComponent<Animator>().SetTrigger("recovery");
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

    [PunRPC]
    void Reload_RPC()
    {
        StartCoroutine(Reload(weaponDatas[currentWeaponIndex].reload_time));
    }

    [PunRPC]
    /// <summary>
    /// 撿武器
    /// </summary>
    /// <param name="name">武器名稱</param>
    void PickWeapon(string name)
    {
        // find the weapon from library
        scr_WeaponData newWeapon = scr_GunLibrary.FindGun(name);

        // add the weapon to the weaponDatas
        // limit can only carry two guns
        if (weaponDatas.Count >= 2)
        {
            // replace the weapon we're holding
            weaponDatas[currentWeaponIndex] = newWeapon;
            Equip(currentWeaponIndex);
        }
        else
        {
            // just equip
            weaponDatas.Add(newWeapon);
            Equip(weaponDatas.Count - 1);
        }
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

        Aim(false);
        currentWeapon.GetComponent<Animator>().SetTrigger("換彈");
        yield return new WaitForSeconds(time);
        weaponDatas[currentWeaponIndex].Reload();

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
        if (Input.GetKeyDown(KeyCode.Alpha1) && weaponDatas[0] != null) { photonView.RPC("Equip", RpcTarget.All, 0); }
        if (Input.GetKeyDown(KeyCode.Alpha2) && weaponDatas[1] != null) { photonView.RPC("Equip", RpcTarget.All, 1); }

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
                        if (weaponDatas[currentWeaponIndex].FireBullet() && !isReloading) photonView.RPC("Shoot", RpcTarget.All);
                    }
                    break;
                case WeaponMode.single:
                    if (Input.GetMouseButtonDown(0) && currentCoolDown <= 0)
                    {
                        if (weaponDatas[currentWeaponIndex].FireBullet()) photonView.RPC("Shoot", RpcTarget.All);
                    }
                    break;
                default:
                    break;
            }
        }

        // 換子彈
        if (Input.GetKeyDown(KeyCode.R) && weaponDatas[currentWeaponIndex].current_clip != weaponDatas[currentWeaponIndex].clip_size) photonView.RPC("Reload_RPC", RpcTarget.All);
    }

    /// <summary>
    /// 瞄準
    /// </summary>
    /// <param name="isAiming">是否瞄準中</param>
    void Aim(bool isAiming)
    {
        if (isReloading)
        {
            anchor_Trans.position = Vector3.Lerp(anchor_Trans.position, base_Trans.position, Time.deltaTime * 100f);
            anchor_Trans.rotation = Quaternion.Lerp(anchor_Trans.rotation, base_Trans.rotation, Time.deltaTime * 100f);
        }
        else
        {
            isAim = isAiming;

            // 抓取
            anchor_Trans = currentWeapon.transform.Find("Anchor");
            base_Trans = currentWeapon.transform.Find("States/Base");
            aim_Trans = currentWeapon.transform.Find("States/Aim");

            // 假如瞄準中 換武器座標
            if (isAiming)
            {
                playerController.playerCamera.fieldOfView = Mathf.Lerp(playerController.playerCamera.fieldOfView, 50, Time.deltaTime * 10f);
                anchor_Trans.position = Vector3.Lerp(anchor_Trans.position, aim_Trans.position, Time.deltaTime * weaponDatas[currentWeaponIndex].aimSpeed);
                anchor_Trans.rotation = Quaternion.Lerp(anchor_Trans.rotation, aim_Trans.rotation, Time.deltaTime * weaponDatas[currentWeaponIndex].aimSpeed);
            }
            else
            {
                playerController.playerCamera.fieldOfView = Mathf.Lerp(playerController.playerCamera.fieldOfView, 60, Time.deltaTime * 10f);
                anchor_Trans.position = Vector3.Lerp(anchor_Trans.position, base_Trans.position, Time.deltaTime * weaponDatas[currentWeaponIndex].aimSpeed);
                anchor_Trans.rotation = Quaternion.Lerp(anchor_Trans.rotation, base_Trans.rotation, Time.deltaTime * weaponDatas[currentWeaponIndex].aimSpeed);
            }
        }
    }

    /// <summary>
    /// 所有冷卻
    /// </summary>
    void CoolDown()
    {
        if (currentCoolDown > 0) currentCoolDown -= Time.deltaTime;

        if (hitmarkerTime > 0) hitmarkerTime -= Time.deltaTime;
        else if (hitmarker_img.color.a > 0) hitmarker_img.color = Color.Lerp(hitmarker_img.color, new Color(1, 1, 1, 0), Time.deltaTime * 3.5f);
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
