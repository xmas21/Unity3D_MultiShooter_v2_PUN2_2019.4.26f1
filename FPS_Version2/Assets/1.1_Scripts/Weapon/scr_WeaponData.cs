using UnityEngine;

[CreateAssetMenu(fileName = "New gun", menuName = "Gun")]
public class scr_WeaponData : ScriptableObject
{
    [Header("武器名稱")] public string name;

    [Header("瞄準速度")] public float aimSpeed;
    [Header("開槍頻率")] public float fireRate;
    [Header("換彈時間")] public float reload_time;
    [Header("後座力")] public float bloom;

    [Header("武器傷害")] public int damage;
    [Header("身上子彈數量")] public int ammo;
    [Header("每個彈夾可以射的子彈數量")] public int clip_size;
    [Header("子彈顆粒")] public int pellets;

    [Header("回復")] public bool recovery;
    [Header("是否可以切換模式")] public bool can_change_mode;
    [Header("武器模式")] public WeaponMode mode;
    [Header("武器預置物")] public GameObject weaponPrefab;

    [HideInInspector] public int current_ammo;
    [HideInInspector] public int current_clip;

    /// <summary>
    /// 初始化子彈
    /// </summary>
    public void Initialize()
    {
        current_clip = clip_size;
        current_ammo = ammo;
    }

    /// <summary>
    /// 可以射擊
    /// </summary>
    /// <returns>是否可以射擊</returns>
    public bool FireBullet()
    {
        if (current_clip > 0)
        {
            current_clip -= 1;
            return true;
        }
        else return false;
    }

    /// <summary>
    /// 裝子彈
    /// </summary>
    public void Reload()
    {
        // 所有的子彈 = 身上的 + 槍裡面的
        current_ammo += current_clip;
        // 假如身上子彈 > 彈夾容量 => 裝容量數量得子彈 
        // 不然就裝剩餘的子彈
        current_clip = Mathf.Min(clip_size, current_ammo);
        // 身上的子彈 = 所有的 - 槍裡面的
        current_ammo -= current_clip;

    }

    /// <summary>
    /// 回傳子彈數量
    /// </summary>
    /// <returns>子彈數量</returns>
    public int CallAmmo() { return current_ammo; }

    /// <summary>
    /// 回禪彈夾數量
    /// </summary>
    /// <returns>彈夾數量</returns>
    public int CallClip() { return current_clip; }
}

/// <summary>
/// 武器模式
/// </summary>
public enum WeaponMode
{
    auto, single
}
