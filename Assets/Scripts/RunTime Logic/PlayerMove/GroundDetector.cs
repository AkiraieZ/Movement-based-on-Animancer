using UnityEngine;

public class GroundDetector : MonoBehaviour
{
    public LayerMask groundLayerMask;
    [SerializeField] private MainProcessPipeline pipeline;
    private PlayRuntimeData data;
    public bool isGround;

    private int _groundContactCount;

    private void Update()
    {
        data = pipeline.GetRuntimeData();
        // 触发器事件可能在组件第一次 Update 之前就到达（此时 data 还是 null，UpdateGrounded 什么也没写），
        // 那样 isGrounded 会永久停在 false（角色一出生就贴地时必现）。
        // 这里每帧用接触计数兜底同步一次，保证旗标与真实接触状态一致。
        if (data != null)
        {
            data.isGrounded = _groundContactCount > 0;
            isGround = data.isGrounded;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if ((groundLayerMask.value & (1 << other.gameObject.layer)) > 0)
        {
            _groundContactCount++;
            UpdateGrounded();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if ((groundLayerMask.value & (1 << other.gameObject.layer)) > 0)
        {
            _groundContactCount--;
            UpdateGrounded();
        }
    }

    private void UpdateGrounded()
    {
        if (data != null)
            data.isGrounded = _groundContactCount > 0;
    }
}
