using Animancer;
using UnityEngine;

public class JumpState : CharacterBaseState
{
    private enum JumpSubState
    {
        Airborne,
        Landing
    }

    private JumpSubState _subState;

    private AnimationClip enter_Jump;
    private AnimationClip landed_Jump;

    public JumpState(CharacterStateMachine stateMachine, AnimancerComponent animancer, StateData stateData)
        : base(stateMachine, animancer, stateData) { }

    public override void Enter()
    {
        if (_stateData == null || _stateData.clipList == null || _stateData.clipList.Count < 2)
        {
            Debug.LogError("跳跃动画配置不正确！请检查 JumpData 的 clipList 是否至少包含两个动画片段。");
            return;
        }

        if (_stateData.animationType == StateData.AnimationType.SingleAnimation)
        {
            enter_Jump = _stateData.clipList[0];
            landed_Jump = _stateData.clipList[1];
        }
        else
        {
            Debug.LogError("没有配置正确的跳跃动画类型(请检查JumpData)");
            return;
        }

        _animancer.Play(enter_Jump, 0.15f);

        var data = _stateMachine.GetData();
        if (data != null)
        {
            data.requestJump = true;
        }

        _subState = JumpSubState.Airborne;
        Debug.Log("Jump Start (Airborne)");
    }

    public override void Update(PlayRuntimeData data)
    {
        var currentAnimState = _animancer.States.Current;

        switch (_subState)
        {
            case JumpSubState.Airborne:
                if (currentAnimState != null && currentAnimState.NormalizedTime < 0.3f)
                {
                    return;
                }

                if (data.isGrounded)
                {
                    _animancer.Play(landed_Jump, 0.25f);
                    _subState = JumpSubState.Landing;
                    Debug.Log("Jump -> Landing Buffer");
                }
                break;

            case JumpSubState.Landing:
                if (data.rawInput != Vector2.zero)
                {
                    data.lockMovement = false;
                    _stateMachine.SwitchState(_stateMachine.MoveState);
                    return;
                }

                data.lockMovement = true;

                if (currentAnimState != null && currentAnimState.NormalizedTime >= 1f)
                {
                    data.lockMovement = false;
                    _stateMachine.SwitchState(_stateMachine.IdleState);
                }
                break;
        }
    }

    public override void Exit()
    {
        var data = _stateMachine.GetData();
        if (data != null)
        {
            data.lockMovement = false;
        }
        Debug.Log("Jump Exit");
    }
}