/*
 作为各种状态的数据配置表，包括了动画资源和根运动数据这类资源
 */
using Animancer;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "new StateData",menuName ="StateData")]
public class StateData : ScriptableObject
{
    public enum AnimationType
    {
        SingleAnimation,
        LinearMixer,
        Mixer2D
    }
    [Header("动画配置")]
    public AnimationType animationType;
    public ClipTransition animation;
    public TransitionAsset mixer;
    public List<AnimationClip> clipList;



    [Header("根运动数据")]
    public RootMotionData rootMotionData;
}
