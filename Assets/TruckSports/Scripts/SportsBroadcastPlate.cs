using UnityEngine;
using UnityEngine.UI;
namespace LWS.TruckSports {
// Code-native broadcast silhouette; keeps existing Heat buttons and their Image references intact.
[DisallowMultipleComponent]public sealed class SportsBroadcastPlate:BaseMeshEffect {
 public float cut=12;public bool accent;
 public override void ModifyMesh(VertexHelper mesh){if(!IsActive()||mesh.currentVertCount==0)return;var vertex=new UIVertex();mesh.PopulateUIVertex(ref vertex,0);Color color=vertex.color;var r=((RectTransform)transform).rect;float c=Mathf.Min(cut,Mathf.Min(r.width,r.height)*.23f);Vector2[] p={new Vector2(r.xMin+c,r.yMin),new Vector2(r.xMax,r.yMin),new Vector2(r.xMax,r.yMax-c),new Vector2(r.xMax-c,r.yMax),new Vector2(r.xMin,r.yMax),new Vector2(r.xMin,r.yMin+c)};mesh.Clear();mesh.AddVert(r.center,color,Vector2.zero);for(int i=0;i<p.Length;i++)mesh.AddVert(p[i],color,Vector2.zero);for(int i=0;i<p.Length;i++)mesh.AddTriangle(0,i+1,(i+1)%p.Length+1);if(accent){int at=mesh.currentVertCount;Color orange=new Color(1,.39f,.08f,color.a);mesh.AddVert(new Vector2(r.xMin+c,r.yMin),orange,Vector2.zero);mesh.AddVert(new Vector2(r.xMax,r.yMin),orange,Vector2.zero);mesh.AddVert(new Vector2(r.xMax,r.yMin+3),orange,Vector2.zero);mesh.AddVert(new Vector2(r.xMin+c+3,r.yMin+3),orange,Vector2.zero);mesh.AddTriangle(at,at+1,at+2);mesh.AddTriangle(at,at+2,at+3);}}
}
}
