#if UNITY_EDITOR
using System;
using UnityEngine;

// Native test-only read observation after ordinary final attachment/camera
// updates. Never supplies input, animation, physics, callbacks, or scores.
[DefaultExecutionOrder(1000)]
public sealed class DragonRiderSaddleFrameObserver : MonoBehaviour
{
    public Action Observe;
    void LateUpdate()=>Observe?.Invoke();
}
#endif
