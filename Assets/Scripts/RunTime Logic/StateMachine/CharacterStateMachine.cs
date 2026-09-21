/*
 作为状态机的驱动器，负责在状态切换时调用状态的Enter/Exit，每帧调用当前状态的update
 */
using Animancer;
using UnityEngine;

public class CharacterStateMachine : MonoBehaviour
{
    [SerializeField] private AnimancerComponent animancer;
    [SerializeField] private MainProcessPipeline pipeline;

    [Header("状态配置")]
    public StateData idleData;
    public StateData moveData;
    public StateData runData;
    public StateData JumpData;

    [Header("跳跃时序（秒）")]
    [Tooltip("起跳片段淡入时长")]
    [SerializeField] private float jumpEnterFade = 0.15f;
    [Tooltip("落地片段淡入时长。越小越干脆、越大越平滑——落地瞬间的观感主要靠它调")]
    [SerializeField] private float jumpLandFade = 0.15f;
    [Tooltip("最小滞空时间（防抖地板）：离地后至少经过这么久，再次贴地才算落地，用来过滤起跳/贴坡时的接触抖动")]
    [SerializeField] private float jumpMinAirborne = 0.1f;
    [Tooltip("安全网：起跳后超过该时长仍未判定落地，只要贴地就强制按落地处理，避免卡在跳跃状态")]
    [SerializeField] private float jumpAirborneTimeout = 3f;

    public float JumpEnterFade => jumpEnterFade;
    public float JumpLandFade => jumpLandFade;
    public float JumpMinAirborne => jumpMinAirborne;
    public float JumpAirborneTimeout => jumpAirborneTimeout;

    public IdleState IdleState { get; private set; }
    public MoveState MoveState { get; private set; }
    public RunState RunState { get; private set; }
    public JumpState JumpState { get; private set; }

    /// <summary>
    /// 阶段 4：供网络同步读取的状态标识（RunState 从未被切换进入，不占编号）。
    /// </summary>
    public enum StateId : byte
    {
        Idle = 0,
        Move = 1,
        Jump = 2,
    }

    /// <summary>
    /// 阶段 4：当前状态标识。默认 Idle，正好覆盖"spawn 帧还没跑 Start"的窗口。
    /// </summary>
    public StateId CurrentStateId { get; private set; }

    private CharacterBaseState _currentState;
    private PlayRuntimeData _data;

    private void Awake()
    {
        IdleState = new IdleState(this, animancer, idleData);
        MoveState = new MoveState(this, animancer, moveData);
        RunState = new RunState(this, animancer, runData);
        JumpState = new JumpState(this, animancer, JumpData);
    }

    private void Start()
    {
        SwitchState(IdleState);
    }

    private void Update()
    {
        _data = pipeline.GetRuntimeData();

        if (_data != null)
        {
            _currentState?.Update(_data);
        }
        else
        {
            Debug.LogError("RuntimeData为空");
        }
    }

    public void SwitchState(CharacterBaseState newState)
    {
        _currentState?.Exit();

        _currentState = newState;

        _currentState?.Enter();

        // 阶段 4 新增：只记录标识，不改变任何状态切换行为（由 NetworkStateSync 采样）
        CurrentStateId = newState == JumpState ? StateId.Jump
                       : newState == MoveState ? StateId.Move
                       : StateId.Idle;
    }

    public PlayRuntimeData GetData() => _data;

    /// <summary>
    /// 是否已执行过 Start（即是否已进入初始 IdleState）。
    /// 阶段 3/4 用它判断远端木偶的动画时间线是否干净（没有被本地状态机抢占过）。
    /// </summary>
    public bool HasStarted => _currentState != null;
}
