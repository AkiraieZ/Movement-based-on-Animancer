using Animancer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testAnimation : MonoBehaviour
{
    public AnimationClip clip;
    public AnimancerComponent animancer;

    [ContextMenu("Test Animation")]
    public void TestAnimation()
    {
        if (animancer == null || clip == null)
        {
            return;
        }
        StartCoroutine(PlayAndStopAnimation());
    }

    IEnumerator PlayAndStopAnimation()
    {
        var state = animancer.Play(clip);

        yield return new WaitForSeconds(state.Length);

        animancer.Stop();
    }
}
