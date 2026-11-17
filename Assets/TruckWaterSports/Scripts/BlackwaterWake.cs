using UnityEngine;
namespace LWS.TruckWaterSports {
// The same entity-centered wave drives the rendered surface and vessel waterline.
public sealed class BlackwaterWake:MonoBehaviour {
 public BlackwaterMatch match;public Rigidbody entity;Mesh mesh;Vector3[] original,vertices;
 void Start(){var filter=GetComponent<MeshFilter>();mesh=Instantiate(filter.sharedMesh);mesh.MarkDynamic();filter.sharedMesh=mesh;original=mesh.vertices;vertices=new Vector3[original.Length];}
 public float Height(Vector3 point,float clock){if(entity==null)return 0;float distance=Vector3.ProjectOnPlane(point-entity.position,Vector3.up).magnitude;float influence=Mathf.Clamp01(1-distance/45);return Mathf.Sin(distance*.65f-clock*3.2f)*influence*influence*.8f;}
 void LateUpdate(){if(mesh==null||match==null)return;for(int i=0;i<vertices.Length;i++){vertices[i]=original[i];vertices[i].y+=Height(transform.TransformPoint(original[i]),match.Clock);}mesh.vertices=vertices;mesh.RecalculateNormals();}
 void OnDestroy(){if(mesh!=null)Destroy(mesh);}
}
}
