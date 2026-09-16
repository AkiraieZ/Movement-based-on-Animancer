/*
 玩家网络控制器（阶段 2 范围）：
 1. 出生点分配：按 OwnerClientId % 出生点数量 取点，所有端各自本地应用同一条确定性规则，
    避免"只在服务端设置位置"被 owner 权威 transform 覆盖回 prefab 原点；
 2. 最小所有权门控：非 Owner 端禁用本地逻辑组件、把刚体切成 kinematic，使其成为木偶。

 相机组开关与光标锁的彻底迁移属于阶段 3，本类现在只持有引用（camera3D / camera2D）。
 */
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

    [Header("相机组（阶段 3 开关用，本阶段只持有引用）")]
    [SerializeField] private GameObject camera3D;
    [SerializeField] private GameObject camera2D;

    [Header("物理")]
    [SerializeField] private Rigidbody playerRigidbody;

    public override void OnNetworkSpawn()
    {
        ApplySpawnPoint();
        ApplyOwnershipGate();

        string bodyPos = playerRigidbody != null ? playerRigidbody.position.ToString("F3") : "null";
        string kinematic = playerRigidbody != null ? playerRigidbody.isKinematic.ToString() : "-";
        Debug.Log($"[NetworkPlayerController] spawn owner={OwnerClientId} isOwner={IsOwner} " +
                  $"root={transform.position.ToString("F3")} body={bodyPos} kinematic={kinematic}");
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
    /// 非 Owner：禁用输入/状态机/移动逻辑，刚体转 kinematic，成为由网络驱动的木偶。
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
        }

        // 兜底：禁用远端 MotionDriver 会触发它的 OnDisable -> Cursor.lockState = None，
        // 而本机自己的角色可能先于远端生成，所以这里统一按"本机是否已有已生成的玩家对象"重新锁一次。
        // 阶段 3.4 会把光标锁整体迁移到本类，这里只是过渡手段。
        LockCursorIfLocalPlayerReady();
    }

    private static void LockCursorIfLocalPlayerReady()
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || manager.LocalClient == null) return;

        NetworkObject localPlayer = manager.LocalClient.PlayerObject;
        if (localPlayer != null && localPlayer.IsSpawned)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
