using UnityEngine;

public class scr_GunLibrary : MonoBehaviour
{
    [SerializeField] [Header("全部武器資料")] scr_WeaponData[] allGuns;

    public static scr_WeaponData[] guns; // 可直接呼叫的武器列表

    void Awake()
    {
        guns = allGuns; // 把所有槍的資料放進 static
    }

    /// <summary>
    /// 找武器
    /// </summary>
    /// <param name="name">武器名稱</param>
    /// <returns></returns>
    public static scr_WeaponData FindGun(string name)
    {
        foreach (scr_WeaponData item in guns) if (item.name.Equals(name)) return item;

        return guns[0];
    }
}
