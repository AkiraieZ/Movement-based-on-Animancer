/*
 输入管线：仅处理输入
 */
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputPipeline : MonoBehaviour
{
    //输入配置
    private PlayerInput _playerInput;

    //玩家操作对应事件
    public event Action<Vector2> OnMoveInput;
    public event Action<Vector2> OnLookInput;
    public event Action OnJumpInput;
    public event Action<bool> OnRunInput;
    public event Action<Vector2> OnScrollInput;
    public event Action<bool> OnTargetLockedInput;

    #region 生命周期
    private void Awake()
    {
        // 实例化生成的输入类（绑定所有Action）
        _playerInput = new PlayerInput();
    }
    private void Update()
    {
    }

    private void OnEnable()
    {
        //启动输入
        _playerInput.Enable();

        //在按下和抬起的时候都调用一次
        _playerInput.Player.Move.performed += OnMoveHandle;
        _playerInput.Player.Move.canceled += OnMoveHandle;

        //look
        _playerInput.Player.Look.performed += OnLookHandle;

        //仅按下的时候触发跳跃
        _playerInput.Player.Jump.performed += OnJumpHandle;


        _playerInput.Player.Run.performed += OnRunHandle;
        _playerInput.Player.Run.canceled += OnRunHandle;

        _playerInput.Player.Scroll.performed += OnScrollHandle;

        //cameraLock，视角锁定
        _playerInput.Player.TargetLock.started += OnTargetLockedHandle;
    }

    private void OnDisable()
    {
        // 注销事件
        _playerInput.Player.Move.performed -= OnMoveHandle;
        _playerInput.Player.Move.canceled -= OnMoveHandle;

        _playerInput.Player.Look.performed -= OnLookHandle;

        _playerInput.Player.Jump.performed -= OnJumpHandle;

        _playerInput.Player.Run.performed -= OnRunHandle;
        _playerInput.Player.Run.canceled -= OnRunHandle;

        _playerInput.Player.Scroll.performed -= OnScrollHandle;

        _playerInput.Player.TargetLock.started -= OnTargetLockedHandle;


        // 禁用输入图
        _playerInput.Disable();
    }


    #endregion

    #region 输入回调
    private void OnMoveHandle(InputAction.CallbackContext context)
    {
        OnMoveInput?.Invoke(context.ReadValue<Vector2>());
    }

    private void OnLookHandle(InputAction.CallbackContext context)
    {
        OnLookInput?.Invoke(context.ReadValue<Vector2>());
    }

    private void OnJumpHandle(InputAction.CallbackContext context)
    {
        OnJumpInput?.Invoke();
    }

    private void OnRunHandle(InputAction.CallbackContext context)
    {
        OnRunInput?.Invoke(context.performed);
    }

    private void OnScrollHandle(InputAction.CallbackContext context)
    {
        OnScrollInput?.Invoke(context.ReadValue<Vector2>());
    }

    private void OnTargetLockedHandle(InputAction.CallbackContext context)
    {
        OnTargetLockedInput?.Invoke(context.started);
    }
    #endregion
}