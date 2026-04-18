using Animancer;
using UnityEditor.Rendering.Universal;
using UnityEngine;

public class IdleState :CharacterBaseState
{
    private CartesianMixerState _2dMixer;
    public IdleState(CharacterStateMachine stateMachine, AnimancerComponent animancer, StateData stateData) : base(stateMachine, animancer, stateData)
    {

    }

    public override void Enter()
    {
        if (_stateData.animationType == StateData.AnimationType.Mixer2D)
        {
            _currentAnimancerState = _animancer.Play(_stateData.mixer);
            _2dMixer = _currentAnimancerState as CartesianMixerState; //缓存当前mixer
        }
    }
    public override void Update(PlayRuntimeData data)
    {
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


        //玩家是否想要移动
        if (data.worldMoveDir != Vector3.zero)
        {
            //玩家想要移动
            _stateMachine.SwitchState(_stateMachine.MoveState);
            Debug.Log("Idle->Move");
        }

        //是否要跳跃
        if (data.wantJump)
        {
            //_stateMachine.SwitchState(_stateMachine.)
        }

    }

    public override void Exit()
    {
        //也许有需要重置的变量
        Debug.Log("Exit Idle");
    }


}
