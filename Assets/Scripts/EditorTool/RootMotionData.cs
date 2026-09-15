/*
    存储烘焙后的根运动位移相关数据，用SO可序列化容器存储
 */
using UnityEngine;

[CreateAssetMenu(fileName = "NewRootMotionData", menuName = "Game/Root Motion Data")]
public class RootMotionData : ScriptableObject
{
    [Header("源动画信息")]
    public AnimationClip sourceClip;
    public float clipLength;
    public float sampleRate; // 采样率（每秒多少帧）
    public bool isLooping;

    [Header("烘焙数据")]
    public Vector3[] deltaPositions;  // 每帧相对于上一帧的位移增量
    public Vector3[] totalPositions;  // 每帧相对于动画开始的累积位移
    public Vector3[] velocities;      // 每帧的瞬时速度
    public float[] sampleTimes;       // 每帧的时间点（秒）

    /// <summary>
    /// 根据归一化时间(0-1)获取插值后的位移增量
    /// </summary>
    public Vector3 GetDeltaPosition(float normalizedTime)
    {
        normalizedTime = Mathf.Clamp01(normalizedTime);
        if (isLooping) normalizedTime %= 1f;

        float time = normalizedTime * clipLength;
        return InterpolateArray(deltaPositions, sampleTimes, time);
    }

    /// <summary>
    /// 根据归一化时间(0-1)获取插值后的累积位移
    /// </summary>
    public Vector3 GetTotalPosition(float normalizedTime)
    {
        normalizedTime = Mathf.Clamp01(normalizedTime);
        if (isLooping) normalizedTime %= 1f;

        float time = normalizedTime * clipLength;
        return InterpolateArray(totalPositions, sampleTimes, time);
    }

    /// <summary>
    /// 根据归一化时间(0-1)获取插值后的瞬时速度
    /// </summary>
    public Vector3 GetVelocity(float normalizedTime)
    {
        normalizedTime = Mathf.Clamp01(normalizedTime);
        if (isLooping) normalizedTime %= 1f;

        float time = normalizedTime * clipLength;
        return InterpolateArray(velocities, sampleTimes, time);
    }

    // 通用线性插值方法
    private Vector3 InterpolateArray(Vector3[] array, float[] times, float targetTime)
    {
        if (array == null || array.Length == 0) return Vector3.zero;
        if (array.Length == 1) return array[0];

        // 二分查找找到最近的两个采样点
        int left = 0;
        int right = array.Length - 1;

        while (left < right)
        {
            int mid = (left + right) / 2;
            if (times[mid] < targetTime)
                left = mid + 1;
            else
                right = mid;
        }

        if (left == 0) return array[0];
        if (left == array.Length) return array[array.Length - 1];

        // 计算插值系数
        float t = Mathf.InverseLerp(times[left - 1], times[left], targetTime);
        return Vector3.Lerp(array[left - 1], array[left], t);
    }
}
