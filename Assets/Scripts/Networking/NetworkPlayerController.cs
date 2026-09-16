/*
 玩家网络控制器

 阶段 2（已落地）：
   1. 出生点分配：按 OwnerClientId % 出生点数量 取点，所有端各自本地应用同一条确定性规则，
      避免"只在服务端设置位置"被 owner 权威 transform 覆盖回 prefab 原点；
   2. 最小所有权门控：非 Owner 端禁用本地逻辑组件、把刚体切成 kinematic，使其成为木偶。

 阶段 3（本版新增）：
   3. 相机组门控：非 Owner 端关闭 Camera(3D)/Camera2D 并禁用 DimensionCameraController，
      让每台机器只有本地玩家的 vcam 生效；
   4. 光标锁统一管理：按 Owner + 窗口焦点锁/解锁，Esc 解锁、再按锁回，OnDisable 兜底释放；
   5. 诊断日志（-net-debug 门控）：spawn / focus / 1Hz 相机快照，供打包端取证。

 阶段 4（本版新增）：
   6. 状态与动画同步门控：Owner 端启用 NetworkStateSync（写同步变量），
      非 Owner 端启用 NetworkAnimationPresenter（用同步变量驱动木偶 Animancer）；
   7. spawn 诊断行补充 syncEnabled / presenterEnabled 两个字段。
 */
using Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerController : NetworkBehaviour
{
    [Header("逻辑组件（非 Owner 端禁用）")]
    [SerializeField] private InputPipeline inputPipeline;
    [SerializeField] private MainProcessPipeline mainProcessPipeline;
    [SerializeField] private CharacterStateMachine characterStateMachine;
    [SerializeField] private MotionDriver motionDriver;
    [SerializeField] private GroundDetector groundDetector;

    [Header("相机组（阶段 3 开关用）")]
    [SerializeField] private GameObject camera3D;
    [SerializeField] private GameObject camera2D;
    [SerializeField] private DimensionCameraController dimensionCameraController;

    [Header("物理")]
    [SerializeField] private Rigidbody playerRigidbody;

    [Header("阶段 4：状态与动画同步")]
    [SerializeField] private NetworkStateSync networkStateSync;
    [SerializeField] private NetworkAnimationPresenter animationPresenter;

    private bool _isLocalOwner;
    private bool _netDebug;

    private void Awake()
    {
        // 兜底查找：SwitchView 是常驻激活物体，即使 prefab 忘接线也能取到
        if (dimensionCameraController == null)
        {
            dimensionCameraController = GetComponentInChildren<DimensionCameraController>(true);
        }

        _netDebug = NetworkGameManager.HasCommandLineArg("-net-debug");
    }

    public override void OnNetworkSpawn()
    {
        _isLocalOwner = IsOwner;

        ApplySpawnPoint();
        ApplyOwnershipGate();
        LogSpawnState();

        if (_netDebug)
        {
            InvokeRepeating(nameof(LogCameraSnapshot), 1f, 1f);
        }
    }

    private void Update()
    {
        // 阶段 3：Esc 解锁 / 再按锁回。只有本地玩家轮询，远端控制器直接早退。
        if (!_isLocalOwner) return;
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        ApplyCursorLock(Cursor.lockState != CursorLockMode.Locked);
        if (_netDebug)
        {
            Debug.Log($"[NetDbg][esc] owner={OwnerClientId} cursor={Cursor.lockState}");
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!_isLocalOwner) return;

        // 回到窗口即锁回（覆盖此前 Esc 的解锁状态）
        ApplyCursorLock(hasFocus);
        if (_netDebug)
        {
            Debug.Log($"[NetDbg][focus] owner={OwnerClientId} hasFocus={hasFocus} cursor={Cursor.lockState}");
        }
    }

    private void OnDisable()
    {
        // 唯一的光标解锁出口：对象销毁 / 池化失活 / 退出 Play Mode 都会走到这里
        if (_isLocalOwner) ApplyCursorLock(false);
        CancelInvoke(nameof(LogCameraSnapshot));
    }

    /// <summary>
    /// 所有端（含服务端）都用同一条确定性规则算出同一个出生点，并各自本地应用。
    /// </summary>
    private void ApplySpawnPoint()
    {
        Transform[] points = NetworkGameManager.SpawnPoints;
        if (points == null || points.Length == 0)
        {
            Debug.LogWarning("[NetworkPlayerController] 未配置出生点，沿用 prefab 原始位置");
            return;
        }

        int index = (int)(OwnerClientId % (ulong)points.Length);
        Transform point = points[index];
        if (point == null)
        {
            Debug.LogWarning($"[NetworkPlayerController] 出生点 [{index}] 为空引用，沿用原始位置");
            return;
        }

        transform.SetPositionAndRotation(point.position, point.rotation);
    }

    /// <summary>
    /// 非 Owner：禁用输入/状态机/移动逻辑、刚体转 kinematic、关闭相机组，成为由网络驱动的木偶。
    /// Owner：让本类成为相机状态与光标锁的唯一权威入口。
    /// </summary>
    private void ApplyOwnershipGate()
    {
        if (!IsOwner)
        {
            if (inputPipeline != null) inputPipeline.enabled = false;
            if (mainProcessPipeline != null) mainProcessPipeline.enabled = false;
            if (characterStateMachine != null) characterStateMachine.enabled = false;
            if (motionDriver != null) motionDriver.enabled = false;
            if (groundDetector != null) groundDetector.enabled = false;
            if (playerRigidbody != null) playerRigidbody.isKinematic = true;

            // 阶段 3：先禁控制器（它只在 enabled 重入时才会 ApplySwitch），再关两个相机物体。
            // 关掉 Camera 等于同时停掉其上的 CinemachineInputProvider 与 ThridCamera：
            // 远端既不消费 Look，也不会订阅滚轮。
            if (dimensionCameraController != null) dimensionCameraController.enabled = false;
            if (camera3D != null) camera3D.SetActive(false);
            if (camera2D != null) camera2D.SetActive(false);

            // 阶段 4：木偶的动画表现层接管；同步变量只由 Owner 写
            if (networkStateSync != null) networkStateSync.enabled = false;
            if (animationPresenter != null) animationPresenter.enabled = true;
        }
        else
        {
            // 防御性：让门控成为相机状态的唯一权威入口。
            // MVP 下 prefab 已由 DimensionCameraController.OnEnable 把 3D 摆好，这一步不改变现有状态。
            if (dimensionCameraController != null)
            {
                dimensionCameraController.enabled = true;
                dimensionCameraController.ApplyCurrentView();
            }

            // 阶段 4：本地玩家的动画由状态机自己驱动，同步变量由本端写
            if (networkStateSync != null) networkStateSync.enabled = true;
            if (animationPresenter != null) animationPresenter.enabled = false;

            ApplyCursorLock(Application.isFocused);
        }
    }

    private static void ApplyCursorLock(bool locked)
    {
        // 与单机基线一致：只改 lockState，不碰 Cursor.visible（lockState=None 时系统自动显示光标）
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
    }

    #region 诊断日志（-net-debug，默认关闭；供打包端取证）

    private void LogSpawnState()
    {
        if (!_netDebug) return;

        string kinematic = playerRigidbody != null ? playerRigidbody.isKinematic.ToString() : "null";
        bool stateStartRan = characterStateMachine != null && characterStateMachine.HasStarted;

        Debug.Log($"[NetDbg][spawn] owner={OwnerClientId} isOwner={IsOwner} " +
                  $"cam3D={ActiveSelf(camera3D)} cam2D={ActiveSelf(camera2D)} dcamEnabled={EnabledState(dimensionCameraController)} " +
                  $"kinematic={kinematic} stateStartRan={stateStartRan} " +
                  $"syncEnabled={EnabledState(networkStateSync)} presenterEnabled={EnabledState(animationPresenter)} " +
                  $"cursor={Cursor.lockState} root={transform.position.ToString("F3")}");
    }

    private void LogCameraSnapshot()
    {
        CinemachineCore core = CinemachineCore.Instance;
        CinemachineBrain brain = core.BrainCount > 0 ? core.GetActiveBrain(0) : null;
        ICinemachineCamera active = brain != null ? brain.ActiveVirtualCamera : null;
        GameObject activeGo = active != null ? active.VirtualCameraGameObject : null;
        bool activeIsMine = activeGo != null && camera3D != null && activeGo == camera3D;

        CinemachinePOV pov = null;
        CinemachineFramingTransposer framing = null;
        if (camera3D != null)
        {
            CinemachineVirtualCamera vcam = camera3D.GetComponent<CinemachineVirtualCamera>();
            if (vcam != null)
            {
                pov = vcam.GetCinemachineComponent<CinemachinePOV>();
                framing = vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
            }
        }

        string povX = pov != null ? pov.m_HorizontalAxis.Value.ToString("F3") : "-";
        string povY = pov != null ? pov.m_VerticalAxis.Value.ToString("F3") : "-";
        string dist = framing != null ? framing.m_CameraDistance.ToString("F3") : "-";
        int listeners = FindObjectsOfType<AudioListener>(true).Length;
        // pos/t 不属于阶段 3 验收的最小字段，但打包端没有反射可采样：
        // 有 position 才能自证"移动同步通道 / 远端木偶是否在动"，有 t 才能自证"失焦实例是否仍在跑"。
        string bodyPos = playerRigidbody != null ? playerRigidbody.position.ToString("F3") : "null";

        Debug.Log($"[NetDbg][cam] owner={OwnerClientId} isOwner={IsOwner} vcamCount={core.VirtualCameraCount} " +
                  $"brains={core.BrainCount} listeners={listeners} activeIsMine={activeIsMine} " +
                  $"cam3D={ActiveSelf(camera3D)} cam2D={ActiveSelf(camera2D)} " +
                  $"povX={povX} povY={povY} dist={dist} cursor={Cursor.lockState} " +
                  $"pos={bodyPos} t={Time.realtimeSinceStartup.ToString("F1")}");
    }

    private static string ActiveSelf(GameObject go) => go == null ? "null" : go.activeSelf.ToString();

    private static string EnabledState(Behaviour behaviour) => behaviour == null ? "null" : behaviour.enabled.ToString();

    #endregion
}
