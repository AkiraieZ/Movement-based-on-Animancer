using Animancer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpState : CharacterBaseState
{
    private CharacterController _controller;

    public AnimationClip enter_Jump;
    public AnimationClip stay_Jump;
    public AnimationClip landed_Jump;
    public JumpState(CharacterStateMachine stateMachine, AnimancerComponent animancer, StateData stateData) : base(stateMachine, animancer, stateData)
    {
    }

    public override void Enter()
    {
        _animancer.Play(enter_Jump);
        Debug.Log("Begin Jump");
    }

    public override void Exit()
    {
        _animancer.Play(landed_Jump);
        Debug.Log("Landed");
    }

    public override void Update(PlayRuntimeData data)
    {
        _animancer.Play(stay_Jump);
        Debug.Log("In Air");
    }

}
