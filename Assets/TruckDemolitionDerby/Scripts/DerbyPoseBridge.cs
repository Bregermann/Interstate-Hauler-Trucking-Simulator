using UnityEngine;
namespace LWS.TruckDerby {
// Stable render-only record sources avoid depending on NWH's runtime audio/wheel child hierarchy.
// Every proxy pose/visibility comes from the actual intact or broken mesh, never new simulation.
public sealed class DerbyPoseBridge:MonoBehaviour {
 public DerbyFighter fighter;public Transform proxy;public MeshFilter[] actual,copies;public Renderer[] renderers;
 public void Sync(){proxy.SetPositionAndRotation(fighter.body.position,fighter.body.rotation);for(int i=0;i<actual.Length;i++){var a=actual[i].transform;var b=copies[i].transform;b.SetPositionAndRotation(a.position,a.rotation);b.localScale=renderers[i].enabled&&renderers[i].gameObject.activeInHierarchy?a.lossyScale:Vector3.zero;}}
}
}
