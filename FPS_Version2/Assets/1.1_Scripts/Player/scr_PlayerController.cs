using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

public class scr_PlayerController : MonoBehaviourPunCallbacks
{
    #region - Variables -
    [SerializeField] [Header("滑鼠水平靈敏度")] float mouseSensitivity_X;
    [SerializeField] [Header("滑鼠垂直靈敏度")] float mouseSensitivity_Y;
    [SerializeField] [Header("走路 - 速度")] float walkSpeed;
    [SerializeField] [Header("跑步 - 速度")] float runSpeed;
    [SerializeField] [Header("移動滑順時間")] float moveSmoothTime;
    [SerializeField] [Header("跳躍力道")] float jumpForce;
    [SerializeField] [Header("最大血量")] int maxHealth;
    [SerializeField] [Header("當前血量")] int currentHealth;

    [SerializeField] [Header("攝影機座標")] GameObject cameraHolder;
    [SerializeField] [Header("玩家攝影機")] Camera playerCamera;
    [SerializeField] [Header("武器座標")] Transform weapon_Trans;
    [SerializeField] [Header("發射點座標")] Transform shoot_Trans;

    [HideInInspector] public bool isGrounded;

    bool cursorLocked = true;           // 滑鼠鎖定
    bool isRunning = false;

    float lookRotation;                 // 上下視角旋轉值
    float walkFOV;                      // 走路視野
    float runFOV;                       // 跑步視野
    float counter;                      // 呼吸武器變數

    Vector3 moveSmoothVelocity;         // 滑順加速度
    Vector3 moveDir;                    // 移動到的位置
    Vector3 target_weapon_Trans;        // 武器目標座標

    Text ammo_UI;
    Image healthBar;
    Rigidbody rig;
    scr_Weapon scr_weapon;
    scr_GameManager scr_gameManager;

    #endregion

    #region - Callbacks -
    void Awake()
    {
        weapon_Trans = transform.GetChild(2).transform;
        shoot_Trans = transform.GetChild(3).transform;
        playerCamera = transform.GetChild(1).GetChild(0).GetComponent<Camera>();
        scr_weapon = GetComponent<scr_Weapon>();
        scr_gameManager = GameObject.Find("GameManager").GetComponent<scr_GameManager>();
        healthBar = GameObject.Find("HUD/血量顯示器/Health/bar").GetComponent<Image>();
        ammo_UI = GameObject.Find("HUD/子彈/Text").GetComponent<Text>();
        rig = GetComponent<Rigidbody>();
    }

    void Start()
    {
        cameraHolder.SetActive(photonView.IsMine);

        if (!photonView.IsMine) gameObject.layer = 11;

        walkFOV = playerCamera.fieldOfView;
        runFOV = walkFOV * 1.15f;
        currentHealth = maxHealth;
    }

    void Update()
    {
        // 只控制自己生成的物件
        if (!photonView.IsMine) return;

        Move();
        View();
        Jump();
        CursorLock();
        BreathSwitch();
        UpdateHpBar();
        UpdateAmmo();
    }

    void FixedUpdate()
    {
        rig.MovePosition(rig.position + transform.TransformDirection(moveDir) * Time.deltaTime);
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.name == "地圖外")
        {
            Die();
        }
    }
    #endregion

    #region - Methods -
    /// <summary>
    /// 受傷
    /// </summary>
    /// <param name="damage">傷害值</param>
    public void TakeDamage(int damage)
    {
        if (photonView.IsMine)
        {
            currentHealth -= damage;

            if (currentHealth <= 0)
            {
                Die();
            }
        }
    }

    /// <summary>
    /// 鼠標消失
    /// </summary>
    void CursorLock()
    {
        if (cursorLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                cursorLocked = false;
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                cursorLocked = true;
            }
        }
    }

    /// <summary>
    /// 移動
    /// </summary>
    void Move()
    {
        Vector3 direction = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        // 判斷是否在跑步
        isRunning = Input.GetKey(KeyCode.LeftShift) & Input.GetKey(KeyCode.W);

        // 跑步中調整 FOV
        if (isRunning) playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, runFOV, Time.deltaTime * 10f);
        else playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, walkFOV, Time.deltaTime * 10f); ;

        // 滑順移動
        moveDir = Vector3.SmoothDamp(moveDir, direction * (isRunning ? runSpeed : walkSpeed), ref moveSmoothVelocity, moveSmoothTime);
    }

    /// <summary>
    /// 視角
    /// </summary>
    void View()
    {
        // 角色直接左右旋轉 (X軸)
        transform.Rotate(Vector3.up * Input.GetAxisRaw("Mouse X") * mouseSensitivity_X * Time.deltaTime * 60f);

        lookRotation += Input.GetAxisRaw("Mouse Y") * mouseSensitivity_Y * Time.deltaTime * 60f;
        lookRotation = Mathf.Clamp(lookRotation, -80, 75);

        // 攝影機角度轉換 (Y軸)
        cameraHolder.transform.localEulerAngles = -Vector3.right * lookRotation;

        // 讓武器同步轉角度
        weapon_Trans.rotation = cameraHolder.transform.rotation;

        // 讓子彈同步轉角度
        shoot_Trans.rotation = cameraHolder.transform.rotation;
    }

    /// <summary>
    /// 跳躍
    /// </summary>
    void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rig.AddForce(transform.up * jumpForce);
        }
    }

    /// <summary>
    /// 呼吸
    /// </summary>
    /// <param name="p_x">X 倍率</param>
    /// <param name="p_y">Y 倍率</param>
    void Breath(float p_x, float p_y)
    {
        Vector3 temp = new Vector3(Mathf.Cos(counter) * p_x, Mathf.Sin(counter * 2) * p_y, 0);
        target_weapon_Trans = temp + weapon_Trans.localPosition;
    }

    /// <summary>
    /// 呼吸搖擺切換
    /// </summary>
    void BreathSwitch()
    {
        Vector3 direction = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        if (scr_weapon.isAim)
        {
            Breath(0, 0);
        }
        else if (direction == Vector3.zero)
        {
            Breath(0.02f, 0.02f);
            counter += Time.deltaTime;
            weapon_Trans.localPosition = Vector3.Lerp(weapon_Trans.localPosition, target_weapon_Trans, Time.deltaTime);
        }
        else if (!isRunning)
        {
            Breath(0.05f, 0.05f);
            counter += Time.deltaTime * 5f;
            weapon_Trans.localPosition = Vector3.Lerp(weapon_Trans.localPosition, target_weapon_Trans, Time.deltaTime * 2f);
        }
        else
        {
            Breath(0.08f, 0.08f);
            counter += Time.deltaTime * 8f;
            weapon_Trans.localPosition = Vector3.Lerp(weapon_Trans.localPosition, target_weapon_Trans, Time.deltaTime * 4f);
        }
    }

    /// <summary>
    /// 死亡
    /// </summary>
    void Die()
    {
        scr_gameManager.Spawn();
        PhotonNetwork.Destroy(gameObject);
    }

    /// <summary>
    /// 更新血條資訊
    /// </summary>
    void UpdateHpBar()
    {
        healthBar.fillAmount = (float)currentHealth / maxHealth;
    }

    /// <summary>
    /// 更新子彈UI
    /// </summary>
    void UpdateAmmo()
    {
        ammo_UI.text = scr_weapon.UpdateAmmo();
    }

    #endregion
}
