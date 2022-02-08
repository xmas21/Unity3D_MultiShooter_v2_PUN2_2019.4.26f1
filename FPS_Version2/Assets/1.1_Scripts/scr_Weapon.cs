using UnityEngine;

public class scr_Weapon : MonoBehaviour
{
    [Header("武器資料")] public scr_WeaponData[] loadout;

    [Header("武器座標")] public Transform weaponPosition;

    GameObject currentWeapon;

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
    }

    /// <summary>
    /// 裝備武器
    /// </summary>
    /// <param name="weapon_ID">武器編號</param>
    void Equip(int weapon_ID)
    {
        // 裝備前先清除所有手上槍枝
        if (currentWeapon != null) Destroy(currentWeapon);

        GameObject newWeapon = Instantiate(loadout[weapon_ID].weaponPrefab, weaponPosition.position, weaponPosition.rotation, weaponPosition) as GameObject;
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localEulerAngles = Vector3.zero;

        currentWeapon = newWeapon;
    }
}
