/*
 作为状态的调度机，负责保存状态，切换状态，每一帧调用当前状态的update
 */
using Animancer;
using UnityEngine;

public class CharacterStateMachine : MonoBehaviour
{
    //组件调用，inspector窗口赋值
    [SerializeField] private AnimancerComponent animancer;
    [SerializeField] private MainProcessPipeline pipeline; // 用来获取 PlayRuntimeData

    [Header("状态数据")]//获取对应的状态数据
    public StateData idleData;
    public StateData moveData;
    public StateData runData;

    //状态实例
    public IdleState IdleState { get; private set; }
    public MoveState MoveState { get; private set; }
    public RunState RunState { get; private set; }


    private CharacterBaseState _currentState;
    private PlayRuntimeData _data;

    private void Awake()
    {
        //初始化当前状态
        IdleState = new IdleState(this,animancer,idleData);
        MoveState = new MoveState(this,animancer,moveData);
        RunState = new RunState(this, animancer, runData);
    
    }
    private void Start()
    {

        //初次状态应该是Idle
        SwitchState(IdleState);
        
    }

    private void Update()
    {
        _data = pipeline.GetRuntimeData();//获取数据上下文

        if (_data != null)
        {
            _currentState?.Update(_data);
        }
        else
        {
            Debug.LogError("RuntimeData为空");
        }

        Debug.Log("当前状态" + _currentState);
    }

    public void SwitchState(CharacterBaseState newState)
    {
        //清理旧状态
        _currentState?.Exit();

        //更新当前状态
        _currentState = newState;

        //调用当前状态Enter
        _currentState?.Enter();
    }
}
