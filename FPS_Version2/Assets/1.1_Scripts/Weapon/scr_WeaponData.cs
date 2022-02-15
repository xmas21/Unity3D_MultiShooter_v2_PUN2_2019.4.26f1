using UnityEngine;

[CreateAssetMenu(fileName = "New gun", menuName = "Gun")]
public class scr_WeaponData : ScriptableObject
{

    [Header("武器名稱")] public string name;

    [Header("瞄準速度")] public float aimSpeed;
    [Header("開槍頻率")] public float fireRate;
    [Header("武器傷害")] public float damage;

    [Header("武器預置物")] public GameObject weaponPrefab;
}
