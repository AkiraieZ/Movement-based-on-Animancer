using Animancer;
using UnityEngine;

public class IdleState : CharacterBaseState
{
    private CartesianMixerState _2dMixer;

    public IdleState(CharacterStateMachine stateMachine, AnimancerComponent animancer, StateData stateData) : base(stateMachine, animancer, stateData)
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

    public override void Update(PlayRuntimeData data)
    {
        if (!data.isGrounded)
        {
            return;
        }

        if (!data.targetLocked)
        {
            float go;
            if (data.rawInput.magnitude != 0)
            {
                go = 1;
            }
            else
            {
                go = 0;
            }

            _2dMixer.ParameterX = 0;
            _2dMixer.ParameterY = Mathf.Lerp(_2dMixer.ParameterY, go, 3f * Time.deltaTime);
        }
        else
        {
            _2dMixer.ParameterX = Mathf.Lerp(_2dMixer.ParameterX, data.rawInput.x, 3f * Time.deltaTime);
            _2dMixer.ParameterY = Mathf.Lerp(_2dMixer.ParameterY, data.rawInput.y, 3f * Time.deltaTime);
        }

        if (data.worldMoveDir != Vector3.zero)
        {
            _stateMachine.SwitchState(_stateMachine.MoveState);
            Debug.Log("Idle->Move");
        }

        if (data.wantJump && data.isGrounded)
        {
            _stateMachine.SwitchState(_stateMachine.JumpState);
        }
    }

    public override void Exit()
    {
        Debug.Log("Exit Idle");
    }
}