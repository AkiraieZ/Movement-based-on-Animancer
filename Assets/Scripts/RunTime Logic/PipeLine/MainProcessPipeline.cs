/*
 作为调度者管理子pipeline,并且负责将对应数据写入数据黑板PlayRuntimeData中，提供接口让其他模块来读取共享数据
 */
using UnityEngine;
public class MainProcessPipeline : MonoBehaviour
{
    [SerializeField]private InputPipeline inputPipe;//输入管线
    [SerializeField]private ThridCamera thridCamera;//第三人称摄像机

    private PlayRuntimeData data;

    //相关行为意图解析器
    private MoveIntentProcess moveIntentPrecess;//移动意图解析

    //临时存储
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
        //解读行为意图
        moveIntentPrecess.ProcessIntent(data);
    }
    private void OnEnable()
    {
        // 订阅输入管线事件
        inputPipe.OnMoveInput += (v) => data.rawInput = v;
        inputPipe.OnJumpInput += () => data.isJumpPressed = true;
        inputPipe.OnRunInput += (isPressing) => data.isShiftPressed = isPressing;
        inputPipe.OnTargetLockedInput += (pressed) => {
            // 只在按键按下的瞬间切换
            if (pressed)
            {
                _isTargetLocked = !_isTargetLocked;  // 切换状态
                data.targetLocked = _isTargetLocked;

                Debug.Log($"锁定状态切换为: {_isTargetLocked}");
            }
        };
    }

    private void OnDisable()
    {
        // 注销事件
        inputPipe.OnMoveInput -= (v) => data.rawInput = v;
        inputPipe.OnJumpInput -= () => data.isJumpPressed = true;
        inputPipe.OnRunInput -= (isPressing) => data.isShiftPressed = isPressing;
        inputPipe.OnTargetLockedInput -= (pressed) => {
            // 只在按键按下的瞬间切换
            if (pressed)
            {
                _isTargetLocked = !_isTargetLocked;  // 切换状态
                data.targetLocked = _isTargetLocked;

                Debug.Log($"锁定状态切换为: {_isTargetLocked}");
            }
        };
    }
    public PlayRuntimeData GetRuntimeData() => data;
}
