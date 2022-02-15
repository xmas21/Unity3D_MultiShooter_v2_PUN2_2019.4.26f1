using Photon.Pun;
using UnityEngine;

public class scr_WeaponSway : MonoBehaviourPunCallbacks
{
    [SerializeField] [Header("搖晃倍率")] float amplifier;
    [SerializeField] [Header("搖晃倍率")] float smooth_time;

    Quaternion origin_rotation; // 原始方位

    void Start()
    {
        origin_rotation = transform.localRotation;
    }

    void Update()
    {
        if (!photonView.IsMine) return;
        MouseSway();
    }

    /// <summary>
    /// 滑鼠移動搖擺
    /// </summary>
    void MouseSway()
    {
        // Detect mouse value
        float mouseX_value = Input.GetAxis("Mouse X");
        float mouseY_value = Input.GetAxis("Mouse Y");

        // Calculate final rotation the weapon sway
        Quaternion X_temp = Quaternion.AngleAxis(amplifier * -mouseX_value, Vector3.up);
        Quaternion Y_temp = Quaternion.AngleAxis(amplifier * mouseY_value, Vector3.right);
        Quaternion target_rotation = origin_rotation * X_temp * Y_temp;

        // Sway
        transform.localRotation = Quaternion.Lerp(transform.localRotation, target_rotation, smooth_time * Time.deltaTime);
    }
}
