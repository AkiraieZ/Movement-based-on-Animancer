/*
 负责角色当前状态的物理移动和旋转，根据pipeline中的数据驱动角色的运动行为
 */
using UnityEngine;

public class MotionDriver : MonoBehaviour
{
    [Header("依赖")]
    [SerializeField] private CharacterStateMachine charState;
    [SerializeField] private MainProcessPipeline pipeline;
    [SerializeField] private ThridCamera _camera;

    private Rigidbody rb;
    private PlayRuntimeData data;

    private Vector2 moveInput;
    private Vector3 moveDir;

    [Header("参数")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float speedLimit;
    public float currentSpeed;
    public bool wantRun;
    public float jumpForce = 4f;

    private float blendParameter;

    private void Awake()
    {
        if (charState == null)
        {
            Debug.LogError(this + "没有绑定CharacterStateMachine");
        }
        if (pipeline == null)
        {
            Debug.LogError(this + "没有绑定MainProcessPipeline");
        }
        if (_camera == null)
        {
            Debug.LogError(this + "没有绑定Camera");
        }
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    // 阶段 3：光标锁已迁移到 NetworkPlayerController（按 Owner + 窗口焦点统一管理，另加 Esc 解锁），
    // 本类不再碰 Cursor，避免"禁用远端 MotionDriver 触发 OnDisable 解锁本机光标"的竞态。

    private void Update()
    {
        data = pipeline.GetRuntimeData();
        moveInput = data.rawInput;
        wantRun = data.wantRun;
        currentSpeed = wantRun ? runSpeed : walkSpeed;
    }

    private void FixedUpdate()
    {
        if (data == null)
        {
            data = pipeline?.GetRuntimeData();
            if (data == null) return;
        }
        moveDir = data.worldMoveDir;
        CharFaceDir();
        Move();
        Jump();
        WaitDataUpdate();
    }

    #region 角色移动与跳跃逻辑

    private void Move()
    {
        if (data.lockMovement)
        {
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, 5f * Time.deltaTime);
            currentSpeed = 0;
            return;
        }

        if (moveInput == Vector2.zero)
        {
            rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, 2f * Time.deltaTime);
        }
        rb.velocity = Vector3.Lerp(rb.velocity, currentSpeed * moveDir, 3f * Time.deltaTime);
        currentSpeed = rb.velocity.magnitude;
    }

    private void Jump()
    {
        if (data.requestJump && data.isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            data.requestJump = false;
        }
    }

    #endregion

    void CharFaceDir()
    {
        if (data.lockMovement)
        {
            return;
        }

        if (moveInput == Vector2.zero)
        {
            return;
        }
        if (data.targetLocked)
        {
            Vector3 cameraFwd = _camera.transform.forward;
            cameraFwd.y = 0;
            transform.forward = cameraFwd;
        }
        else
        {
            transform.forward = Vector3.Lerp(transform.forward, moveDir, 0.2f);
        }
    }

    void WaitDataUpdate()
    {
        data.currentSpeed = currentSpeed;
        if (wantRun)
        {
            blendParameter = 1;
        }
        else
        {
            blendParameter = 0;
        }
        data.animationBlend = blendParameter;
        data.currentVelocity = rb.velocity;
    }
}
