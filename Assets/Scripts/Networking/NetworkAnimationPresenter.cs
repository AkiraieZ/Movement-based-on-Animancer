/*
 阶段 4：远端木偶（非 Owner）的动画表现层。

 设计约束（与 plan/phase4.md 第 3.3 节一致）：
   1. 只读 NetworkStateSync 的同步变量——远端 runtime data 不可用
      （MainProcessPipeline / GroundDetector 被所有权门控禁用，data.isGrounded 恒为 false）；
   2. 只用 Play() 的返回值缓存状态，禁止读 Transition.State（资产级全局缓存，见 NetworkStateSync 注释）；
   3. 不自行判定 Landing 的退出：本地一切换状态，AnimationState 就变，这里跟着切，
      不做第二套状态机（否则必然与本地分叉）；
   4. Play 的淡入参数与本地逐字一致：起跳 0.15f、落地 0.25f、站立/行走 0.25f。

 与本地状态机的对应关系：
   IdleState.Enter   → Play(idleData.mixer, 0.25f)  + 每帧写 ParameterX/Y
   MoveState.Enter   → Play(moveData.mixer, 0.25f)  + 每帧写 ParameterX/Y 与 LinearMixerState.Parameter
   JumpState.Enter   → Play(JumpData.clipList[0], 0.15f)
   JumpState 落地     → Play(JumpData.clipList[1], 0.25f)（前置闸门 NormalizedTime >= 0.3f）
 */
using Animancer;
using Unity.Netcode;
using UnityEngine;

public class NetworkAnimationPresenter : NetworkBehaviour
{
    private enum JumpPhase
    {
        None,
        Airborne,
        Landing,
    }

    [Header("依赖（prefab 接线；漏接时 Awake 兜底查找）")]
    [SerializeField] private NetworkStateSync stateSync;
    [SerializeField] private AnimancerComponent animancer;
    [SerializeField] private CharacterStateMachine characterStateMachine;

    private CartesianMixerState _mixer;
    private AnimancerState _jumpState;
    private JumpPhase _jumpPhase;

    // 哨兵值：保证 spawn 后第一次 Update 必走一次 ApplyState（不依赖 spawn 消息里变量的到位时机）
    private byte _appliedState = byte.MaxValue;

    private bool _netDebug;
    private float _nextHeartbeat;
    private byte _lastLoggedState = byte.MaxValue;

    private void Awake()
    {
        if (stateSync == null) stateSync = GetComponent<NetworkStateSync>();
        if (animancer == null) animancer = GetComponentInChildren<AnimancerComponent>(true);
        if (characterStateMachine == null)
        {
            characterStateMachine = GetComponentInChildren<CharacterStateMachine>(true);
        }

        _netDebug = NetworkGameManager.HasCommandLineArg("-net-debug");
    }

    private void Update()
    {
        // 双保险：门控（NetworkPlayerController）已按所有权开关本组件，这里再自带一次早退，
        // 避免任何时序意外让木偶表现层跑到本地玩家身上。
        if (!IsSpawned || IsOwner) return;
        if (stateSync == null || animancer == null || characterStateMachine == null) return;

        byte state = stateSync.AnimationState.Value;

        if (state != _appliedState)
        {
            _appliedState = state;
            ApplyState(state);
        }

        if (state == (byte)CharacterStateMachine.StateId.Jump)
        {
            UpdateJump();
        }
        else
        {
            ApplyLocomotionParameters();
        }

        LogAnim(state);
    }

