/*
 根据当前状态实现角色各类行动,仅依赖pipeline中的数据
传递当前速度到共享数据中
 */
using UnityEngine;



public class MotionDriver : MonoBehaviour
{
    [Header("组件")]
    [SerializeField]private CharacterStateMachine charState;
    [SerializeField]private MainProcessPipeline pipeline;
    [SerializeField]private ThridCamera _camera;

    private Rigidbody rb;
    private PlayRuntimeData data;

    //变量
    private Vector2 moveInput;
    private Vector3 moveDir;

    [Header("数值")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float speedLimit;
    public float currentSpeed;
    public bool wantRun;

    //共享数据
    private float blendParameter;
      
    private void Awake()
    {
        //组件获取
        if (charState == null)
        {
            Debug.LogError(this + "没有添加CharacterStateMachine");
        }
        if (pipeline == null)
        {
            Debug.LogError(this + "没有添加MainProcessPipeline");
        }
        if(_camera == null)
        {
            Debug.LogError(this + "没有添加Camera");
        }
        rb = GetComponent<Rigidbody>();

    }
    private void OnEnable()
    {
      Cursor.lockState = CursorLockMode.Locked;
    }
    private void OnDisable()
    {
      Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
        data = pipeline.GetRuntimeData();//获取共享数据


        moveInput = data.rawInput;//获取输入
        wantRun = data.wantRun;
        currentSpeed = wantRun ? runSpeed : walkSpeed;

    }
    private void FixedUpdate()
    {
        //调整方向
        moveDir = data.worldMoveDir;
        CharFaceDir();
        //实现移动
        Move();

        //更新共享数据
        WaitDataUpdate();
    }

    private void Move()
    {
        if (moveInput == Vector2.zero)
        {
            rb.velocity = Vector3.Lerp(rb.velocity,Vector3.zero,2f*Time.deltaTime);
        }
        rb.velocity = Vector3.Lerp(rb.velocity,currentSpeed * moveDir,3f*Time.deltaTime);
        currentSpeed = rb.velocity.magnitude;
        //Debug.Log("角色开始移动，当前速度" + currentSpeed);
    }
    void CharFaceDir()
    {
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
    }



}
