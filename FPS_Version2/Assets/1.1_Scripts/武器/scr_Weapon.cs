using UnityEngine;

public class scr_Weapon : MonoBehaviour
{
    [Header("武器資料")] public scr_WeaponData[] weaponDatas;
    [Header("武器座標")] public Transform weaponPosition;

    int currentWeaponIndex;

    Transform anchor_Trans;      // 武器座標
    Transform base_Trans;        // 一般武器座標
    Transform aim_Trans;         // 瞄準武器座標

    GameObject currentWeapon;    // 目前手上的武器

    void Update()
    {
        Onclick();
    }

    /// <summary>
    /// 按鍵觸發
    /// </summary>
    void Onclick()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) Equip(0);
        if (currentWeapon != null) Aim(Input.GetMouseButton(1));
    }

    /// <summary>
    /// 裝備武器
    /// </summary>
    /// <param name="weapon_ID">武器編號</param>
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
    /// 瞄準
    /// </summary>
    /// <param name="isAiming">是否瞄準中</param>
    void Aim(bool isAiming)
    {
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
}
