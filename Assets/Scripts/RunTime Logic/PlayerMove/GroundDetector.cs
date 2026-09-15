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
        isGround = data.isGrounded;
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