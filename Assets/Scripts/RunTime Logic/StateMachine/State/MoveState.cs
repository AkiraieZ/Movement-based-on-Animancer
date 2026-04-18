using Animancer;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MoveState :CharacterBaseState
{
    private CartesianMixerState _2dMixer;
    public MoveState(CharacterStateMachine stateMachine, AnimancerComponent animancer, StateData stateData) : base(stateMachine, animancer, stateData)
    {

    }
    public override void Enter()
    {
        if (_stateData.animationType == StateData.AnimationType.Mixer2D)
        {
            _currentAnimancerState = _animancer.Play(_stateData.mixer);
            _2dMixer = _currentAnimancerState as CartesianMixerState; //»º´æµ±Ç°mixer
        }

        Debug.Log("×´Ì¬ÇÐ»»£ºMove");
    }

    public override void Exit()
    {
        Debug.Log("Exit Move");
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
            _2dMixer.ParameterY = Mathf.Lerp(_2dMixer.ParameterY,go, 3f * Time.deltaTime);
        }
        else
        {
            _2dMixer.ParameterX = Mathf.Lerp(_2dMixer.ParameterX, data.rawInput.x, 3f * Time.deltaTime);
            _2dMixer.ParameterY = Mathf.Lerp(_2dMixer.ParameterY, data.rawInput.y, 3f * Time.deltaTime);
        }


            int count = _2dMixer.ChildCount;
        for (int i = 0; i < count; i++)
        {
            var cur = _2dMixer.GetChild(i);
            if (cur is LinearMixerState linearMixerState)
            {
                linearMixerState.Parameter = Mathf.Lerp(linearMixerState.Parameter,data.animationBlend,3f*Time.deltaTime);
            }
        }


        if (data.rawInput==Vector2.zero)
        {
            _stateMachine.SwitchState(_stateMachine.IdleState);
        }
        //if (data.wantRun)
        //{
        //    _stateMachine.SwitchState(_stateMachine.RunState);
        //}
        else
        {
            Debug.Log("Move");
        }
    }

}
