/*
 作为游戏的主处理pipeline，负责接收来自输入系统的数据，汇总到PlayRuntimeData中，对外提供接口给各模块读取运行时数据
 */
using UnityEngine;

public class MainProcessPipeline : MonoBehaviour
{
    [SerializeField] private InputPipeline inputPipe;
    [SerializeField] private ThridCamera thridCamera;

    private PlayRuntimeData data;

    private MoveIntentProcess moveIntentPrecess;

    private bool _isTargetLocked = false;

    private void Awake()
    {
        data = new PlayRuntimeData();
        moveIntentPrecess = new MoveIntentProcess(thridCamera);
    }

    private void Start()
    {
        Debug.Log("Run MainPipeline");
    }

    private void Update()
    {
        moveIntentPrecess.ProcessIntent(data);
    }

    private void OnEnable()
    {
        inputPipe.OnMoveInput += HandleMoveInput;
        inputPipe.OnJumpInput += HandleJumpInput;
        inputPipe.OnRunInput += HandleRunInput;
        inputPipe.OnTargetLockedInput += HandleTargetLockedInput;
    }

    private void OnDisable()
    {
        inputPipe.OnMoveInput -= HandleMoveInput;
        inputPipe.OnJumpInput -= HandleJumpInput;
        inputPipe.OnRunInput -= HandleRunInput;
        inputPipe.OnTargetLockedInput -= HandleTargetLockedInput;
    }

    private void HandleMoveInput(Vector2 v)
    {
        data.rawInput = v;
    }

    private void HandleJumpInput()
    {
        data.isJumpPressed = true;
    }

    private void HandleRunInput(bool isPressing)
    {
        data.isShiftPressed = isPressing;
    }

    private void HandleTargetLockedInput(bool pressed)
    {
        if (pressed)
        {
            _isTargetLocked = !_isTargetLocked;
            data.targetLocked = _isTargetLocked;
            Debug.Log($"锁定状态切换为: {_isTargetLocked}");
        }
    }

    public PlayRuntimeData GetRuntimeData() => data;
}