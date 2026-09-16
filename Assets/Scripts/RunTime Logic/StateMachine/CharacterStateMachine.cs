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

    public IdleState IdleState { get; private set; }
    public MoveState MoveState { get; private set; }
    public RunState RunState { get; private set; }
    public JumpState JumpState { get; private set; }

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
    }

    public PlayRuntimeData GetData() => _data;

    /// <summary>
    /// 是否已执行过 Start（即是否已进入初始 IdleState）。
    /// 阶段 3/4 用它判断远端木偶的动画时间线是否干净（没有被本地状态机抢占过）。
    /// </summary>
    public bool HasStarted => _currentState != null;
}
