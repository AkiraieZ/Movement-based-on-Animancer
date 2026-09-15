/*
 作为状态基类，规定所有状态类要做的事情
 */
using Animancer;
using UnityEngine;

public abstract class CharacterBaseState
{
    protected readonly CharacterStateMachine _stateMachine;
    protected readonly AnimancerComponent _animancer;//Animancer组件
    protected readonly StateData _stateData;//根运动数据

    protected AnimancerState _currentAnimancerState;//缓存当前播放的动画状态

    //有参构造函数，被子类调用初始化
    protected CharacterBaseState(CharacterStateMachine stateMachine, AnimancerComponent animancer,StateData stateData)
    {
        _stateMachine = stateMachine;
        _animancer = animancer; ;
        _stateData = stateData;
    }

    /// <summary>
    /// 规定状态进入时的行为，调用一次
    /// </summary>
    public abstract void Enter();

    /// <summary>
    /// 规定状态进行时的行为，每帧调用
    /// </summary>
    public abstract void Update(PlayRuntimeData data);

    /// <summary>
    /// 规定状态退出时的行为，调用一次
    /// </summary>
    public abstract void Exit();

    
}
