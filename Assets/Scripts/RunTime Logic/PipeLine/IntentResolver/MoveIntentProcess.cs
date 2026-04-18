/*
 移动意图解析进程
 */
using UnityEngine;

public class MoveIntentProcess
{
    private ThridCamera _camera;//第三人称摄像头

    public MoveIntentProcess(ThridCamera camera) =>_camera = camera;

    /// <summary>
    /// 作为接口，被调用时用来解析移动意图
    /// </summary>
    public void ProcessIntent(PlayRuntimeData data)
    {
        //获取当前摄像机面朝方向
        Vector3 forward = _camera.transform.forward;
        Vector3 right = _camera.transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        data.worldMoveDir = (forward * data.rawInput.y + right * data.rawInput.x).normalized;//如果大于0,就存在前进意图
        data.wantRun = data.isShiftPressed;
    }
}
