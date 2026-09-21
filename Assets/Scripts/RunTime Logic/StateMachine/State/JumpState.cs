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

    /// <summary>
    /// 起跳后是否真的离开过地面。
    /// 这是"落地"的唯一前置条件：起跳那一两帧角色仍然贴地（冲量还没把它抬起来），
    /// 只有先确认离地，之后再贴地才算落地。
    /// </summary>
    private bool _hasLeftGround;

    private float _enterTime;
    private float _leftGroundTime;

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

        _animancer.Play(enter_Jump, _stateMachine.JumpEnterFade);

        var data = _stateMachine.GetData();
        if (data != null)
        {
            data.requestJump = true;
        }

        _subState = JumpSubState.Airborne;
        _hasLeftGround = false;
        _enterTime = Time.time;
        Debug.Log("Jump Start (Airborne)");
    }

    public override void Update(PlayRuntimeData data)
    {
        CharacterStateMachine sm = _stateMachine;

        switch (_subState)
        {
            case JumpSubState.Airborne:
                // 1) 离地中：记录离地事实与时刻，等待触地
                if (!data.isGrounded)
                {
                    if (!_hasLeftGround)
                    {
                        _hasLeftGround = true;
                        _leftGroundTime = Time.time;
                    }
                    return;
                }

                // 2) 贴地但还没离过地：起跳帧的常态，继续等（仅超时兜底）
                if (!_hasLeftGround)
                {
                    if (Time.time - _enterTime >= sm.JumpAirborneTimeout)
                    {
                        EnterLanding();
                    }
                    return;
                }

                // 3) 离过地之后再次贴地 = 落地。最小滞空时间只用于过滤接触抖动
                if (Time.time - _leftGroundTime >= sm.JumpMinAirborne
                    || Time.time - _enterTime >= sm.JumpAirborneTimeout)
                {
                    EnterLanding();
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

                var landAnimState = _animancer.States.Current;
                if (landAnimState != null && landAnimState.NormalizedTime >= 1f)
                {
                    data.lockMovement = false;
                    _stateMachine.SwitchState(_stateMachine.IdleState);
                }
                break;
        }
    }

    private void EnterLanding()
    {
        _animancer.Play(landed_Jump, _stateMachine.JumpLandFade);
        _subState = JumpSubState.Landing;
        Debug.Log("Jump -> Landing Buffer");
    }

    public override void Exit()
    {
        var data = _stateMachine.GetData();
        if (data != null)
        {
            data.lockMovement = false;
        }
        _hasLeftGround = false;
        Debug.Log("Jump Exit");
    }
}