    /// <summary>状态变化时切换到对应的 StateData（与本地各状态 Enter 使用同一批资产与同一淡入值）。</summary>
    private void ApplyState(byte state)
    {
        if (state == (byte)CharacterStateMachine.StateId.Jump)
        {
            StateData jumpData = characterStateMachine.JumpData;
            if (jumpData == null || jumpData.clipList == null || jumpData.clipList.Count < 2)
            {
                Debug.LogError("[NetworkAnimationPresenter] JumpData 配置不完整（clipList 至少需要 2 个片段）");
                return;
            }

            _mixer = null;
            _jumpState = animancer.Play(jumpData.clipList[0], 0.15f);
            _jumpPhase = JumpPhase.Airborne;
            return;
        }

        // Idle 与 Move 引用的是同一条 2D 混合器资产，Play 经 GetOrCreateState 命中同一状态实例，
        // 因此这一步等价于"保持当前混合器并淡入"，与本地行为一致。
        StateData data = state == (byte)CharacterStateMachine.StateId.Move
            ? characterStateMachine.moveData
            : characterStateMachine.idleData;

        if (data == null || data.mixer == null)
        {
            Debug.LogError($"[NetworkAnimationPresenter] StateData 或 mixer 未配置（state={state}）");
            return;
        }

        _jumpPhase = JumpPhase.None;
        _jumpState = null;
        _mixer = animancer.Play(data.mixer, 0.25f) as CartesianMixerState;
    }

    /// <summary>Idle/Move 期每帧套用 Owner 采样来的实际参数值（远端不自行 Lerp，避免二次平滑）。</summary>
    private void ApplyLocomotionParameters()
    {
        if (_mixer == null) return;

        _mixer.ParameterX = stateSync.MixerX.Value;
        _mixer.ParameterY = stateSync.MixerY.Value;

        float blend = stateSync.RunBlend.Value;
        int count = _mixer.ChildCount;
        for (int i = 0; i < count; i++)
        {
            if (_mixer.GetChild(i) is LinearMixerState linear)
            {
                linear.Parameter = blend;
            }
        }
    }

    /// <summary>跳跃期：起跳片段播到 0.3 之后且已离地，才切落地片段（与 JumpState.Update 同款闸门）。</summary>
    private void UpdateJump()
    {
        if (_jumpPhase != JumpPhase.Airborne || _jumpState == null) return;

        // 进入 Jump 时本地往往仍处于贴地帧，IsGrounded 的网络延迟会让远端一进 Jump 就播落地动画，
        // 所以必须先等起跳片段走过 0.3（JumpState.cs:58 的同一判据）。
        if (_jumpState.NormalizedTime < 0.3f) return;
        if (!stateSync.IsGrounded.Value) return;

        StateData jumpData = characterStateMachine.JumpData;
        if (jumpData == null || jumpData.clipList == null || jumpData.clipList.Count < 2) return;

        _jumpState = animancer.Play(jumpData.clipList[1], 0.25f);
        _jumpPhase = JumpPhase.Landing;
    }

    #region 诊断日志（-net-debug，默认关闭；供打包端取证）

    private void LogAnim(byte state)
    {
        if (!_netDebug) return;

        float now = Time.realtimeSinceStartup;
        bool stateChanged = state != _lastLoggedState;
        if (!stateChanged && now < _nextHeartbeat) return;

        _lastLoggedState = state;
        _nextHeartbeat = now + 1f;

        // 这里输出的是"应用后本木偶自己混合器的实值"，用于与 role=local 行逐字段比对
        float mixerX = _mixer != null ? _mixer.ParameterX : 0f;
        float mixerY = _mixer != null ? _mixer.ParameterY : 0f;
        float blend = 0f;
        if (_mixer != null)
        {
            int count = _mixer.ChildCount;
            for (int i = 0; i < count; i++)
            {
                if (_mixer.GetChild(i) is LinearMixerState linear)
                {
                    blend = linear.Parameter;
                    break;
                }
            }
        }

        string clip = _jumpState != null && _jumpState.Clip != null ? _jumpState.Clip.name : "-";
        string nt = _jumpState != null ? _jumpState.NormalizedTime.ToString("F3") : "-";

        Debug.Log($"[NetDbg][anim] owner={OwnerClientId} isOwner={IsOwner} role=puppet " +
                  $"state={NetworkStateSync.StateName(state)} mixerX={mixerX.ToString("F3")} mixerY={mixerY.ToString("F3")} " +
                  $"blend={blend.ToString("F3")} grounded={stateSync.IsGrounded.Value} " +
                  $"clip={clip} nt={nt} t={now.ToString("F1")}" +
                  (stateChanged ? " event=stateChange" : string.Empty));
    }

    #endregion
}
