/*
 摄像机的控制，包括镜头灵敏度，距离人物远近这类
 */
using Cinemachine;
using UnityEngine;

public class ThridCamera : MonoBehaviour
{
    [SerializeField]private InputPipeline inputPipeline;
    [SerializeField]CinemachineVirtualCamera _camera;
    private CinemachineFramingTransposer framingTransposer;
    private CinemachinePOV pov;

    [Header("摄像机参数")]
    [Range(1f,10f)]public float sensitivePov = 1.0f;
    public float sensitiveDist = 1.0f;
    [Range(1f,3f)]public float minDist = 3.0f;
    [Range(5f,10f)]public float maxDist = 10f;
    public float defaultDist = 5.0f;
    public float smoothness = 1.0f;

    private float currentDist;
    private void Awake()
    {
        if(_camera == null)
        {
            Debug.LogError("第三人称组件没有设置摄像头");
        }

 
        framingTransposer = _camera.GetCinemachineComponent<CinemachineFramingTransposer>();
        pov = _camera.GetCinemachineComponent<CinemachinePOV>();

        //初始摄像机距离
        currentDist = defaultDist;
    }

    private void Update()
    {
        if (pov != null)
        {
            pov.m_HorizontalAxis.m_SpeedMode = AxisState.SpeedMode.InputValueGain;
            pov.m_VerticalAxis.m_SpeedMode = AxisState.SpeedMode.InputValueGain;
            pov.m_HorizontalAxis.m_MaxSpeed = sensitivePov*0.1f;
            pov.m_VerticalAxis.m_MaxSpeed = sensitivePov*0.1f;

        }
        else
        {
            Debug.LogError("摄像机没有设置POV");
        }
    }
    private void LateUpdate()
    {
        if (framingTransposer != null)
        {
            framingTransposer.m_CameraDistance = Mathf.Lerp(framingTransposer.m_CameraDistance, currentDist, smoothness);
        }
        else
        {
            Debug.LogError("摄像机没有设置FramingTransposer");
        }
    }
    private void OnEnable()
    {
        if (inputPipeline != null)
        {
            inputPipeline.OnScrollInput += ScrollHandle;
        }
        else
        {
            Debug.LogError("第三人称组件没有设置输入管线");
        }
    }
    private void OnDisable()
    {
        if (inputPipeline != null)
        {
            inputPipeline.OnScrollInput -= ScrollHandle;
        }
    }

    //滚轮控制摄像机距离角色远近
    private void ScrollHandle(Vector2 v)
    {
        currentDist -= v.y * sensitiveDist*Time.deltaTime;//更新距离
        currentDist = Mathf.Clamp(currentDist, minDist, maxDist);//限定距离
    }
}
