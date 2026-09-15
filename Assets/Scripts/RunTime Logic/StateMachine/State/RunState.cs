using Animancer;
using UnityEngine;

public class RunState : CharacterBaseState
{
    private CartesianMixerState _2dMixer;

    public RunState(CharacterStateMachine stateMachine, AnimancerComponent animancer, StateData stateData) : base(stateMachine, animancer, stateData)
    {
    }

    public override void Enter()
    {
        if (_stateData.animationType == StateData.AnimationType.Mixer2D)
        {
            _currentAnimancerState = _animancer.Play(_stateData.mixer, 0.25f);
            _2dMixer = _currentAnimancerState as CartesianMixerState;
        }
    }

    public override void Exit()
    {
    }

    public override void Update(PlayRuntimeData data)
    {
        if (data.rawInput != Vector2.zero)
        {
            if (data.wantRun == false)
            {
                _stateMachine.SwitchState(_stateMachine.MoveState);
            }
        }
        else
        {
            _stateMachine.SwitchState(_stateMachine.IdleState);
        }
    }
}