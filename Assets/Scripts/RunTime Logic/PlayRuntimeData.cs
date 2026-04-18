/*
 作为系统的数据黑板，存储运行时数据
 */
using UnityEngine;
public class PlayRuntimeData
{
    [Header("输入意图")]//Pipeline写入
    public Vector2 rawInput;//原始wasd
    public bool isJumpPressed;
    public bool isShiftPressed;

    [Header("解析意图")]//子Pipeline解析计算
    public Vector3 worldMoveDir;
    public bool wantJump;
    public bool wantRun;

    [Header("运动状态")]
    public bool isGrounded;
    public float currentSpeed;
    public Vector3 currentVelocity;
    public bool targetLocked;

    [Header("动画控制")]
    public float animationBlend;//混合树的参数
}
