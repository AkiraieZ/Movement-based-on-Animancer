/*
 阶段 4：Owner 权威的动画同步变量。

 职责边界（与 plan/phase4.md 第 2.3 / 3.2 节一致）：
   1. 只由 Owner 写（LateUpdate 采样"本地实际渲染参数"），非 Owner 只读；
   2. 采样的是 Animancer 的实际参数（CartesianMixerState.ParameterX/Y 与
      LinearMixerState.Parameter），不按 rb.velocity 反推——本地状态机写这些参数用的是
      data.rawInput + data.targetLocked，与速度无关（撞墙/被挡时速度≈0 会算出错的姿势）；
   3. 远端（木偶）拿不到 PlayRuntimeData（MainProcessPipeline / GroundDetector 被门控禁用），
      所以 IsGrounded 必须由这里同步，远端不能用自己本地的 data.isGrounded；
   4. 禁止读 Transition.State：idleData.mixer 与 moveData.mixer 是同一个资产实例，
      该属性是资产级全局缓存，同进程里本地玩家与木偶会互相覆盖；一律用 Play() 的返回值。

 写入开销：NetworkVariable 的 Value setter 在值相等时早退（NetworkVariable.cs:122），
 每帧采样写入不会置脏、不会发包。脏变量在同一 tick 的同一消息内到达，远端不会出现
"状态变了但参数还是旧的"跨帧错配。
 */
using Animancer;
using Unity.Netcode;
using UnityEngine;

public class NetworkStateSync : NetworkBehaviour
{
    [Header("依赖（prefab 接线；漏接时 Awake 兜底查找）")]
    [SerializeField] private CharacterStateMachine characterStateMachine;
    [SerializeField] private AnimancerComponent animancer;
    [SerializeField] private GroundDetector groundDetector;

    [Header("同步变量（Owner 写，其余端只读）")]
    public readonly NetworkVariable<byte> AnimationState = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public readonly NetworkVariable<float> MixerX = new(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public readonly NetworkVariable<float> MixerY = new(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public readonly NetworkVariable<float> RunBlend = new(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public readonly NetworkVariable<bool> IsGrounded = new(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private bool _netDebug;
    private float _nextHeartbeat;
    private byte _lastLoggedState = byte.MaxValue;

    private void Awake()
    {
        // 兜底：prefab 忘接线时仍能工作（与阶段 3 的 dimensionCameraController 同款做法）
        if (characterStateMachine == null)
        {
            characterStateMachine = GetComponentInChildren<CharacterStateMachine>(true);
        }
        if (animancer == null)
        {
            animancer = GetComponentInChildren<AnimancerComponent>(true);
        }
        if (groundDetector == null)
        {
            groundDetector = GetComponentInChildren<GroundDetector>(true);
        }

        _netDebug = NetworkGameManager.HasCommandLineArg("-net-debug");
    }

    private void LateUpdate()
    {
        if (!IsSpawned || !IsOwner) return;
        if (characterStateMachine == null || animancer == null) return;

        // 1) 状态标识：远端据此决定播哪一族动画
        byte state = (byte)characterStateMachine.CurrentStateId;
        AnimationState.Value = state;

        // 2) 是否贴地：GroundDetector.Update 里 data.isGrounded 与 isGround 同帧赋值，
        //    这里在 LateUpdate 读到的是本帧最终值（与直接读 data.isGrounded 等价）
        if (groundDetector != null)
        {
            IsGrounded.Value = groundDetector.isGround;
        }

        // 3) 采样本地实际渲染参数。Jump 期 States.Current 是跳跃 ClipState（不是 2D 混合器），
        //    类型判断会自然跳过，变量保持上次值——正是"落地回到 Move 时参数从原处接续"所需语义。
        if (animancer.IsGraphInitialized && animancer.States.Current is CartesianMixerState mixer)
        {
            MixerX.Value = mixer.ParameterX;
            MixerY.Value = mixer.ParameterY;

            int count = mixer.ChildCount;
            for (int i = 0; i < count; i++)
            {
                // 四条方向子混合器被 MoveState 写成同一个值，取第一条即可
                if (mixer.GetChild(i) is LinearMixerState linear)
                {
                    RunBlend.Value = linear.Parameter;
                    break;
                }
            }
        }

        LogAnim(state, "local");
    }

    #region 诊断日志（-net-debug，默认关闭；供打包端取证）

    private void LogAnim(byte state, string role)
    {
        if (!_netDebug) return;

        float now = Time.realtimeSinceStartup;
        bool stateChanged = state != _lastLoggedState;
        if (!stateChanged && now < _nextHeartbeat) return;

        _lastLoggedState = state;
        _nextHeartbeat = now + 1f;

        string clip = "-";
        string nt = "-";
        if (animancer.IsGraphInitialized)
        {
            AnimancerState current = animancer.States.Current;
            if (current != null && current.Clip != null)
            {
                clip = current.Clip.name;
                nt = current.NormalizedTime.ToString("F3");
            }
        }

        Debug.Log($"[NetDbg][anim] owner={OwnerClientId} isOwner={IsOwner} role={role} " +
                  $"state={StateName(state)} mixerX={MixerX.Value.ToString("F3")} mixerY={MixerY.Value.ToString("F3")} " +
                  $"blend={RunBlend.Value.ToString("F3")} grounded={IsGrounded.Value} " +
                  $"clip={clip} nt={nt} t={now.ToString("F1")}" +
                  (stateChanged ? " event=stateChange" : string.Empty));
    }

    internal static string StateName(byte state)
        => state == (byte)CharacterStateMachine.StateId.Move ? "Move"
         : state == (byte)CharacterStateMachine.StateId.Jump ? "Jump"
         : "Idle";

    #endregion
}
