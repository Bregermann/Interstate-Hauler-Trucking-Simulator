#if UNITY_EDITOR
using UnityEngine;
[DefaultExecutionOrder(32000)]
public sealed class FashionGalleryLateProbe : MonoBehaviour
{
    public System.Action sample;
    void LateUpdate() { sample?.Invoke(); }
}
#endif
