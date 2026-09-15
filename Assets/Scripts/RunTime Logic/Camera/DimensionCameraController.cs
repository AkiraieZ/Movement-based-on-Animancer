/*
 2D/3D 维度切换相机控制器：通过 SwitchView 动作在正交相机(2D)与第三人称相机(3D)之间切换。
 */
using UnityEngine;

public class DimensionCameraController : MonoBehaviour
{
    [Header("组件")]
    [SerializeField] private InputPipeline inputPipeline;
    [SerializeField] private GameObject _2dCam;
    [SerializeField] private GameObject _3dCam;

    [Header("状态")]
    public bool switched = false;

    private void Awake()
    {
        if (_2dCam == null || _3dCam == null)
        {
            Debug.LogError("DimensionCameraController：未设置 2D/3D 相机");
        }
    }

    private void OnEnable()
    {
        if (inputPipeline != null)
        {
            inputPipeline.OnSwitchViewInput += SwitchView;
        }
        else
        {
            Debug.LogError("DimensionCameraController：未设置输入管线");
        }

        ApplySwitch();
    }

    private void OnDisable()
    {
        if (inputPipeline != null)
        {
            inputPipeline.OnSwitchViewInput -= SwitchView;
        }
    }

    private void SwitchView()
    {
        switched = !switched;
        ApplySwitch();
    }

    private void ApplySwitch()
    {
        if (_2dCam != null) _2dCam.SetActive(switched);
        if (_3dCam != null) _3dCam.SetActive(!switched);
    }
}
