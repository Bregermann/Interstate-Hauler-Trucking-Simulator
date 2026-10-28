#if UNITY_EDITOR
using UnityEngine;
[DefaultExecutionOrder(32000)]public sealed class BalletLateObserver:MonoBehaviour
{
    public System.Action sample;
    public int Samples{get;private set;}
    public int Frame{get;private set;}
    void LateUpdate(){Frame=Time.frameCount;Samples++;sample?.Invoke();}
}
#endif
