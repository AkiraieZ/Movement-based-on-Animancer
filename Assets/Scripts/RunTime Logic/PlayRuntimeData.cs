/*
 作为系统数据容器，存储运行时数据
 */
using UnityEngine;
public class PlayRuntimeData
{
    [Header("输入数据")]//Pipeline写入
    public Vector2 rawInput;//原始wasd输入
    public bool isJumpPressed;
    public bool isShiftPressed;

    [Header("意图数据")]//由Pipeline解析后写入
    public Vector3 worldMoveDir;
    public bool wantRun;
    public bool wantJump;

    [Header("状态数据")]
    public bool isGrounded;
    public float currentSpeed;
    public Vector3 currentVelocity;
    public bool targetLocked;
    public bool turn;
    public bool requestJump;
    public bool lockMovement;

    [Header("动画相关")]
    public float animationBlend;//动画混合参数
}