/*
 输入处理流水线，负责监听用户输入
 */
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputPipeline : MonoBehaviour
{
    private PlayerInput _playerInput;

    public event Action<Vector2> OnMoveInput;
    public event Action<Vector2> OnLookInput;
    public event Action OnJumpInput;
    public event Action<bool> OnRunInput;
    public event Action<Vector2> OnScrollInput;
    public event Action<bool> OnTargetLockedInput;
    public event Action OnSwitchViewInput;

    #region 生命周期
    private void Awake()
    {
        _playerInput = new PlayerInput();
    }

    private void Update()
    {
    }

    private void OnEnable()
    {
        _playerInput.Enable();

        _playerInput.Player.Move.performed += OnMoveHandle;
        _playerInput.Player.Move.canceled += OnMoveHandle;

        _playerInput.Player.Look.performed += OnLookHandle;

        _playerInput.Player.Jump.performed += OnJumpHandle;

        _playerInput.Player.Run.performed += OnRunHandle;
        _playerInput.Player.Run.canceled += OnRunHandle;

        _playerInput.Player.Scroll.performed += OnScrollHandle;

        _playerInput.Player.TargetLock.started += OnTargetLockedHandle;

        _playerInput.Player.SwitchView.started += OnSwitchViewHandle;
    }

    private void OnDisable()
    {
        _playerInput.Player.Move.performed -= OnMoveHandle;
        _playerInput.Player.Move.canceled -= OnMoveHandle;

        _playerInput.Player.Look.performed -= OnLookHandle;

        _playerInput.Player.Jump.performed -= OnJumpHandle;

        _playerInput.Player.Run.performed -= OnRunHandle;
        _playerInput.Player.Run.canceled -= OnRunHandle;

        _playerInput.Player.Scroll.performed -= OnScrollHandle;

        _playerInput.Player.TargetLock.started -= OnTargetLockedHandle;

        _playerInput.Player.SwitchView.started -= OnSwitchViewHandle;

        _playerInput.Disable();
    }
    #endregion

    #region 回调处理
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

    private void OnSwitchViewHandle(InputAction.CallbackContext context)
    {
        OnSwitchViewInput?.Invoke();
    }
    #endregion
}